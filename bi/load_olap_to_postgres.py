from __future__ import annotations

import os
from pathlib import Path
from typing import Any

import pyarrow.parquet as parquet
import psycopg


ROOT_DIR = Path(__file__).resolve().parents[1]
CUBES_DIR = ROOT_DIR / "spark" / "data" / "cubos_olap"


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


CUBE_DEFINITIONS: dict[str, dict[str, Any]] = {
    "cubo_tiempo": {
        "columns": [
            "anio",
            "mes",
            "ingresos_totales",
            "cantidad_pedidos",
        ],
        "create_sql": """
            CREATE TABLE olap.cubo_tiempo (
                anio INTEGER,
                mes INTEGER,
                ingresos_totales NUMERIC(18, 2),
                cantidad_pedidos BIGINT
            )
        """,
    },
    "cubo_ubicacion": {
        "columns": [
            "nombre_restaurante",
            "direccion",
            "ingresos",
            "volumen_pedidos",
        ],
        "create_sql": """
            CREATE TABLE olap.cubo_ubicacion (
                nombre_restaurante TEXT,
                direccion TEXT,
                ingresos NUMERIC(18, 2),
                volumen_pedidos BIGINT
            )
        """,
    },
    "cubo_frecuencia": {
        "columns": [
            "id_usuario",
            "total_pedidos_historico",
            "gasto_total",
        ],
        "create_sql": """
            CREATE TABLE olap.cubo_frecuencia (
                id_usuario INTEGER,
                total_pedidos_historico BIGINT,
                gasto_total NUMERIC(18, 2)
            )
        """,
    },
    "cubo_horarios": {
        "columns": [
            "dia_semana",
            "hora_dia",
            "trafico_pedidos",
        ],
        "create_sql": """
            CREATE TABLE olap.cubo_horarios (
                dia_semana INTEGER,
                hora_dia INTEGER,
                trafico_pedidos BIGINT
            )
        """,
    },
    "cubo_estado": {
        "columns": [
            "estado",
            "total_pedidos",
            "monto_involucrado",
        ],
        "create_sql": """
            CREATE TABLE olap.cubo_estado (
                estado TEXT,
                total_pedidos BIGINT,
                monto_involucrado NUMERIC(18, 2)
            )
        """,
    },
}


def read_parquet_cube(
    cube_name: str,
) -> list[dict[str, Any]]:
    """Read every Spark Parquet part from one cube."""
    cube_path = CUBES_DIR / cube_name

    if not cube_path.exists():
        raise FileNotFoundError(
            f"No existe la carpeta del cubo: {cube_path}"
        )

    parquet_files = sorted(
        cube_path.glob("*.parquet")
    )

    if not parquet_files:
        raise FileNotFoundError(
            f"El cubo {cube_name} no contiene archivos Parquet."
        )

    table = parquet.read_table(
        [str(file) for file in parquet_files]
    )

    expected_columns = set(
        CUBE_DEFINITIONS[cube_name]["columns"]
    )

    available_columns = set(table.column_names)

    missing_columns = (
        expected_columns - available_columns
    )

    if missing_columns:
        raise ValueError(
            f"Al cubo {cube_name} le faltan columnas: "
            f"{sorted(missing_columns)}. "
            f"Columnas encontradas: "
            f"{sorted(available_columns)}."
        )

    records = table.to_pylist()

    if not records:
        raise ValueError(
            f"El cubo {cube_name} no contiene registros."
        )

    return records


def insert_cube(
    cursor: psycopg.Cursor[Any],
    cube_name: str,
    records: list[dict[str, Any]],
) -> None:
    """Recreate and populate one OLAP table."""
    definition = CUBE_DEFINITIONS[cube_name]
    columns: list[str] = definition["columns"]

    cursor.execute(
        f"DROP TABLE IF EXISTS "
        f"olap.{cube_name} CASCADE"
    )

    cursor.execute(definition["create_sql"])

    column_list = ", ".join(columns)

    placeholders = ", ".join(
        ["%s"] * len(columns)
    )

    insert_sql = (
        f"INSERT INTO olap.{cube_name} "
        f"({column_list}) "
        f"VALUES ({placeholders})"
    )

    values = [
        tuple(record.get(column) for column in columns)
        for record in records
    ]

    cursor.executemany(insert_sql, values)


def create_olap_views(
    cursor: psycopg.Cursor[Any],
) -> None:
    """Create readable SQL views for Metabase."""

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_ingresos_por_tiempo
        AS
        SELECT
            anio,
            mes,
            make_date(anio, mes, 1) AS periodo,
            ingresos_totales,
            cantidad_pedidos
        FROM olap.cubo_tiempo
        """
    )

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_actividad_por_restaurante
        AS
        SELECT
            nombre_restaurante,
            direccion,
            ingresos,
            volumen_pedidos
        FROM olap.cubo_ubicacion
        """
    )

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_frecuencia_clientes
        AS
        SELECT
            id_usuario,
            total_pedidos_historico,
            gasto_total
        FROM olap.cubo_frecuencia
        """
    )

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_horarios_pico
        AS
        SELECT
            dia_semana,
            CASE dia_semana
                WHEN 1 THEN 'Domingo'
                WHEN 2 THEN 'Lunes'
                WHEN 3 THEN 'Martes'
                WHEN 4 THEN 'Miércoles'
                WHEN 5 THEN 'Jueves'
                WHEN 6 THEN 'Viernes'
                WHEN 7 THEN 'Sábado'
            END AS nombre_dia,
            hora_dia,
            trafico_pedidos
        FROM olap.cubo_horarios
        """
    )

    cursor.execute(
        """
        CREATE OR REPLACE VIEW
            olap.v_estado_pedidos
        AS
        SELECT
            estado,
            total_pedidos,
            monto_involucrado
        FROM olap.cubo_estado
        """
    )


def validate_loaded_data(
    cursor: psycopg.Cursor[Any],
) -> None:
    """Ensure that every table contains records."""
    for cube_name in CUBE_DEFINITIONS:
        cursor.execute(
            f"""
            SELECT count(*)
            FROM olap.{cube_name}
            """
        )

        result = cursor.fetchone()

        total_rows = (
            int(result[0])
            if result is not None
            else 0
        )

        if total_rows <= 0:
            raise ValueError(
                f"La tabla olap.{cube_name} "
                "quedó vacía."
            )

        print(
            f"- olap.{cube_name}: "
            f"{total_rows} registros"
        )


def main() -> None:
    loaded_cubes: dict[
        str,
        list[dict[str, Any]],
    ] = {}

    print("Leyendo cubos Parquet de Spark...")

    for cube_name in CUBE_DEFINITIONS:
        records = read_parquet_cube(cube_name)
        loaded_cubes[cube_name] = records

        print(
            f"- {cube_name}: "
            f"{len(records)} registros leídos"
        )

    print()
    print("Conectando con PostgreSQL analítico...")

    with psycopg.connect(
        **DATABASE_CONFIG
    ) as connection:
        with connection.cursor() as cursor:
            cursor.execute(
                "CREATE SCHEMA IF NOT EXISTS olap"
            )

            for cube_name, records in (
                loaded_cubes.items()
            ):
                insert_cube(
                    cursor=cursor,
                    cube_name=cube_name,
                    records=records,
                )

            create_olap_views(cursor)

            print()
            print("Validando tablas cargadas:")

            validate_loaded_data(cursor)

        connection.commit()

    print()
    print(
        "Los cinco cubos OLAP fueron publicados "
        "correctamente en PostgreSQL."
    )

    print(
        "Metabase ya puede consultar el esquema olap."
    )


if __name__ == "__main__":
    main()