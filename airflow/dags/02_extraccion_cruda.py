import csv
import os
from airflow import DAG
from airflow.providers.postgres.hooks.postgres import PostgresHook
from airflow.operators.python import PythonOperator
from datetime import datetime, timedelta

# Función que se conectará a tu BD y exportará la tabla solicitada
def extraer_tabla_a_csv(tabla, ruta_salida):
    print(f"Iniciando extracción de la tabla: {tabla}")
    
    # Conexión usando las credenciales seguras de Airflow
    hook = PostgresHook(postgres_conn_id='postgres_api_db')
    conn = hook.get_conn()
    cursor = conn.cursor()

    # Consulta a la tabla
    cursor.execute(f"SELECT * FROM {tabla};")
    resultados = cursor.fetchall()
    
    # Extraer los nombres de las columnas
    columnas = [desc[0] for desc in cursor.description]

    # Crear la carpeta contenedora si no existe
    os.makedirs(os.path.dirname(ruta_salida), exist_ok=True)

    # Escribir el archivo CSV
    with open(ruta_salida, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow(columnas)
        writer.writerows(resultados)

    print(f"Éxito: {len(resultados)} registros guardados en {ruta_salida}")
    
    cursor.close()
    conn.close()

def orquestar_extraccion():
    # Usamos la carpeta dags/raw_data porque ya la tienes mapeada en tu entorno local
    ruta_base = '/opt/airflow/dags/raw_data'
    
    tablas_a_extraer = ['users', 'restaurants', 'menus', 'reservations', 'orders']

    for tabla in tablas_a_extraer:
        ruta_archivo = f"{ruta_base}/{tabla}.csv"
        try:
            extraer_tabla_a_csv(tabla, ruta_archivo)
        except Exception as e:
            print(f"Error extrayendo {tabla}. Revisa si la tabla existe. Detalle: {e}")

# Parámetros del flujo
default_args = {
    'owner': 'jose_david',
    'start_date': datetime(2026, 6, 1),
    'retries': 1,
    'retry_delay': timedelta(minutes=2),
}

# Definición del DAG
with DAG(
    '02_extraccion_datos_crudos',
    default_args=default_args,
    description='Extrae datos transaccionales de Postgres a formato CSV crudo',
    schedule_interval='@daily',
    catchup=False
) as dag:

    tarea_extraccion = PythonOperator(
        task_id='volcado_postgres_a_csv',
        python_callable=orquestar_extraccion
    )

    tarea_extraccion