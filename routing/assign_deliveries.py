from __future__ import annotations

import csv
from collections import Counter, defaultdict
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parents[1]
RAW_DATA_DIR = ROOT_DIR / "spark" / "data" / "raw_data"


def read_csv(filename: str) -> list[dict[str, str]]:
    """Read a CSV file from the shared raw-data directory."""
    input_path = RAW_DATA_DIR / filename

    if not input_path.exists():
        raise FileNotFoundError(
            f"No se encontró el archivo requerido: {input_path}"
        )

    with input_path.open(
        mode="r",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        return list(csv.DictReader(csv_file))


def is_active(driver: dict[str, str]) -> bool:
    """Interpret common true values from the CSV."""
    return driver["active"].strip().lower() in {
        "true",
        "1",
        "yes",
        "si",
        "sí",
    }


def validate_assignments(
    pending_orders: list[dict[str, str]],
    drivers_by_id: dict[int, dict[str, str]],
    assignments: list[dict[str, Any]],
) -> None:
    """Validate coverage, uniqueness, capacity and restaurant matching."""
    pending_order_ids = {
        int(order["id"])
        for order in pending_orders
    }

    assigned_order_ids = [
        int(assignment["order_id"])
        for assignment in assignments
    ]

    if len(assigned_order_ids) != len(set(assigned_order_ids)):
        raise ValueError(
            "Un pedido fue asignado a más de un repartidor."
        )

    if set(assigned_order_ids) != pending_order_ids:
        missing_orders = pending_order_ids - set(assigned_order_ids)
        unexpected_orders = set(assigned_order_ids) - pending_order_ids

        raise ValueError(
            "La asignación no coincide con los pedidos pendientes. "
            f"Faltantes: {sorted(missing_orders)}. "
            f"Inesperados: {sorted(unexpected_orders)}."
        )

    assigned_per_driver = Counter(
        int(assignment["driver_id"])
        for assignment in assignments
    )

    for driver_id, total_assigned in assigned_per_driver.items():
        capacity = int(drivers_by_id[driver_id]["capacity"])

        if total_assigned > capacity:
            raise ValueError(
                f"El repartidor {driver_id} recibió "
                f"{total_assigned} pedidos y su capacidad es {capacity}."
            )

    orders_by_id = {
        int(order["id"]): order
        for order in pending_orders
    }

    for assignment in assignments:
        driver_id = int(assignment["driver_id"])
        order_id = int(assignment["order_id"])

        driver_restaurant = int(
            drivers_by_id[driver_id]["restaurant_id"]
        )

        order_restaurant = int(
            orders_by_id[order_id]["restaurant_id"]
        )

        if driver_restaurant != order_restaurant:
            raise ValueError(
                f"El repartidor {driver_id} y el pedido {order_id} "
                "pertenecen a restaurantes diferentes."
            )


def main() -> None:
    orders = read_csv("orders.csv")
    drivers = read_csv("drivers.csv")

    pending_orders = sorted(
        [
            order
            for order in orders
            if order["status"].strip() == "Preparando"
        ],
        key=lambda order: int(order["id"]),
    )

    active_drivers = [
        driver
        for driver in drivers
        if is_active(driver)
    ]

    if not pending_orders:
        raise ValueError(
            "No existen pedidos con estado Preparando."
        )

    if not active_drivers:
        raise ValueError(
            "No existen repartidores activos."
        )

    drivers_by_id = {
        int(driver["id"]): driver
        for driver in active_drivers
    }

    drivers_by_restaurant: dict[
        int,
        list[dict[str, str]],
    ] = defaultdict(list)

    for driver in active_drivers:
        drivers_by_restaurant[
            int(driver["restaurant_id"])
        ].append(driver)

    driver_loads: Counter[int] = Counter()
    assignments: list[dict[str, Any]] = []

    assignment_base_time = datetime(
        year=2026,
        month=7,
        day=1,
        hour=8,
        minute=0,
    )

    for assignment_id, order in enumerate(
        pending_orders,
        start=1,
    ):
        restaurant_id = int(order["restaurant_id"])

        eligible_drivers = drivers_by_restaurant.get(
            restaurant_id,
            [],
        )

        available_drivers = [
            driver
            for driver in eligible_drivers
            if driver_loads[int(driver["id"])]
            < int(driver["capacity"])
        ]

        if not available_drivers:
            raise ValueError(
                "No hay capacidad disponible para el pedido "
                f"{order['id']} del restaurante {restaurant_id}."
            )

        selected_driver = min(
            available_drivers,
            key=lambda driver: (
                driver_loads[int(driver["id"])],
                int(driver["id"]),
            ),
        )

        selected_driver_id = int(selected_driver["id"])
        driver_loads[selected_driver_id] += 1

        assigned_at = (
            assignment_base_time
            + timedelta(minutes=assignment_id * 2)
        )

        assignments.append(
            {
                "id": assignment_id,
                "driver_id": selected_driver_id,
                "order_id": int(order["id"]),
                "restaurant_id": restaurant_id,
                "delivery_location_id": int(
                    order["delivery_location_id"]
                ),
                "assigned_at": assigned_at.isoformat(
                    timespec="seconds"
                ),
            }
        )

    validate_assignments(
        pending_orders=pending_orders,
        drivers_by_id=drivers_by_id,
        assignments=assignments,
    )

    output_path = RAW_DATA_DIR / "delivery_assignments.csv"

    with output_path.open(
        mode="w",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        writer = csv.DictWriter(
            csv_file,
            fieldnames=[
                "id",
                "driver_id",
                "order_id",
                "restaurant_id",
                "delivery_location_id",
                "assigned_at",
            ],
        )

        writer.writeheader()
        writer.writerows(assignments)

    print(
        f"delivery_assignments.csv: "
        f"{len(assignments)} asignaciones generadas"
    )

    print("Distribución por repartidor:")

    for driver_id in sorted(drivers_by_id):
        driver = drivers_by_id[driver_id]

        print(
            f"- {driver['name']}: "
            f"{driver_loads[driver_id]} pedidos"
        )

    print("Validaciones de asignación completadas correctamente.")


if __name__ == "__main__":
    main()