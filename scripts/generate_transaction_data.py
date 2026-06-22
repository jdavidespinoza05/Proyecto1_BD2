from __future__ import annotations

import csv
from collections import Counter, defaultdict
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parents[1]
RAW_DATA_DIR = ROOT_DIR / "spark" / "data" / "raw_data"

TOTAL_ORDERS = 500

# Cada restaurante tiene tres combinaciones diseñadas para producir
# patrones de co-compra reconocibles.
PRODUCT_COMBOS: dict[int, list[tuple[int, int]]] = {
    1: [
        (1, 4),   # Casado + refresco
        (3, 4),   # Chifrijo + refresco
        (5, 6),   # Arroz con pollo + tres leches
    ],
    2: [
        (7, 8),   # Hamburguesa + papas
        (7, 10),  # Hamburguesa + limonada
        (9, 10),  # Pizza + limonada
    ],
    3: [
        (13, 14),  # Café + cheesecake
        (15, 17),  # Sándwich + capuchino
        (16, 13),  # Gallo pinto + café
    ],
    4: [
        (19, 22),  # Pollo + batido
        (20, 23),  # Tacos + empanada
        (21, 24),  # Sopa negra + flan
    ],
    5: [
        (25, 29),  # Salmón + té frío
        (26, 28),  # Pasta + bruschetta
        (27, 29),  # Ensalada + té frío
    ],
}

# Favorece la primera combinación de cada restaurante:
# 50% primera, 33% segunda y 17% tercera aproximadamente.
COMBO_PATTERN = [0, 0, 0, 1, 1, 2]

# Horas repetidas intencionalmente para producir horarios pico.
ORDER_HOURS = [
    11,
    12,
    12,
    13,
    18,
    19,
    19,
    20,
    20,
    20,
    21,
]


def read_csv(filename: str) -> list[dict[str, str]]:
    """Read a UTF-8 CSV file from the shared raw-data directory."""
    path = RAW_DATA_DIR / filename

    if not path.exists():
        raise FileNotFoundError(f"No se encontró el archivo requerido: {path}")

    with path.open(mode="r", newline="", encoding="utf-8-sig") as csv_file:
        return list(csv.DictReader(csv_file))


def write_csv(
    filename: str,
    fieldnames: list[str],
    rows: list[dict[str, Any]],
) -> None:
    """Overwrite a CSV file with deterministic generated data."""
    path = RAW_DATA_DIR / filename

    with path.open(
        mode="w",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        writer = csv.DictWriter(csv_file, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)

    print(f"{filename}: {len(rows)} registros generados")


def get_order_status(order_id: int) -> str:
    """
    Create an exact status distribution for every group of 20 orders:

    - 14 completed: 70%
    - 3 cancelled: 15%
    - 3 preparing: 15%
    """
    position = (order_id - 1) % 20

    if position < 14:
        return "Completado"

    if position < 17:
        return "Cancelado"

    return "Preparando"


def validate_generated_data(
    orders: list[dict[str, Any]],
    order_items: list[dict[str, Any]],
    menu_by_id: dict[int, dict[str, str]],
) -> None:
    """Validate the generated transactional dataset before writing it."""

    if len(orders) != TOTAL_ORDERS:
        raise ValueError(
            f"Se esperaban {TOTAL_ORDERS} pedidos y se generaron {len(orders)}."
        )

    expected_items = TOTAL_ORDERS * 3

    if len(order_items) != expected_items:
        raise ValueError(
            f"Se esperaban {expected_items} detalles y se generaron "
            f"{len(order_items)}."
        )

    items_by_order: dict[int, list[dict[str, Any]]] = defaultdict(list)

    for item in order_items:
        items_by_order[int(item["order_id"])].append(item)

    for order in orders:
        order_id = int(order["id"])
        items = items_by_order[order_id]

        if len(items) != 3:
            raise ValueError(
                f"El pedido {order_id} no contiene exactamente 3 productos."
            )

        product_ids = {int(item["menu_id"]) for item in items}

        if len(product_ids) != 3:
            raise ValueError(
                f"El pedido {order_id} contiene productos duplicados."
            )

        calculated_total = sum(int(item["subtotal"]) for item in items)

        if calculated_total != int(order["total_amount"]):
            raise ValueError(
                f"El total del pedido {order_id} no coincide con sus detalles."
            )

        restaurant_id = int(order["restaurant_id"])

        for product_id in product_ids:
            product_restaurant_id = int(
                menu_by_id[product_id]["restaurant_id"]
            )

            if product_restaurant_id != restaurant_id:
                raise ValueError(
                    f"El producto {product_id} no pertenece al restaurante "
                    f"del pedido {order_id}."
                )


def main() -> None:
    users = read_csv("users.csv")
    restaurants = read_csv("restaurants.csv")
    menus = read_csv("menus.csv")

    if not users:
        raise ValueError("users.csv no contiene usuarios.")

    if not restaurants:
        raise ValueError("restaurants.csv no contiene restaurantes.")

    if not menus:
        raise ValueError("menus.csv no contiene productos.")

    user_by_id = {
        int(user["id"]): user
        for user in users
    }

    menu_by_id = {
        int(menu["id"]): menu
        for menu in menus
    }

    product_ids_by_restaurant: dict[int, list[int]] = defaultdict(list)

    for menu in menus:
        restaurant_id = int(menu["restaurant_id"])
        product_ids_by_restaurant[restaurant_id].append(int(menu["id"]))

    orders: list[dict[str, Any]] = []
    order_items: list[dict[str, Any]] = []

    next_item_id = 1
    base_date = datetime(2025, 1, 1)

    for order_id in range(1, TOTAL_ORDERS + 1):
        restaurant_id = ((order_id - 1) % 5) + 1

        # Distribuye los pedidos entre los 15 usuarios.
        user_id = (((order_id * 7) - 1) % len(user_by_id)) + 1
        user = user_by_id[user_id]

        combo_options = PRODUCT_COMBOS[restaurant_id]
        combo_cycle_position = ((order_id - 1) // 5) % len(COMBO_PATTERN)
        combo_index = COMBO_PATTERN[combo_cycle_position]
        selected_combo = combo_options[combo_index]

        # Selecciona un tercer producto del mismo restaurante.
        available_products = [
            product_id
            for product_id in product_ids_by_restaurant[restaurant_id]
            if product_id not in selected_combo
        ]

        third_product = available_products[
            (order_id * 11) % len(available_products)
        ]

        selected_products = [
            selected_combo[0],
            selected_combo[1],
            third_product,
        ]

        # Distribuye los pedidos a lo largo de aproximadamente 18 meses.
        progress = (order_id - 1) / (TOTAL_ORDERS - 1)
        day_offset = int((progress ** 0.72) * 539)

        hour = ORDER_HOURS[(order_id - 1) % len(ORDER_HOURS)]
        minute = (order_id * 7) % 60

        order_date = base_date + timedelta(
            days=day_offset,
            hours=hour,
            minutes=minute,
        )

        current_items: list[dict[str, Any]] = []
        total_amount = 0

        for position, product_id in enumerate(selected_products, start=1):
            product = menu_by_id[product_id]
            unit_price = int(float(product["price"]))

            # Produce cantidades 1 o 2 de manera determinista.
            quantity = 1 + ((order_id + position) % 2)
            subtotal = unit_price * quantity
            total_amount += subtotal

            current_items.append(
                {
                    "id": next_item_id,
                    "order_id": order_id,
                    "menu_id": product_id,
                    "quantity": quantity,
                    "unit_price": unit_price,
                    "subtotal": subtotal,
                }
            )

            next_item_id += 1

        orders.append(
            {
                "id": order_id,
                "user_id": user_id,
                "restaurant_id": restaurant_id,
                "order_date": order_date.isoformat(timespec="seconds"),
                "total_amount": total_amount,
                "status": get_order_status(order_id),
                "delivery_location_id": int(user["location_id"]),
            }
        )

        order_items.extend(current_items)

    validate_generated_data(
        orders=orders,
        order_items=order_items,
        menu_by_id=menu_by_id,
    )

    write_csv(
        "orders.csv",
        [
            "id",
            "user_id",
            "restaurant_id",
            "order_date",
            "total_amount",
            "status",
            "delivery_location_id",
        ],
        orders,
    )

    write_csv(
        "order_items.csv",
        [
            "id",
            "order_id",
            "menu_id",
            "quantity",
            "unit_price",
            "subtotal",
        ],
        order_items,
    )

    status_counts = Counter(order["status"] for order in orders)

    print("Distribución de estados:")

    for status, total in sorted(status_counts.items()):
        print(f"- {status}: {total}")

    print("Validaciones internas completadas correctamente.")


if __name__ == "__main__":
    main()