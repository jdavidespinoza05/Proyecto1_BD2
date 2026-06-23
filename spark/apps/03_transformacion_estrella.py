from pyspark.sql import SparkSession
from pyspark.sql.functions import col, year, month, dayofweek, hour, sum, count

# 1. Inicializar el motor de Spark
spark = SparkSession.builder \
    .appName("ETL_Restaurantes_DataWarehouse") \
    .getOrCreate()

spark.sparkContext.setLogLevel("WARN")
print("Iniciando procesamiento de Big Data con Apache Spark...")

# 2. Definir las rutas 
ruta_cruda = "/opt/spark/data/raw_data/"
ruta_warehouse = "/opt/spark/data/warehouse/"
ruta_cubos = "/opt/spark/data/cubos_olap/"

# 3. Cargar los datos crudos
print("Cargando archivos CSV...")
df_users = spark.read.csv(ruta_cruda + "users.csv", header=True, inferSchema=True)
df_restaurants = spark.read.csv(ruta_cruda + "restaurants.csv", header=True, inferSchema=True)
df_menus = spark.read.csv(ruta_cruda + "menus.csv", header=True, inferSchema=True)
df_orders = spark.read.csv(ruta_cruda + "orders.csv", header=True, inferSchema=True)

# ==========================================
# 4. CONSTRUCCIÓN DE LAS DIMENSIONES
# ==========================================
print("Construyendo Tablas de Dimensiones...")
dim_usuario = df_users.select(col("id").alias("id_usuario"), col("name").alias("nombre_usuario"), col("email").alias("correo"))
dim_restaurante = df_restaurants.select(col("id").alias("id_restaurante"), col("name").alias("nombre_restaurante"), col("address").alias("direccion"))
dim_producto = df_menus.select(col("id").alias("id_producto"), col("name").alias("nombre_producto"), col("price").alias("precio"), col("restaurant_id").alias("id_restaurante_origen"))

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
print("Construyendo Tabla de Hechos...")
fact_pedidos = df_orders.select(
    col("id").alias("id_pedido"),
    col("user_id").alias("id_usuario"),
    col("restaurant_id").alias("id_restaurante"),
    col(columna_fecha).alias("fecha_pedido"),
    col("total_amount").alias("monto_total"), 
    col("status").alias("estado")
)

# ==========================================
# 6. CREACIÓN DE LOS 5 CUBOS OLAP (Requerimiento TEC)
# ==========================================
print("Generando las 5 Vistas OLAP agregadas...")

# Enriquecer la tabla de hechos con dimensiones para facilitar la agregación
df_enriquecido = fact_pedidos \
    .join(dim_tiempo, fact_pedidos.fecha_pedido == dim_tiempo.fecha_completa, "left") \
    .join(dim_restaurante, "id_restaurante", "left")

# Cubo 1: Ingresos por Tiempo (Mes y Año)
cubo_tiempo = df_enriquecido.groupBy("anio", "mes") \
    .agg(sum("monto_total").alias("ingresos_totales"), count("id_pedido").alias("cantidad_pedidos"))

# Cubo 2: Actividad por Ubicación (Restaurante)
cubo_ubicacion = df_enriquecido.groupBy("nombre_restaurante", "direccion") \
    .agg(sum("monto_total").alias("ingresos"), count("id_pedido").alias("volumen_pedidos"))

# Cubo 3: Frecuencia de Uso por Usuario
cubo_frecuencia = fact_pedidos.groupBy("id_usuario") \
    .agg(count("id_pedido").alias("total_pedidos_historico"), sum("monto_total").alias("gasto_total"))

# Cubo 4: Horarios Pico (Tendencia de consumo por hora y día)
cubo_horarios = df_enriquecido.groupBy("dia_semana", "hora_dia") \
    .agg(count("id_pedido").alias("trafico_pedidos"))

# Cubo 5: Estadísticas de Estado de Pedidos (Completados vs Cancelados)
cubo_estado = fact_pedidos.groupBy("estado") \
    .agg(count("id_pedido").alias("total_pedidos"), sum("monto_total").alias("monto_involucrado"))

# ==========================================
# 7. CARGA AL DATA WAREHOUSE 
# ==========================================
print("Guardando Data Warehouse y Cubos OLAP...")
dim_usuario.write.mode("overwrite").parquet(ruta_warehouse + "dim_usuario")
dim_restaurante.write.mode("overwrite").parquet(ruta_warehouse + "dim_restaurante")
dim_producto.write.mode("overwrite").parquet(ruta_warehouse + "dim_producto")
dim_tiempo.write.mode("overwrite").parquet(ruta_warehouse + "dim_tiempo")
fact_pedidos.write.mode("overwrite").parquet(ruta_warehouse + "fact_pedidos")

# Guardar los cubos
cubo_tiempo.write.mode("overwrite").parquet(ruta_cubos + "cubo_tiempo")
cubo_ubicacion.write.mode("overwrite").parquet(ruta_cubos + "cubo_ubicacion")
cubo_frecuencia.write.mode("overwrite").parquet(ruta_cubos + "cubo_frecuencia")
cubo_horarios.write.mode("overwrite").parquet(ruta_cubos + "cubo_horarios")
cubo_estado.write.mode("overwrite").parquet(ruta_cubos + "cubo_estado")

print("¡Transformación, creación de cubos OLAP y carga completadas con éxito!")
spark.stop()