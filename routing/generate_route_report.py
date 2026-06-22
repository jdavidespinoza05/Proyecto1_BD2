from __future__ import annotations

import csv
from collections import defaultdict
from html import escape
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parents[1]
RAW_DATA_DIR = ROOT_DIR / "spark" / "data" / "raw_data"
ROUTING_OUTPUT_DIR = ROOT_DIR / "routing" / "output"

REPORT_PATH = ROUTING_OUTPUT_DIR / "delivery_routes_report.html"


def read_csv(path: Path) -> list[dict[str, str]]:
    """Read a UTF-8 CSV file."""
    if not path.exists():
        raise FileNotFoundError(
            f"No se encontró el archivo requerido: {path}"
        )

    with path.open(
        mode="r",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        return list(csv.DictReader(csv_file))


def format_number(value: float, decimals: int = 2) -> str:
    """Format a numeric value for the HTML report."""
    return f"{value:,.{decimals}f}"


def validate_data(
    routes: list[dict[str, str]],
    stops: list[dict[str, str]],
) -> None:
    """Validate the consistency of routing output files."""
    if not routes:
        raise ValueError(
            "delivery_routes.csv no contiene rutas."
        )

    route_ids = [
        int(route["route_id"])
        for route in routes
    ]

    if len(route_ids) != len(set(route_ids)):
        raise ValueError(
            "delivery_routes.csv contiene route_id duplicados."
        )

    valid_route_ids = set(route_ids)

    for stop in stops:
        route_id = int(stop["route_id"])

        if route_id not in valid_route_ids:
            raise ValueError(
                f"La parada pertenece a una ruta inexistente: {route_id}."
            )

    expected_orders = sum(
        int(route["total_orders"])
        for route in routes
    )

    routed_orders = sum(
        int(stop["orders_count"])
        for stop in stops
        if stop["stop_type"] == "DELIVERY"
    )

    if expected_orders != routed_orders:
        raise ValueError(
            "La cantidad de pedidos de las rutas no coincide "
            "con las paradas de entrega. "
            f"Esperados: {expected_orders}. "
            f"Enrutados: {routed_orders}."
        )

    for route in routes:
        baseline_distance = float(
            route["baseline_distance_km"]
        )

        optimized_distance = float(
            route["optimized_distance_km"]
        )

        if baseline_distance <= 0:
            raise ValueError(
                f"La ruta {route['route_id']} tiene una "
                "distancia base inválida."
            )

        if optimized_distance <= 0:
            raise ValueError(
                f"La ruta {route['route_id']} tiene una "
                "distancia optimizada inválida."
            )


def build_route_sequence(
    route: dict[str, str],
    route_stops: list[dict[str, str]],
) -> list[str]:
    """Build the ordered list of main route locations."""
    sequence = [route["start_location_name"]]

    for stop in route_stops:
        sequence.append(
            stop["destination_location_name"]
        )

    return sequence


def build_route_svg(
    route: dict[str, str],
    route_stops: list[dict[str, str]],
    locations_by_id: dict[int, dict[str, str]],
) -> str:
    """
    Create a simple standalone SVG map from the route coordinates.

    No external JavaScript or map provider is required.
    """
    location_sequence = [
        int(route["start_location_id"])
    ]

    for stop in route_stops:
        location_sequence.append(
            int(stop["destination_location_id"])
        )

    points: list[dict[str, Any]] = []

    for sequence_number, location_id in enumerate(
        location_sequence
    ):
        location = locations_by_id.get(location_id)

        if location is None:
            raise ValueError(
                f"No se encontró la ubicación {location_id}."
            )

        points.append(
            {
                "id": location_id,
                "name": location["name"],
                "latitude": float(location["latitude"]),
                "longitude": float(location["longitude"]),
                "sequence": sequence_number,
            }
        )

    width = 760
    height = 300
    padding = 45

    latitudes = [
        point["latitude"]
        for point in points
    ]

    longitudes = [
        point["longitude"]
        for point in points
    ]

    minimum_latitude = min(latitudes)
    maximum_latitude = max(latitudes)
    minimum_longitude = min(longitudes)
    maximum_longitude = max(longitudes)

    latitude_range = max(
        maximum_latitude - minimum_latitude,
        0.001,
    )

    longitude_range = max(
        maximum_longitude - minimum_longitude,
        0.001,
    )

    def project(point: dict[str, Any]) -> tuple[float, float]:
        x = padding + (
            (
                point["longitude"] - minimum_longitude
            )
            / longitude_range
        ) * (width - (padding * 2))

        y = height - padding - (
            (
                point["latitude"] - minimum_latitude
            )
            / latitude_range
        ) * (height - (padding * 2))

        return x, y

    projected_points = [
        project(point)
        for point in points
    ]

    polyline_points = " ".join(
        f"{x:.1f},{y:.1f}"
        for x, y in projected_points
    )

    point_elements: list[str] = []

    for index, (point, coordinates) in enumerate(
        zip(points, projected_points)
    ):
        x, y = coordinates

        if index == 0:
            marker_class = "route-start"
            marker_text = "I"
        elif index == len(points) - 1 and point["id"] == points[0]["id"]:
            marker_class = "route-return"
            marker_text = "R"
        else:
            marker_class = "route-stop"
            marker_text = str(index)

        point_elements.append(
            f"""
            <g class="{marker_class}">
                <circle cx="{x:.1f}" cy="{y:.1f}" r="15"></circle>
                <text
                    x="{x:.1f}"
                    y="{y + 5:.1f}"
                    text-anchor="middle"
                >{escape(marker_text)}</text>
                <title>{escape(point["name"])}</title>
            </g>
            """
        )

    return f"""
    <svg
        class="route-map"
        viewBox="0 0 {width} {height}"
        role="img"
        aria-label="Diagrama de la ruta de {escape(route['driver_name'])}"
    >
        <rect
            x="0"
            y="0"
            width="{width}"
            height="{height}"
            rx="16"
            class="map-background"
        ></rect>

        <polyline
            points="{polyline_points}"
            class="route-line"
        ></polyline>

        {''.join(point_elements)}
    </svg>
    """


def build_stop_rows(
    route_stops: list[dict[str, str]],
) -> str:
    """Create HTML rows for one driver's route stops."""
    rows: list[str] = []

    for stop in route_stops:
        stop_type = stop["stop_type"]

        if stop_type == "DELIVERY":
            type_text = "Entrega"
            type_class = "status-delivery"
        else:
            type_text = "Regreso"
            type_class = "status-return"

        order_ids = (
            stop["order_ids"].replace("|", ", ")
            if stop["order_ids"]
            else "—"
        )

        rows.append(
            f"""
            <tr>
                <td>{escape(stop["stop_sequence"])}</td>
                <td>
                    <span class="status {type_class}">
                        {type_text}
                    </span>
                </td>
                <td>{escape(stop["origin_location_name"])}</td>
                <td>{escape(stop["destination_location_name"])}</td>
                <td>{escape(order_ids)}</td>
                <td>{escape(stop["orders_count"])}</td>
                <td>{format_number(float(stop["leg_distance_km"]))} km</td>
                <td>{escape(stop["leg_estimated_minutes"])} min</td>
                <td>{format_number(float(stop["cumulative_distance_km"]))} km</td>
            </tr>
            """
        )

    return "".join(rows)


def build_route_section(
    route: dict[str, str],
    route_stops: list[dict[str, str]],
    locations_by_id: dict[int, dict[str, str]],
) -> str:
    """Create one complete driver section."""
    route_sequence = build_route_sequence(
        route,
        route_stops,
    )

    sequence_html = "".join(
        f"""
        <li>
            <span class="sequence-number">{index}</span>
            <span>{escape(location_name)}</span>
        </li>
        """
        for index, location_name in enumerate(route_sequence)
    )

    route_svg = build_route_svg(
        route=route,
        route_stops=route_stops,
        locations_by_id=locations_by_id,
    )

    stop_rows = build_stop_rows(route_stops)

    improvement = float(
        route["improvement_percentage"]
    )

    improvement_class = (
        "positive"
        if improvement >= 0
        else "negative"
    )

    return f"""
    <section class="route-card">
        <div class="route-header">
            <div>
                <p class="eyebrow">
                    Ruta {escape(route["route_id"])}
                </p>

                <h2>{escape(route["driver_name"])}</h2>

                <p class="route-subtitle">
                    Inicio: {escape(route["start_location_name"])}
                    · Algoritmo: {escape(route["algorithm"])}
                </p>
            </div>

            <div class="route-badge">
                {escape(route["total_orders"])} pedidos
            </div>
        </div>

        <div class="route-metrics">
            <div class="metric">
                <span>Paradas</span>
                <strong>{escape(route["delivery_stops"])}</strong>
            </div>

            <div class="metric">
                <span>Distancia base</span>
                <strong>
                    {format_number(float(route["baseline_distance_km"]))} km
                </strong>
            </div>

            <div class="metric">
                <span>Distancia optimizada</span>
                <strong>
                    {format_number(float(route["optimized_distance_km"]))} km
                </strong>
            </div>

            <div class="metric">
                <span>Tiempo estimado</span>
                <strong>
                    {escape(route["optimized_estimated_minutes"])} min
                </strong>
            </div>

            <div class="metric">
                <span>Distancia ahorrada</span>
                <strong>
                    {format_number(float(route["distance_saved_km"]))} km
                </strong>
            </div>

            <div class="metric">
                <span>Mejora</span>
                <strong class="{improvement_class}">
                    {format_number(improvement)}%
                </strong>
            </div>
        </div>

        <div class="route-visualization">
            <div>
                <h3>Diagrama de la ruta</h3>
                {route_svg}

                <div class="map-legend">
                    <span><b class="legend-start"></b> Inicio</span>
                    <span><b class="legend-stop"></b> Entrega</span>
                    <span><b class="legend-return"></b> Regreso</span>
                </div>
            </div>

            <div>
                <h3>Orden de recorrido</h3>
                <ol class="route-sequence">
                    {sequence_html}
                </ol>
            </div>
        </div>

        <div class="table-wrapper">
            <table>
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Tipo</th>
                        <th>Origen</th>
                        <th>Destino</th>
                        <th>Pedidos</th>
                        <th>Cantidad</th>
                        <th>Distancia</th>
                        <th>Tiempo</th>
                        <th>Acumulado</th>
                    </tr>
                </thead>

                <tbody>
                    {stop_rows}
                </tbody>
            </table>
        </div>
    </section>
    """


def main() -> None:
    routes = read_csv(
        ROUTING_OUTPUT_DIR / "delivery_routes.csv"
    )

    stops = read_csv(
        ROUTING_OUTPUT_DIR / "delivery_route_stops.csv"
    )

    locations = read_csv(
        RAW_DATA_DIR / "locations.csv"
    )

    validate_data(routes, stops)

    locations_by_id = {
        int(location["id"]): location
        for location in locations
    }

    stops_by_route: dict[int, list[dict[str, str]]] = (
        defaultdict(list)
    )

    for stop in stops:
        stops_by_route[int(stop["route_id"])].append(
            stop
        )

    for route_stops in stops_by_route.values():
        route_stops.sort(
            key=lambda stop: int(stop["stop_sequence"])
        )

    sorted_routes = sorted(
        routes,
        key=lambda route: int(route["route_id"]),
    )

    total_routes = len(sorted_routes)

    total_orders = sum(
        int(route["total_orders"])
        for route in sorted_routes
    )

    total_delivery_stops = sum(
        int(route["delivery_stops"])
        for route in sorted_routes
    )

    total_baseline_distance = sum(
        float(route["baseline_distance_km"])
        for route in sorted_routes
    )

    total_optimized_distance = sum(
        float(route["optimized_distance_km"])
        for route in sorted_routes
    )

    total_distance_saved = (
        total_baseline_distance
        - total_optimized_distance
    )

    global_improvement = (
        (
            total_distance_saved
            / total_baseline_distance
        )
        * 100
        if total_baseline_distance > 0
        else 0
    )

    total_estimated_minutes = sum(
        int(route["optimized_estimated_minutes"])
        for route in sorted_routes
    )

    route_sections = "".join(
        build_route_section(
            route=route,
            route_stops=stops_by_route[
                int(route["route_id"])
            ],
            locations_by_id=locations_by_id,
        )
        for route in sorted_routes
    )

    html_content = f"""<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta
        name="viewport"
        content="width=device-width, initial-scale=1.0"
    >

    <title>Reporte de rutas optimizadas</title>

    <style>
        :root {{
            --background: #f4f6f8;
            --surface: #ffffff;
            --surface-alt: #f8fafc;
            --text: #18212f;
            --muted: #667085;
            --border: #dfe4ea;
            --primary: #2563eb;
            --primary-soft: #dbeafe;
            --success: #15803d;
            --success-soft: #dcfce7;
            --warning: #b45309;
            --warning-soft: #fef3c7;
            --danger: #b91c1c;
            --shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
        }}

        * {{
            box-sizing: border-box;
        }}

        body {{
            margin: 0;
            background: var(--background);
            color: var(--text);
            font-family:
                Inter,
                Arial,
                Helvetica,
                sans-serif;
            line-height: 1.5;
        }}

        .container {{
            width: min(1500px, calc(100% - 40px));
            margin: 0 auto;
            padding: 40px 0 70px;
        }}

        .hero {{
            padding: 34px;
            border-radius: 22px;
            background:
                linear-gradient(
                    135deg,
                    #172554,
                    #1d4ed8
                );
            color: white;
            box-shadow: var(--shadow);
        }}

        .hero h1 {{
            margin: 5px 0 10px;
            font-size: clamp(30px, 4vw, 48px);
            line-height: 1.1;
        }}

        .hero p {{
            max-width: 850px;
            margin: 0;
            color: #dbeafe;
        }}

        .eyebrow {{
            margin: 0;
            font-weight: 700;
            letter-spacing: 0.12em;
            text-transform: uppercase;
            font-size: 12px;
        }}

        .summary-grid {{
            display: grid;
            grid-template-columns:
                repeat(auto-fit, minmax(180px, 1fr));
            gap: 16px;
            margin: 24px 0 36px;
        }}

        .summary-card {{
            padding: 22px;
            border-radius: 16px;
            background: var(--surface);
            box-shadow: var(--shadow);
        }}

        .summary-card span {{
            display: block;
            color: var(--muted);
            font-size: 14px;
            margin-bottom: 8px;
        }}

        .summary-card strong {{
            display: block;
            font-size: 27px;
        }}

        .route-card {{
            margin-top: 28px;
            padding: 28px;
            border-radius: 20px;
            background: var(--surface);
            box-shadow: var(--shadow);
        }}

        .route-header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            gap: 20px;
            margin-bottom: 24px;
        }}

        .route-header h2 {{
            margin: 4px 0;
            font-size: 29px;
        }}

        .route-subtitle {{
            margin: 0;
            color: var(--muted);
        }}

        .route-badge {{
            white-space: nowrap;
            padding: 10px 16px;
            border-radius: 999px;
            background: var(--primary-soft);
            color: var(--primary);
            font-weight: 700;
        }}

        .route-metrics {{
            display: grid;
            grid-template-columns:
                repeat(auto-fit, minmax(150px, 1fr));
            gap: 12px;
            margin-bottom: 26px;
        }}

        .metric {{
            padding: 16px;
            border: 1px solid var(--border);
            border-radius: 13px;
            background: var(--surface-alt);
        }}

        .metric span {{
            display: block;
            color: var(--muted);
            font-size: 13px;
        }}

        .metric strong {{
            display: block;
            margin-top: 4px;
            font-size: 19px;
        }}

        .positive {{
            color: var(--success);
        }}

        .negative {{
            color: var(--danger);
        }}

        .route-visualization {{
            display: grid;
            grid-template-columns: minmax(0, 2fr) minmax(280px, 1fr);
            gap: 24px;
            align-items: start;
            margin-bottom: 26px;
        }}

        h3 {{
            margin-top: 0;
        }}

        .route-map {{
            display: block;
            width: 100%;
            border: 1px solid var(--border);
            border-radius: 16px;
        }}

        .map-background {{
            fill: #f8fafc;
        }}

        .route-line {{
            fill: none;
            stroke: var(--primary);
            stroke-width: 5;
            stroke-linecap: round;
            stroke-linejoin: round;
            stroke-dasharray: 10 7;
        }}

        .route-start circle {{
            fill: var(--success);
        }}

        .route-stop circle {{
            fill: var(--primary);
        }}

        .route-return circle {{
            fill: var(--warning);
        }}

        .route-start text,
        .route-stop text,
        .route-return text {{
            fill: white;
            font-weight: bold;
            font-size: 12px;
        }}

        .map-legend {{
            display: flex;
            flex-wrap: wrap;
            gap: 18px;
            margin-top: 12px;
            color: var(--muted);
            font-size: 13px;
        }}

        .map-legend b {{
            display: inline-block;
            width: 11px;
            height: 11px;
            margin-right: 5px;
            border-radius: 50%;
        }}

        .legend-start {{
            background: var(--success);
        }}

        .legend-stop {{
            background: var(--primary);
        }}

        .legend-return {{
            background: var(--warning);
        }}

        .route-sequence {{
            list-style: none;
            margin: 0;
            padding: 0;
        }}

        .route-sequence li {{
            position: relative;
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 8px 0;
        }}

        .route-sequence li:not(:last-child)::after {{
            content: "";
            position: absolute;
            left: 14px;
            top: 36px;
            width: 2px;
            height: 15px;
            background: var(--border);
        }}

        .sequence-number {{
            display: inline-grid;
            place-items: center;
            flex: 0 0 30px;
            width: 30px;
            height: 30px;
            border-radius: 50%;
            background: var(--primary);
            color: white;
            font-size: 12px;
            font-weight: bold;
        }}

        .table-wrapper {{
            overflow-x: auto;
            border: 1px solid var(--border);
            border-radius: 14px;
        }}

        table {{
            width: 100%;
            border-collapse: collapse;
            min-width: 1050px;
        }}

        th,
        td {{
            padding: 13px 14px;
            border-bottom: 1px solid var(--border);
            text-align: left;
            font-size: 13px;
        }}

        th {{
            background: var(--surface-alt);
            color: #475467;
            font-weight: 700;
        }}

        tbody tr:last-child td {{
            border-bottom: 0;
        }}

        .status {{
            display: inline-block;
            padding: 4px 9px;
            border-radius: 999px;
            font-size: 12px;
            font-weight: 700;
        }}

        .status-delivery {{
            color: var(--success);
            background: var(--success-soft);
        }}

        .status-return {{
            color: var(--warning);
            background: var(--warning-soft);
        }}

        .methodology {{
            margin-top: 34px;
            padding: 26px;
            border-radius: 18px;
            background: #172554;
            color: white;
        }}

        .methodology p {{
            margin-bottom: 0;
            color: #dbeafe;
        }}

        @media (max-width: 900px) {{
            .route-visualization {{
                grid-template-columns: 1fr;
            }}

            .route-header {{
                flex-direction: column;
            }}
        }}

        @media print {{
            body {{
                background: white;
            }}

            .container {{
                width: 100%;
                padding: 0;
            }}

            .hero,
            .summary-card,
            .route-card,
            .methodology {{
                box-shadow: none;
            }}

            .route-card {{
                break-inside: avoid;
                border: 1px solid var(--border);
            }}
        }}
    </style>
</head>

<body>
    <main class="container">
        <header class="hero">
            <p class="eyebrow">
                Módulo de rutas y logística
            </p>

            <h1>Reporte de rutas optimizadas</h1>

            <p>
                Asignación de pedidos y optimización de entregas
                utilizando la heurística de vecino más cercano
                y caminos mínimos calculados con Dijkstra.
            </p>
        </header>

        <section class="summary-grid">
            <article class="summary-card">
                <span>Rutas generadas</span>
                <strong>{total_routes}</strong>
            </article>

            <article class="summary-card">
                <span>Pedidos asignados</span>
                <strong>{total_orders}</strong>
            </article>

            <article class="summary-card">
                <span>Paradas de entrega</span>
                <strong>{total_delivery_stops}</strong>
            </article>

            <article class="summary-card">
                <span>Distancia original</span>
                <strong>
                    {format_number(total_baseline_distance)} km
                </strong>
            </article>

            <article class="summary-card">
                <span>Distancia optimizada</span>
                <strong>
                    {format_number(total_optimized_distance)} km
                </strong>
            </article>

            <article class="summary-card">
                <span>Distancia ahorrada</span>
                <strong>
                    {format_number(total_distance_saved)} km
                </strong>
            </article>

            <article class="summary-card">
                <span>Mejora global</span>
                <strong>
                    {format_number(global_improvement)}%
                </strong>
            </article>

            <article class="summary-card">
                <span>Tiempo acumulado</span>
                <strong>
                    {total_estimated_minutes} min
                </strong>
            </article>
        </section>

        {route_sections}

        <section class="methodology">
            <h2>Metodología utilizada</h2>

            <p>
                Los pedidos fueron distribuidos entre repartidores
                activos según su restaurante y capacidad. Las
                entregas con la misma ubicación se agruparon en una
                sola parada. Posteriormente, la heurística de vecino
                más cercano seleccionó la siguiente parada pendiente,
                mientras que el algoritmo de Dijkstra calculó el
                camino de menor distancia sobre el grafo de
                ubicaciones. Al finalizar, cada repartidor regresa
                a su ubicación inicial.
            </p>
        </section>
    </main>
</body>
</html>
"""

    ROUTING_OUTPUT_DIR.mkdir(
        parents=True,
        exist_ok=True,
    )

    REPORT_PATH.write_text(
        html_content,
        encoding="utf-8",
    )

    print(
        "delivery_routes_report.html generado correctamente."
    )

    print(f"Rutas incluidas: {total_routes}")
    print(f"Pedidos incluidos: {total_orders}")
    print(
        f"Distancia original: "
        f"{format_number(total_baseline_distance)} km"
    )
    print(
        f"Distancia optimizada: "
        f"{format_number(total_optimized_distance)} km"
    )
    print(
        f"Mejora global: "
        f"{format_number(global_improvement)}%"
    )
    print(f"Reporte: {REPORT_PATH}")


if __name__ == "__main__":
    main()