from __future__ import annotations

import csv
import os
from datetime import datetime
from decimal import Decimal
from pathlib import Path
from typing import Any

import psycopg


ROOT_DIR = Path(__file__).resolve().parents[1]
RAW_DATA_DIR = ROOT_DIR / "spark" / "data" / "raw_data"


DATABASE_CONFIG = {
    "host": os.getenv("ANALYTICS_DB_HOST", "localhost"),
    "port": int(os.getenv("ANALYTICS_DB_PORT", "5434")),
    "dbname": os.getenv(
        "ANALYTICS_DB_NAME",
        "analytics_db",
    ),
    "user": os.getenv(
        "ANALYTICS_DB_USER",
        "analytics",
    ),
    "password": os.getenv(
        "ANALYTICS_DB_PASSWORD",
        "analytics_password",
    ),
}


def read_csv(filename: str) -> list[dict[str, str]]:
    """Read one CSV file from the shared raw-data directory."""
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
        rows = list(csv.DictReader(csv_file))

    if not rows:
        raise ValueError(
            f"El archivo {filename} no contiene registros."
        )

    return rows


def parse_decimal(value: str) -> Decimal:
    """Convert a CSV numeric value to Decimal."""
    normalized_value = value.strip()

    if not normalized_value:
        return Decimal("0")

    return Decimal(normalized_value)


def parse_datetime(value: str) -> datetime:
    """Convert an ISO-8601 CSV value to datetime."""
    normalized_value = value.strip()

    if not normalized_value:
        raise ValueError(
            "Se encontró una fecha vacía en orders.csv."
        )

    return datetime.fromisoformat(normalized_value)


def recreate_source_tables(
    cursor: psycopg.Cursor[Any],
) -> None:
    """Recreate the analytical source tables."""

    cursor.execute(
        """
        CREATE SCHEMA IF NOT EXISTS analytics_raw
        """
    )

    cursor.execute(
        """
        CREATE SCHEMA IF NOT EXISTS olap
        """
    )

    # Se utiliza CASCADE porque las vistas OLAP dependen de
    # estas tablas. Al finalizar, las vistas se crean nuevamente.
    cursor.execute(
        """
        DROP TABLE IF EXISTS
            analytics_raw.order_items,
            analytics_raw.orders,
            analytics_raw.menus,
            analytics_raw.users,
            analytics_raw.restaurants,
            analytics_raw.locations
        CASCADE
        """
    )

    cursor.execute(
        """
        CREATE TABLE analytics_raw.locations (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            province TEXT NOT NULL,
            canton TEXT NOT NULL,
            district TEXT NOT NULL,
            latitude NUMERIC(10, 7) NOT NULL,
            longitude NUMERIC(10, 7) NOT NULL,
            location_type TEXT
        )
        """
    )

    cursor.execute(
        """
        CREATE TABLE analytics_raw.users (
            id INTEGER PRIMARY KEY,
            keycloak_id TEXT,
            name TEXT NOT NULL,
            email TEXT NOT NULL,
            role TEXT,
            location_id INTEGER
        )
        """
    )

    cursor.execute(
        """
        CREATE TABLE analytics_raw.restaurants (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            address TEXT,
            phone TEXT,
            location_id INTEGER
        )
        """
    )

    cursor.execute(
        """
        CREATE TABLE analytics_raw.menus (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            description TEXT,
            price NUMERIC(18, 2) NOT NULL,
            category TEXT,
            restaurant_id INTEGER NOT NULL
        )
        """
    )

    cursor.execute(
        """
        CREATE TABLE analytics_raw.orders (
            id INTEGER PRIMARY KEY,
            user_id INTEGER NOT NULL,
            restaurant_id INTEGER NOT NULL,
            order_date TIMESTAMP NOT NULL,
            total_amount NUMERIC(18, 2) NOT NULL,
            status TEXT NOT NULL,
            delivery_location_id INTEGER NOT NULL
        )
        """
    )

    cursor.execute(
        """
        CREATE TABLE analytics_raw.order_items (
            id INTEGER PRIMARY KEY,
            order_id INTEGER NOT NULL,
            menu_id INTEGER NOT NULL,
            quantity INTEGER NOT NULL,
            unit_price NUMERIC(18, 2) NOT NULL,
            subtotal NUMERIC(18, 2) NOT NULL
        )
        """
    )


def load_locations(
    cursor: psycopg.Cursor[Any],
    rows: list[dict[str, str]],
) -> None:
    values = []

    for row in rows:
        location_type = (
            row.get("location_type")
            or row.get("locationType")
            or ""
        )

        values.append(
            (
                int(row["id"]),
                row["name"].strip(),
                row["province"].strip(),
                row["canton"].strip(),
                row["district"].strip(),
                parse_decimal(row["latitude"]),
                parse_decimal(row["longitude"]),
                location_type.strip(),
            )
        )

    cursor.executemany(
        """
        INSERT INTO analytics_raw.locations (
            id,
            name,
            province,
            canton,
            district,
            latitude,
            longitude,
            location_type
        )
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
        """,
        values,
    )


def load_users(
    cursor: psycopg.Cursor[Any],
    rows: list[dict[str, str]],
) -> None:
    values = [
        (
            int(row["id"]),
            row.get("keycloak_id", "").strip(),
            row["name"].strip(),
            row["email"].strip(),
            row.get("role", "").strip(),
            int(row["location_id"]),
        )
        for row in rows
    ]

    cursor.executemany(
        """
        INSERT INTO analytics_raw.users (
            id,
            keycloak_id,
            name,
            email,
            role,
            location_id
        )
        VALUES (%s, %s, %s, %s, %s, %s)
        """,
        values,
    )


def load_restaurants(
    cursor: psycopg.Cursor[Any],
    rows: list[dict[str, str]],
) -> None:
    values = [
        (
            int(row["id"]),
            row["name"].strip(),
            row.get("address", "").strip(),
            row.get("phone", "").strip(),
            int(row["location_id"]),
        )
        for row in rows
    ]

    cursor.executemany(
        """
        INSERT INTO analytics_raw.restaurants (
            id,
            name,
            address,
            phone,
            location_id
        )
        VALUES (%s, %s, %s, %s, %s)
        """,
        values,
    )


def load_menus(
    cursor: psycopg.Cursor[Any],
    rows: list[dict[str, str]],
) -> None:
    values = [
        (
            int(row["id"]),
            row["name"].strip(),
            row.get("description", "").strip(),
            parse_decimal(row["price"]),
            row.get("category", "").strip(),
            int(row["restaurant_id"]),
        )
        for row in rows
    ]

    cursor.executemany(
        """
        INSERT INTO analytics_raw.menus (
            id,
            name,
            description,
            price,
            category,
            restaurant_id
        )
        VALUES (%s, %s, %s, %s, %s, %s)
        """,
        values,
    )


def load_orders(
    cursor: psycopg.Cursor[Any],
    rows: list[dict[str, str]],
) -> None:
    values = [
        (
            int(row["id"]),
            int(row["user_id"]),
            int(row["restaurant_id"]),
            parse_datetime(row["order_date"]),
            parse_decimal(row["total_amount"]),
            row["status"].strip(),
            int(row["delivery_location_id"]),
        )
        for row in rows
    ]

    cursor.executemany(
        """
        INSERT INTO analytics_raw.orders (
            id,
            user_id,
            restaurant_id,
            order_date,
            total_amount,
            status,
            delivery_location_id
        )
        VALUES (%s, %s, %s, %s, %s, %s, %s)
        """,
        values,
    )


def load_order_items(
    cursor: psycopg.Cursor[Any],
    rows: list[dict[str, str]],
) -> None:
    values = [
        (
            int(row["id"]),
            int(row["order_id"]),
            int(row["menu_id"]),
            int(row["quantity"]),
            parse_decimal(row["unit_price"]),
            parse_decimal(row["subtotal"]),
        )
        for row in rows
    ]

    cursor.executemany(
        """
        INSERT INTO analytics_raw.order_items (
            id,
            order_id,
            menu_id,
            quantity,
            unit_price,
            subtotal
        )
        VALUES (%s, %s, %s, %s, %s, %s)
        """,
        values,
    )


def create_indexes(
    cursor: psycopg.Cursor[Any],
) -> None:
    """Create indexes used by the OLAP queries."""

    cursor.execute(
        """
        CREATE INDEX idx_orders_date
        ON analytics_raw.orders (order_date)
        """
    )

    cursor.execute(
        """
        CREATE INDEX idx_orders_status
        ON analytics_raw.orders (status)
        """
    )

    cursor.execute(
        """
        CREATE INDEX idx_orders_user
        ON analytics_raw.orders (user_id)
        """
    )

    cursor.execute(
        """
        CREATE INDEX idx_orders_location
        ON analytics_raw.orders (delivery_location_id)
        """
    )

    cursor.execute(
        """
        CREATE INDEX idx_order_items_order
        ON analytics_raw.order_items (order_id)
        """
    )

    cursor.execute(
        """
        CREATE INDEX idx_order_items_menu
        ON analytics_raw.order_items (menu_id)
        """
    )


def create_olap_views(
    cursor: psycopg.Cursor[Any],
) -> None:
    """Create the five analytical views required by the project."""

    # --------------------------------------------------------
    # 1. Ingresos por mes y categoría de producto
    #
    # Solo se contabilizan pedidos completados porque los
    # cancelados no representan ingresos reales.
    # --------------------------------------------------------

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_ingresos_mes_categoria
        AS
        SELECT
            DATE_TRUNC(
                'month',
                orders.order_date
            )::DATE AS periodo,

            EXTRACT(
                YEAR FROM orders.order_date
            )::INTEGER AS anio,

            EXTRACT(
                MONTH FROM orders.order_date
            )::INTEGER AS mes,

            COALESCE(
                NULLIF(TRIM(menus.category), ''),
                'Sin categoría'
            ) AS categoria,

            ROUND(
                SUM(order_items.subtotal),
                2
            ) AS ingresos_totales,

            COUNT(
                DISTINCT orders.id
            ) AS cantidad_pedidos,

            SUM(
                order_items.quantity
            ) AS unidades_vendidas

        FROM analytics_raw.orders AS orders

        INNER JOIN analytics_raw.order_items AS order_items
            ON order_items.order_id = orders.id

        INNER JOIN analytics_raw.menus AS menus
            ON menus.id = order_items.menu_id

        WHERE orders.status = 'Completado'

        GROUP BY
            DATE_TRUNC(
                'month',
                orders.order_date
            )::DATE,

            EXTRACT(
                YEAR FROM orders.order_date
            )::INTEGER,

            EXTRACT(
                MONTH FROM orders.order_date
            )::INTEGER,

            COALESCE(
                NULLIF(TRIM(menus.category), ''),
                'Sin categoría'
            )
        """
    )

    # --------------------------------------------------------
    # 2. Actividad de clientes por zona geográfica
    # --------------------------------------------------------

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_actividad_clientes_geografia
        AS
        SELECT
            locations.province AS provincia,
            locations.canton AS canton,
            locations.district AS distrito,

            ROUND(
                AVG(locations.latitude),
                6
            ) AS latitud_centro,

            ROUND(
                AVG(locations.longitude),
                6
            ) AS longitud_centro,

            COUNT(
                DISTINCT orders.user_id
            ) AS clientes_activos,

            COUNT(
                orders.id
            ) AS total_pedidos,

            COUNT(orders.id) FILTER (
                WHERE orders.status = 'Completado'
            ) AS pedidos_completados,

            COUNT(orders.id) FILTER (
                WHERE orders.status = 'Cancelado'
            ) AS pedidos_cancelados,

            COUNT(orders.id) FILTER (
                WHERE orders.status = 'Preparando'
            ) AS pedidos_en_preparacion,

            ROUND(
                COALESCE(
                    SUM(orders.total_amount) FILTER (
                        WHERE orders.status = 'Completado'
                    ),
                    0
                ),
                2
            ) AS ingresos_completados,

            ROUND(
                COALESCE(
                    AVG(orders.total_amount) FILTER (
                        WHERE orders.status = 'Completado'
                    ),
                    0
                ),
                2
            ) AS ticket_promedio

        FROM analytics_raw.orders AS orders

        INNER JOIN analytics_raw.locations AS locations
            ON locations.id = orders.delivery_location_id

        GROUP BY
            locations.province,
            locations.canton,
            locations.district
        """
    )

    # --------------------------------------------------------
    # 3. Frecuencia de uso por cliente
    # --------------------------------------------------------

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_frecuencia_uso_clientes
        AS
        SELECT
            users.id AS id_usuario,
            users.name AS nombre_usuario,
            users.email AS correo,

            COUNT(
                orders.id
            ) AS total_pedidos,

            COUNT(orders.id) FILTER (
                WHERE orders.status = 'Completado'
            ) AS pedidos_completados,

            COUNT(orders.id) FILTER (
                WHERE orders.status = 'Cancelado'
            ) AS pedidos_cancelados,

            ROUND(
                COALESCE(
                    SUM(orders.total_amount) FILTER (
                        WHERE orders.status = 'Completado'
                    ),
                    0
                ),
                2
            ) AS gasto_total_completado,

            ROUND(
                COALESCE(
                    AVG(orders.total_amount) FILTER (
                        WHERE orders.status = 'Completado'
                    ),
                    0
                ),
                2
            ) AS gasto_promedio,

            MAX(
                orders.order_date
            ) AS fecha_ultimo_pedido

        FROM analytics_raw.users AS users

        LEFT JOIN analytics_raw.orders AS orders
            ON orders.user_id = users.id

        GROUP BY
            users.id,
            users.name,
            users.email
        """
    )

    # --------------------------------------------------------
    # 4. Horarios pico
    #
    # EXTRACT(ISODOW):
    # 1 = lunes
    # 7 = domingo
    # --------------------------------------------------------

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_horarios_pico
        AS
        SELECT
            EXTRACT(
                ISODOW FROM orders.order_date
            )::INTEGER AS numero_dia,

            CASE EXTRACT(
                ISODOW FROM orders.order_date
            )::INTEGER
                WHEN 1 THEN 'Lunes'
                WHEN 2 THEN 'Martes'
                WHEN 3 THEN 'Miércoles'
                WHEN 4 THEN 'Jueves'
                WHEN 5 THEN 'Viernes'
                WHEN 6 THEN 'Sábado'
                WHEN 7 THEN 'Domingo'
            END AS nombre_dia,

            EXTRACT(
                HOUR FROM orders.order_date
            )::INTEGER AS hora_dia,

            COUNT(
                orders.id
            ) AS total_pedidos,

            COUNT(orders.id) FILTER (
                WHERE orders.status = 'Completado'
            ) AS pedidos_completados,

            ROUND(
                COALESCE(
                    SUM(orders.total_amount) FILTER (
                        WHERE orders.status = 'Completado'
                    ),
                    0
                ),
                2
            ) AS ingresos_completados

        FROM analytics_raw.orders AS orders

        GROUP BY
            EXTRACT(
                ISODOW FROM orders.order_date
            )::INTEGER,

            EXTRACT(
                HOUR FROM orders.order_date
            )::INTEGER
        """
    )

    # --------------------------------------------------------
    # 5. Estadísticas por estado de pedido
    # --------------------------------------------------------

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_estado_pedidos
        AS
        SELECT
            orders.status AS estado,

            COUNT(
                orders.id
            ) AS total_pedidos,

            ROUND(
                (
                    COUNT(orders.id) * 100.0
                    / SUM(
                        COUNT(orders.id)
                    ) OVER ()
                ),
                2
            ) AS porcentaje_pedidos,

            ROUND(
                SUM(orders.total_amount),
                2
            ) AS monto_involucrado,

            ROUND(
                AVG(orders.total_amount),
                2
            ) AS monto_promedio

        FROM analytics_raw.orders AS orders

        GROUP BY orders.status
        """
    )


def validate_table_count(
    cursor: psycopg.Cursor[Any],
    table_name: str,
    expected_count: int,
) -> None:
    cursor.execute(
        f"""
        SELECT COUNT(*)
        FROM analytics_raw.{table_name}
        """
    )

    result = cursor.fetchone()
    actual_count = int(result[0]) if result else 0

    if actual_count != expected_count:
        raise ValueError(
            f"La tabla {table_name} contiene "
            f"{actual_count} filas, pero se esperaban "
            f"{expected_count}."
        )

    print(
        f"- analytics_raw.{table_name}: "
        f"{actual_count} registros"
    )


def validate_view(
    cursor: psycopg.Cursor[Any],
    view_name: str,
) -> None:
    cursor.execute(
        f"""
        SELECT COUNT(*)
        FROM olap.{view_name}
        """
    )

    result = cursor.fetchone()
    total_rows = int(result[0]) if result else 0

    if total_rows <= 0:
        raise ValueError(
            f"La vista olap.{view_name} quedó vacía."
        )

    print(
        f"- olap.{view_name}: "
        f"{total_rows} registros"
    )


def main() -> None:
    print("Leyendo archivos CSV oficiales...")

    locations = read_csv("locations.csv")
    users = read_csv("users.csv")
    restaurants = read_csv("restaurants.csv")
    menus = read_csv("menus.csv")
    orders = read_csv("orders.csv")
    order_items = read_csv("order_items.csv")

    print(f"- locations.csv: {len(locations)} registros")
    print(f"- users.csv: {len(users)} registros")
    print(
        f"- restaurants.csv: "
        f"{len(restaurants)} registros"
    )
    print(f"- menus.csv: {len(menus)} registros")
    print(f"- orders.csv: {len(orders)} registros")
    print(
        f"- order_items.csv: "
        f"{len(order_items)} registros"
    )

    print()
    print("Conectando con PostgreSQL analítico...")

    with psycopg.connect(
        **DATABASE_CONFIG
    ) as connection:
        with connection.cursor() as cursor:
            recreate_source_tables(cursor)

            load_locations(cursor, locations)
            load_users(cursor, users)
            load_restaurants(cursor, restaurants)
            load_menus(cursor, menus)
            load_orders(cursor, orders)
            load_order_items(cursor, order_items)

            create_indexes(cursor)
            create_olap_views(cursor)

            print()
            print("Validando tablas de origen:")

            validate_table_count(
                cursor,
                "locations",
                len(locations),
            )

            validate_table_count(
                cursor,
                "users",
                len(users),
            )

            validate_table_count(
                cursor,
                "restaurants",
                len(restaurants),
            )

            validate_table_count(
                cursor,
                "menus",
                len(menus),
            )

            validate_table_count(
                cursor,
                "orders",
                len(orders),
            )

            validate_table_count(
                cursor,
                "order_items",
                len(order_items),
            )

            print()
            print("Validando cinco vistas OLAP:")

            view_names = [
                "v_ingresos_mes_categoria",
                "v_actividad_clientes_geografia",
                "v_frecuencia_uso_clientes",
                "v_horarios_pico",
                "v_estado_pedidos",
            ]

            for view_name in view_names:
                validate_view(cursor, view_name)

        connection.commit()

    print()
    print(
        "Carga analítica completada correctamente."
    )

    print(
        "Las cinco vistas OLAP están disponibles "
        "para Metabase."
    )


if __name__ == "__main__":
    main()