from airflow import DAG
from airflow.providers.postgres.hooks.postgres import PostgresHook
from airflow.operators.python import PythonOperator
from datetime import datetime, timedelta

def probar_conexion_api():
    print("Iniciando prueba de conexión con la API operativa...")
    
    # Airflow busca las credenciales que acabas de guardar en la interfaz
    hook = PostgresHook(postgres_conn_id='postgres_api_db')
    conn = hook.get_conn()
    cursor = conn.cursor()
    
    # Hacemos una consulta muy básica para confirmar el acceso
    cursor.execute("SELECT current_database(), current_user;")
    resultado = cursor.fetchone()
    
    print(f"¡Éxito total! Conectado a la base de datos: {resultado[0]} con el usuario: {resultado[1]}")
    
    cursor.close()
    conn.close()

# Configuración básica del pipeline
default_args = {
    'owner': 'jose_david',
    'start_date': datetime(2026, 6, 1),
    'retries': 1,
    'retry_delay': timedelta(minutes=1),
}

# Definición del flujo de trabajo (DAG)
with DAG(
    '01_prueba_conexion_postgres',
    default_args=default_args,
    description='Ping inicial al servidor de PostgreSQL',
    schedule_interval='@daily',
    catchup=False
) as dag:

    # Nuestra única tarea por ahora
    tarea_prueba = PythonOperator(
        task_id='conectar_bd_transaccional',
        python_callable=probar_conexion_api
    )

    tarea_prueba