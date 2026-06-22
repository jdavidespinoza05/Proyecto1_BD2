from __future__ import annotations

import csv
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parents[1]
RAW_DATA_DIR = ROOT_DIR / "spark" / "data" / "raw_data"


DRIVERS: list[dict[str, Any]] = [
    {
        "id": 1,
        "name": "Valeria Campos",
        "capacity": 13,
        "restaurant_id": 3,
        "start_location_id": 3,
        "active": True,
    },
    {
        "id": 2,
        "name": "Luis Chaves",
        "capacity": 13,
        "restaurant_id": 3,
        "start_location_id": 3,
        "active": True,
    },
    {
        "id": 3,
        "name": "Andrea Solano",
        "capacity": 13,
        "restaurant_id": 4,
        "start_location_id": 4,
        "active": True,
    },
    {
        "id": 4,
        "name": "Marco Rojas",
        "capacity": 13,
        "restaurant_id": 4,
        "start_location_id": 4,
        "active": True,
    },
    {
        "id": 5,
        "name": "Daniela Vega",
        "capacity": 13,
        "restaurant_id": 5,
        "start_location_id": 5,
        "active": True,
    },
    {
        "id": 6,
        "name": "Felipe Mora",
        "capacity": 13,
        "restaurant_id": 5,
        "start_location_id": 5,
        "active": True,
    },
]


def read_ids(filename: str) -> set[int]:
    """Read all identifiers from a CSV file."""
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
        return {
            int(row["id"])
            for row in csv.DictReader(csv_file)
        }


def validate_drivers(
    restaurant_ids: set[int],
    location_ids: set[int],
) -> None:
    """Validate references, capacities and unique identifiers."""
    driver_ids: set[int] = set()

    for driver in DRIVERS:
        driver_id = int(driver["id"])
        restaurant_id = int(driver["restaurant_id"])
        start_location_id = int(driver["start_location_id"])
        capacity = int(driver["capacity"])

        if driver_id in driver_ids:
            raise ValueError(
                f"Identificador de repartidor duplicado: {driver_id}"
            )

        if restaurant_id not in restaurant_ids:
            raise ValueError(
                f"El restaurante {restaurant_id} no existe."
            )

        if start_location_id not in location_ids:
            raise ValueError(
                f"La ubicación {start_location_id} no existe."
            )

        if capacity <= 0:
            raise ValueError(
                f"El repartidor {driver_id} tiene capacidad inválida."
            )

        driver_ids.add(driver_id)


def main() -> None:
    restaurant_ids = read_ids("restaurants.csv")
    location_ids = read_ids("locations.csv")

    validate_drivers(
        restaurant_ids=restaurant_ids,
        location_ids=location_ids,
    )

    output_path = RAW_DATA_DIR / "drivers.csv"

    with output_path.open(
        mode="w",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        writer = csv.DictWriter(
            csv_file,
            fieldnames=[
                "id",
                "name",
                "capacity",
                "restaurant_id",
                "start_location_id",
                "active",
            ],
        )

        writer.writeheader()
        writer.writerows(DRIVERS)

    print(f"drivers.csv: {len(DRIVERS)} repartidores generados")
    print("Validación de repartidores completada correctamente.")


if __name__ == "__main__":
    main()