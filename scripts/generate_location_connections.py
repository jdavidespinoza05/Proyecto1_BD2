from __future__ import annotations

import csv
import math
from collections import defaultdict, deque
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parents[1]
RAW_DATA_DIR = ROOT_DIR / "spark" / "data" / "raw_data"

EARTH_RADIUS_KM = 6371.0
ROAD_DISTANCE_FACTOR = 1.18
AVERAGE_SPEED_KMH = 35.0
FIXED_DELAY_MINUTES = 3
NEIGHBORS_PER_LOCATION = 4


def read_locations() -> list[dict[str, Any]]:
    """Read and validate locations from the shared raw-data folder."""
    input_path = RAW_DATA_DIR / "locations.csv"

    if not input_path.exists():
        raise FileNotFoundError(
            f"No se encontró el archivo requerido: {input_path}"
        )

    locations: list[dict[str, Any]] = []

    with input_path.open(
        mode="r",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        for row in csv.DictReader(csv_file):
            locations.append(
                {
                    "id": int(row["id"]),
                    "name": row["name"],
                    "latitude": float(row["latitude"]),
                    "longitude": float(row["longitude"]),
                }
            )

    if not locations:
        raise ValueError("locations.csv no contiene ubicaciones.")

    location_ids = [location["id"] for location in locations]

    if len(location_ids) != len(set(location_ids)):
        raise ValueError("locations.csv contiene identificadores duplicados.")

    return locations


def haversine_distance(
    first_location: dict[str, Any],
    second_location: dict[str, Any],
) -> float:
    """Calculate straight-line distance between two coordinates."""
    latitude_1 = math.radians(first_location["latitude"])
    longitude_1 = math.radians(first_location["longitude"])
    latitude_2 = math.radians(second_location["latitude"])
    longitude_2 = math.radians(second_location["longitude"])

    latitude_difference = latitude_2 - latitude_1
    longitude_difference = longitude_2 - longitude_1

    haversine_value = (
        math.sin(latitude_difference / 2) ** 2
        + math.cos(latitude_1)
        * math.cos(latitude_2)
        * math.sin(longitude_difference / 2) ** 2
    )

    central_angle = 2 * math.asin(math.sqrt(haversine_value))

    return EARTH_RADIUS_KM * central_angle


def calculate_road_distance(
    first_location: dict[str, Any],
    second_location: dict[str, Any],
) -> float:
    """
    Estimate road distance from straight-line distance.

    The factor represents that real roads are not perfectly straight.
    """
    direct_distance = haversine_distance(
        first_location,
        second_location,
    )

    return round(direct_distance * ROAD_DISTANCE_FACTOR, 2)


def calculate_estimated_minutes(distance_km: float) -> int:
    """Estimate travel time using an average urban driving speed."""
    driving_minutes = (distance_km / AVERAGE_SPEED_KMH) * 60

    return max(
        5,
        round(driving_minutes + FIXED_DELAY_MINUTES),
    )


def validate_connected_graph(
    location_ids: set[int],
    connection_pairs: set[tuple[int, int]],
) -> None:
    """Ensure that every location can be reached from the graph."""
    adjacency: dict[int, set[int]] = defaultdict(set)

    for origin_id, destination_id in connection_pairs:
        adjacency[origin_id].add(destination_id)
        adjacency[destination_id].add(origin_id)

    first_location_id = min(location_ids)
    visited = {first_location_id}
    pending = deque([first_location_id])

    while pending:
        current_location = pending.popleft()

        for neighbor in adjacency[current_location]:
            if neighbor not in visited:
                visited.add(neighbor)
                pending.append(neighbor)

    unreachable_locations = location_ids - visited

    if unreachable_locations:
        raise ValueError(
            "El grafo contiene ubicaciones sin conexión: "
            f"{sorted(unreachable_locations)}"
        )


def main() -> None:
    locations = read_locations()

    locations_by_id = {
        location["id"]: location
        for location in locations
    }

    connection_pairs: set[tuple[int, int]] = set()

    for origin in locations:
        possible_destinations = [
            destination
            for destination in locations
            if destination["id"] != origin["id"]
        ]

        ordered_destinations = sorted(
            possible_destinations,
            key=lambda destination: (
                haversine_distance(origin, destination),
                destination["id"],
            ),
        )

        for destination in ordered_destinations[:NEIGHBORS_PER_LOCATION]:
            pair = tuple(
                sorted(
                    (
                        origin["id"],
                        destination["id"],
                    )
                )
            )

            connection_pairs.add(pair)

    validate_connected_graph(
        location_ids=set(locations_by_id.keys()),
        connection_pairs=connection_pairs,
    )

    rows: list[dict[str, Any]] = []

    for connection_id, pair in enumerate(
        sorted(connection_pairs),
        start=1,
    ):
        origin_id, destination_id = pair

        distance_km = calculate_road_distance(
            locations_by_id[origin_id],
            locations_by_id[destination_id],
        )

        rows.append(
            {
                "id": connection_id,
                "origin_location_id": origin_id,
                "destination_location_id": destination_id,
                "distance_km": distance_km,
                "estimated_minutes": calculate_estimated_minutes(
                    distance_km
                ),
            }
        )

    output_path = RAW_DATA_DIR / "location_connections.csv"

    with output_path.open(
        mode="w",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        writer = csv.DictWriter(
            csv_file,
            fieldnames=[
                "id",
                "origin_location_id",
                "destination_location_id",
                "distance_km",
                "estimated_minutes",
            ],
        )

        writer.writeheader()
        writer.writerows(rows)

    print(
        f"location_connections.csv: {len(rows)} conexiones "
        "bidireccionales lógicas generadas"
    )
    print(
        f"Relaciones dirigidas esperadas en Neo4j: {len(rows) * 2}"
    )
    print("El grafo de ubicaciones está completamente conectado.")


if __name__ == "__main__":
    main()