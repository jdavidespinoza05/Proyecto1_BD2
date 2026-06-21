from pyspark.sql import SparkSession
from pyspark.sql.functions import col, year, month, dayofweek, hour

# 1. Inicializar el motor de Spark
spark = SparkSession.builder \
    .appName("ETL_Restaurantes_DataWarehouse") \
    .getOrCreate()

spark.sparkContext.setLogLevel("WARN")
print("Iniciando procesamiento de Big Data con Apache Spark...")

# 2. Definir las rutas (usando el volumen interno del contenedor de Spark)
ruta_cruda = "/opt/bitnami/spark/data/raw_data/"
ruta_warehouse = "/opt/bitnami/spark/data/warehouse/"

# 3. Cargar los datos crudos (Los "Assets" sin procesar)
print("Cargando archivos CSV...")
df_users = spark.read.csv(ruta_cruda + "users.csv", header=True, inferSchema=True)
df_restaurants = spark.read.csv(ruta_cruda + "restaurants.csv", header=True, inferSchema=True)
df_menus = spark.read.csv(ruta_cruda + "menus.csv", header=True, inferSchema=True)
df_orders = spark.read.csv(ruta_cruda + "orders.csv", header=True, inferSchema=True)

# ==========================================
# 4. CONSTRUCCIÓN DE LAS DIMENSIONES
# ==========================================
print("Construyendo Tablas de Dimensiones...")

# Dimensión Usuario
dim_usuario = df_users.select(
    col("id").alias("id_usuario"),
    col("name").alias("nombre_usuario"),
    col("email").alias("correo")
)

# Dimensión Restaurante
dim_restaurante = df_restaurants.select(
    col("id").alias("id_restaurante"),
    col("name").alias("nombre_restaurante"),
    col("address").alias("direccion")
)

# Dimensión Producto (Menú)
dim_producto = df_menus.select(
    col("id").alias("id_producto"),
    col("name").alias("nombre_producto"),
    col("price").alias("precio"),
    col("restaurant_id").alias("id_restaurante_origen")
)

# Dimensión Tiempo (Extraída de la fecha de las órdenes)
# OJO: Asumo 'order_date'. Si te da un error similar aquí, cámbialo a 'date', 'created_at' o lo que diga el log.
columna_fecha = "order_date" 
dim_tiempo = df_orders.select(
    col(columna_fecha).alias("fecha_completa"),
    year(col(columna_fecha)).alias("anio"),
    month(col(columna_fecha)).alias("mes"),
    dayofweek(col(columna_fecha)).alias("dia_semana"),
    hour(col(columna_fecha)).alias("hora_dia")
).distinct()

# ==========================================
# 5. CONSTRUCCIÓN DE LA TABLA DE HECHOS
# ==========================================
print("Construyendo Tabla de Hechos (Fact_Pedidos)...")

# La tabla de hechos centraliza las métricas numéricas y las llaves
fact_pedidos = df_orders.select(
    col("id").alias("id_pedido"),
    col("user_id").alias("id_usuario"),
    col("restaurant_id").alias("id_restaurante"),
    col(columna_fecha).alias("fecha_pedido"),
    col("total_amount").alias("monto_total"), 
    col("status").alias("estado")
)

# ==========================================
# 6. CARGA AL DATA WAREHOUSE (Formato Parquet)
# ==========================================
print("Guardando Data Warehouse en formato columnar optimizado (Parquet)...")

# Guardamos todo en formato Parquet (el estándar de la industria para OLAP)
dim_usuario.write.mode("overwrite").parquet(ruta_warehouse + "dim_usuario")
dim_restaurante.write.mode("overwrite").parquet(ruta_warehouse + "dim_restaurante")
dim_producto.write.mode("overwrite").parquet(ruta_warehouse + "dim_producto")
dim_tiempo.write.mode("overwrite").parquet(ruta_warehouse + "dim_tiempo")
fact_pedidos.write.mode("overwrite").parquet(ruta_warehouse + "fact_pedidos")

print("¡Transformación y carga completada con éxito!")
spark.stop()