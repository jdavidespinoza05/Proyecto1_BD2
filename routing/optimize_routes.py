from __future__ import annotations

import csv
import heapq
from collections import defaultdict
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parents[1]
RAW_DATA_DIR = ROOT_DIR / "spark" / "data" / "raw_data"
OUTPUT_DIR = ROOT_DIR / "routing" / "output"

RETURN_TO_START = True


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


def write_csv(
    filename: str,
    fieldnames: list[str],
    rows: list[dict[str, Any]],
) -> None:
    """Write a generated routing result."""
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    output_path = OUTPUT_DIR / filename

    with output_path.open(
        mode="w",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        writer = csv.DictWriter(
            csv_file,
            fieldnames=fieldnames,
        )
        writer.writeheader()
        writer.writerows(rows)

    print(f"{filename}: {len(rows)} registros generados")


def build_location_graph(
    connections: list[dict[str, str]],
) -> dict[int, list[tuple[int, float, int]]]:
    """
    Build an undirected weighted graph.

    Each entry contains:
    destination, distance in kilometers, estimated minutes.
    """
    graph: dict[int, list[tuple[int, float, int]]] = defaultdict(list)

    for connection in connections:
        origin_id = int(connection["origin_location_id"])
        destination_id = int(connection["destination_location_id"])
        distance_km = float(connection["distance_km"])
        estimated_minutes = int(connection["estimated_minutes"])

        if origin_id == destination_id:
            raise ValueError(
                f"Conexión inválida de la ubicación {origin_id} consigo misma."
            )

        if distance_km <= 0 or estimated_minutes <= 0:
            raise ValueError(
                "Las conexiones deben tener distancia y tiempo positivos."
            )

        graph[origin_id].append(
            (
                destination_id,
                distance_km,
                estimated_minutes,
            )
        )

        graph[destination_id].append(
            (
                origin_id,
                distance_km,
                estimated_minutes,
            )
        )

    return graph


def shortest_path(
    graph: dict[int, list[tuple[int, float, int]]],
    origin_id: int,
    destination_id: int,
) -> tuple[list[int], float, int]:
    """
    Find the minimum-distance path using Dijkstra's algorithm.

    Distance is the primary weight and time is used as a
    deterministic secondary criterion.
    """
    if origin_id == destination_id:
        return [origin_id], 0.0, 0

    best_cost: dict[int, tuple[float, int]] = {
        origin_id: (0.0, 0)
    }

    previous_location: dict[int, int] = {}

    pending: list[tuple[float, int, int]] = [
        (0.0, 0, origin_id)
    ]

    while pending:
        (
            current_distance,
            current_minutes,
            current_id,
        ) = heapq.heappop(pending)

        current_cost = (
            current_distance,
            current_minutes,
        )

        if current_cost != best_cost.get(current_id):
            continue

        if current_id == destination_id:
            break

        for (
            neighbor_id,
            edge_distance,
            edge_minutes,
        ) in graph.get(current_id, []):
            candidate_distance = (
                current_distance + edge_distance
            )

            candidate_minutes = (
                current_minutes + edge_minutes
            )

            candidate_cost = (
                candidate_distance,
                candidate_minutes,
            )

            current_best = best_cost.get(neighbor_id)

            if (
                current_best is None
                or candidate_cost < current_best
            ):
                best_cost[neighbor_id] = candidate_cost
                previous_location[neighbor_id] = current_id

                heapq.heappush(
                    pending,
                    (
                        candidate_distance,
                        candidate_minutes,
                        neighbor_id,
                    ),
                )

    if destination_id not in best_cost:
        raise ValueError(
            f"No existe una ruta entre "
            f"{origin_id} y {destination_id}."
        )

    path = [destination_id]
    current_id = destination_id

    while current_id != origin_id:
        current_id = previous_location[current_id]
        path.append(current_id)

    path.reverse()

    total_distance, total_minutes = best_cost[
        destination_id
    ]

    return path, round(total_distance, 2), total_minutes


def calculate_sequence_metrics(
    graph: dict[int, list[tuple[int, float, int]]],
    start_location_id: int,
    destination_sequence: list[int],
    return_to_start: bool,
) -> tuple[float, int]:
    """Calculate distance and time for a location sequence."""
    current_location_id = start_location_id
    total_distance = 0.0
    total_minutes = 0

    for destination_id in destination_sequence:
        _, distance_km, estimated_minutes = shortest_path(
            graph,
            current_location_id,
            destination_id,
        )

        total_distance += distance_km
        total_minutes += estimated_minutes
        current_location_id = destination_id

    if (
        return_to_start
        and destination_sequence
        and current_location_id != start_location_id
    ):
        _, distance_km, estimated_minutes = shortest_path(
            graph,
            current_location_id,
            start_location_id,
        )

        total_distance += distance_km
        total_minutes += estimated_minutes

    return round(total_distance, 2), total_minutes


def optimize_driver_route(
    route_id: int,
    driver: dict[str, str],
    assignments: list[dict[str, str]],
    graph: dict[int, list[tuple[int, float, int]]],
    locations_by_id: dict[int, dict[str, str]],
) -> tuple[dict[str, Any], list[dict[str, Any]]]:
    """
    Optimize one driver's route using nearest neighbor.

    Orders going to the same location are grouped into one stop.
    """
    driver_id = int(driver["id"])
    start_location_id = int(driver["start_location_id"])

    orders_by_location: dict[int, list[int]] = defaultdict(list)

    for assignment in assignments:
        delivery_location_id = int(
            assignment["delivery_location_id"]
        )

        orders_by_location[delivery_location_id].append(
            int(assignment["order_id"])
        )

    if not orders_by_location:
        raise ValueError(
            f"El repartidor {driver_id} no tiene pedidos asignados."
        )

    # Ruta base: ubicación de cada pedido según el orden original
    # de asignación. Puede repetir ubicaciones.
    baseline_sequence = [
        int(assignment["delivery_location_id"])
        for assignment in assignments
    ]

    baseline_distance_km, baseline_minutes = (
        calculate_sequence_metrics(
            graph=graph,
            start_location_id=start_location_id,
            destination_sequence=baseline_sequence,
            return_to_start=RETURN_TO_START,
        )
    )

    unvisited_locations = set(orders_by_location.keys())
    current_location_id = start_location_id

    cumulative_distance = 0.0
    cumulative_minutes = 0
    stop_sequence = 1

    route_stops: list[dict[str, Any]] = []

    while unvisited_locations:
        candidates: list[
            tuple[float, int, int, list[int]]
        ] = []

        for destination_id in sorted(unvisited_locations):
            path, distance_km, estimated_minutes = shortest_path(
                graph,
                current_location_id,
                destination_id,
            )

            candidates.append(
                (
                    distance_km,
                    estimated_minutes,
                    destination_id,
                    path,
                )
            )

        (
            leg_distance_km,
            leg_estimated_minutes,
            selected_location_id,
            selected_path,
        ) = min(candidates)

        cumulative_distance += leg_distance_km
        cumulative_minutes += leg_estimated_minutes

        order_ids = sorted(
            orders_by_location[selected_location_id]
        )

        route_stops.append(
            {
                "route_id": route_id,
                "driver_id": driver_id,
                "driver_name": driver["name"],
                "stop_sequence": stop_sequence,
                "stop_type": "DELIVERY",
                "origin_location_id": current_location_id,
                "origin_location_name": locations_by_id[
                    current_location_id
                ]["name"],
                "destination_location_id": selected_location_id,
                "destination_location_name": locations_by_id[
                    selected_location_id
                ]["name"],
                "order_ids": "|".join(
                    str(order_id)
                    for order_id in order_ids
                ),
                "orders_count": len(order_ids),
                "leg_distance_km": round(
                    leg_distance_km,
                    2,
                ),
                "leg_estimated_minutes": leg_estimated_minutes,
                "cumulative_distance_km": round(
                    cumulative_distance,
                    2,
                ),
                "cumulative_estimated_minutes": cumulative_minutes,
                "path_location_ids": "|".join(
                    str(location_id)
                    for location_id in selected_path
                ),
                "path_location_names": " -> ".join(
                    locations_by_id[location_id]["name"]
                    for location_id in selected_path
                ),
            }
        )

        current_location_id = selected_location_id
        unvisited_locations.remove(selected_location_id)
        stop_sequence += 1

    if (
        RETURN_TO_START
        and current_location_id != start_location_id
    ):
        (
            return_path,
            return_distance_km,
            return_estimated_minutes,
        ) = shortest_path(
            graph,
            current_location_id,
            start_location_id,
        )

        cumulative_distance += return_distance_km
        cumulative_minutes += return_estimated_minutes

        route_stops.append(
            {
                "route_id": route_id,
                "driver_id": driver_id,
                "driver_name": driver["name"],
                "stop_sequence": stop_sequence,
                "stop_type": "RETURN",
                "origin_location_id": current_location_id,
                "origin_location_name": locations_by_id[
                    current_location_id
                ]["name"],
                "destination_location_id": start_location_id,
                "destination_location_name": locations_by_id[
                    start_location_id
                ]["name"],
                "order_ids": "",
                "orders_count": 0,
                "leg_distance_km": round(
                    return_distance_km,
                    2,
                ),
                "leg_estimated_minutes": (
                    return_estimated_minutes
                ),
                "cumulative_distance_km": round(
                    cumulative_distance,
                    2,
                ),
                "cumulative_estimated_minutes": cumulative_minutes,
                "path_location_ids": "|".join(
                    str(location_id)
                    for location_id in return_path
                ),
                "path_location_names": " -> ".join(
                    locations_by_id[location_id]["name"]
                    for location_id in return_path
                ),
            }
        )

    optimized_distance_km = round(cumulative_distance, 2)
    distance_saved_km = round(
        baseline_distance_km - optimized_distance_km,
        2,
    )

    improvement_percentage = (
        round(
            (distance_saved_km / baseline_distance_km) * 100,
            2,
        )
        if baseline_distance_km > 0
        else 0.0
    )

    route_summary = {
        "route_id": route_id,
        "driver_id": driver_id,
        "driver_name": driver["name"],
        "restaurant_id": int(driver["restaurant_id"]),
        "start_location_id": start_location_id,
        "start_location_name": locations_by_id[
            start_location_id
        ]["name"],
        "delivery_stops": len(orders_by_location),
        "total_orders": len(assignments),
        "baseline_distance_km": baseline_distance_km,
        "baseline_estimated_minutes": baseline_minutes,
        "optimized_distance_km": optimized_distance_km,
        "optimized_estimated_minutes": cumulative_minutes,
        "distance_saved_km": distance_saved_km,
        "improvement_percentage": improvement_percentage,
        "return_to_start": RETURN_TO_START,
        "algorithm": "Nearest Neighbor + Dijkstra",
    }

    return route_summary, route_stops


def validate_results(
    assignments: list[dict[str, str]],
    route_summaries: list[dict[str, Any]],
    route_stops: list[dict[str, Any]],
) -> None:
    """Validate that all assigned orders are routed exactly once."""
    assigned_order_ids = {
        int(assignment["order_id"])
        for assignment in assignments
    }

    routed_order_ids: list[int] = []

    for stop in route_stops:
        if stop["stop_type"] != "DELIVERY":
            continue

        raw_order_ids = str(stop["order_ids"]).strip()

        if not raw_order_ids:
            raise ValueError(
                "Se encontró una parada de entrega sin pedidos."
            )

        routed_order_ids.extend(
            int(order_id)
            for order_id in raw_order_ids.split("|")
        )

    if len(routed_order_ids) != len(set(routed_order_ids)):
        raise ValueError(
            "Un pedido aparece en más de una parada de entrega."
        )

    if set(routed_order_ids) != assigned_order_ids:
        missing_orders = assigned_order_ids - set(routed_order_ids)
        unexpected_orders = set(routed_order_ids) - assigned_order_ids

        raise ValueError(
            "Las rutas no coinciden con las asignaciones. "
            f"Faltantes: {sorted(missing_orders)}. "
            f"Inesperados: {sorted(unexpected_orders)}."
        )

    if len(route_summaries) != 6:
        raise ValueError(
            f"Se esperaban 6 rutas y se generaron "
            f"{len(route_summaries)}."
        )

    for summary in route_summaries:
        if int(summary["total_orders"]) <= 0:
            raise ValueError(
                f"La ruta {summary['route_id']} no contiene pedidos."
            )

        if float(summary["optimized_distance_km"]) <= 0:
            raise ValueError(
                f"La ruta {summary['route_id']} tiene distancia inválida."
            )

        if int(summary["optimized_estimated_minutes"]) <= 0:
            raise ValueError(
                f"La ruta {summary['route_id']} tiene tiempo inválido."
            )


def main() -> None:
    drivers = read_csv("drivers.csv")
    assignments = read_csv("delivery_assignments.csv")
    locations = read_csv("locations.csv")
    connections = read_csv("location_connections.csv")

    active_drivers = [
        driver
        for driver in drivers
        if driver["active"].strip().lower()
        in {"true", "1", "yes", "si", "sí"}
    ]

    locations_by_id = {
        int(location["id"]): location
        for location in locations
    }

    graph = build_location_graph(connections)

    assignments_by_driver: dict[
        int,
        list[dict[str, str]],
    ] = defaultdict(list)

    for assignment in assignments:
        assignments_by_driver[
            int(assignment["driver_id"])
        ].append(assignment)

    for driver_assignments in assignments_by_driver.values():
        driver_assignments.sort(
            key=lambda assignment: int(assignment["id"])
        )

    route_summaries: list[dict[str, Any]] = []
    route_stops: list[dict[str, Any]] = []

    for route_id, driver in enumerate(
        sorted(
            active_drivers,
            key=lambda current_driver: int(
                current_driver["id"]
            ),
        ),
        start=1,
    ):
        driver_id = int(driver["id"])
        driver_assignments = assignments_by_driver.get(
            driver_id,
            [],
        )

        route_summary, driver_route_stops = (
            optimize_driver_route(
                route_id=route_id,
                driver=driver,
                assignments=driver_assignments,
                graph=graph,
                locations_by_id=locations_by_id,
            )
        )

        route_summaries.append(route_summary)
        route_stops.extend(driver_route_stops)

    validate_results(
        assignments=assignments,
        route_summaries=route_summaries,
        route_stops=route_stops,
    )

    write_csv(
        "delivery_routes.csv",
        [
            "route_id",
            "driver_id",
            "driver_name",
            "restaurant_id",
            "start_location_id",
            "start_location_name",
            "delivery_stops",
            "total_orders",
            "baseline_distance_km",
            "baseline_estimated_minutes",
            "optimized_distance_km",
            "optimized_estimated_minutes",
            "distance_saved_km",
            "improvement_percentage",
            "return_to_start",
            "algorithm",
        ],
        route_summaries,
    )

    write_csv(
        "delivery_route_stops.csv",
        [
            "route_id",
            "driver_id",
            "driver_name",
            "stop_sequence",
            "stop_type",
            "origin_location_id",
            "origin_location_name",
            "destination_location_id",
            "destination_location_name",
            "order_ids",
            "orders_count",
            "leg_distance_km",
            "leg_estimated_minutes",
            "cumulative_distance_km",
            "cumulative_estimated_minutes",
            "path_location_ids",
            "path_location_names",
        ],
        route_stops,
    )

    print()
    print("Resumen de rutas optimizadas:")

    for summary in route_summaries:
        print(
            f"- {summary['driver_name']}: "
            f"{summary['total_orders']} pedidos, "
            f"{summary['delivery_stops']} paradas, "
            f"{summary['optimized_distance_km']} km, "
            f"{summary['optimized_estimated_minutes']} min, "
            f"ahorro {summary['improvement_percentage']}%"
        )

    print()
    print(
        "Todos los pedidos asignados aparecen exactamente "
        "una vez en las rutas."
    )
    print("Optimización completada correctamente.")


if __name__ == "__main__":
    main()