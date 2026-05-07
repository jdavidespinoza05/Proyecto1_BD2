Sistema de Reserva Inteligente de Restaurantes
Instituto Tecnológico de Costa Rica Sede Central Cartago 
Curso: Bases de Datos 2

Profesor: Kenneth Obando Rodríguez

Fecha: 6 de mayo, 2026

 

* Descripción del Proyecto
Esta es una API REST desarrollada en .NET 9 diseñada para la gestión interna de reservas y pedidos en restaurantes. El sistema implementa una arquitectura que incluye:

1. Seguridad: Autenticación y autorización basada en JWT utilizando Keycloak.

2. Base de Datos: Base de Datos: Persistencia políglota con PostgreSQL 16 y un clúster de MongoDB.

3. Infraestructura: Orquestación completa mediante Docker y Docker Compose.

4. Calidad de Software: 21 pruebas unitarias con reportes de cobertura.

5. Caché: Almacenamiento en memoria de alta velocidad utilizando Redis.

6. Búsquedas: Motor de búsqueda avanzado integrado con ElasticSearch.



* Arquitectura y Servicios
El sistema se despliega mediante tres contenedores principales interconectados:

1. api_restaurante: Aplicación .NET 9 para usar los servicios REST.

2. base_datos_restaurante: Instancia de PostgreSQL para el almacenamiento de datos.

3. auth_restaurante: Servidor de Keycloak encargado de la gestión de identidades.

4. nginx_proxy: Proxy inverso y balanceador de carga para enrutar el tráfico de forma segura.

5. mongo_nodes: Clúster de nodos en Replica Set para alta disponibilidad de datos.

6. redis_cache: Servidor de caché para optimizar consultas recurrentes.

7. elasticsearch: Motor encargado de la indexación y búsquedas de texto completo.



* Guía de Instalación y Despliegue

1. Requisitos:
Tener instalado Docker y Docker Desktop funcionando.
Clonar o descargar este repositorio.
Asegurarse de tener los siguientes puertos libres en su máquina: 80 (Nginx), 8080 (Keycloak), 5432 (Postgres), 27017 (Mongo), 6379 (Redis) y 9200 (ElasticSearch).

2. Como levantar el entorno:
Ejecute el siguiente comando en la terminal desde la raíz del proyecto (carpeta principal):

docker-compose up -d --build

Este comando descargará las imágenes, compilará la API, desplegará 3 réplicas de la aplicación y levantará todos los servicios de bases de datos, caché (Redis) y motor de búsqueda (ElasticSearch) automáticamente en segundo plano.
(Nota: El despliegue inicial puede tomar un par de minutos mientras el clúster de MongoDB y ElasticSearch terminan de configurarse).

3. Configuración de Seguridad (Keycloak)
Para que la seguridad funcione, debe importar la configuración del reino:

Ingrese a http://localhost:8080
(Username: admin / Password: admin)

Haga clic en "Create Realm" y seleccione el botón "Browse" para subir el archivo realm-export.json incluido en la raíz.

Esto cargará automáticamente el cliente restaurantes-api, los roles de admin y cliente, y los usuarios de prueba.



* Documentación de la API (Swagger)
Todo el tráfico hacia la API ahora está siendo protegido y distribuido por el balanceador de carga Nginx. Una vez que el entorno esté corriendo, la documentación interactiva de Swagger estará disponible directamente en:

http://localhost/swagger/index.html

Desde aquí podrá probar todos los endpoints de:

Auth: Login y Registro.

Users: Gestión del perfil de usuario autenticado.

Restaurants & Menus: CRUD de locales y platillos (Acelerado por caché en Redis).

Reservations & Orders: Gestión de pedidos y citas.

Search: Motor de búsqueda avanzado y reconstrucción de índices (ElasticSearch).

Nota: El WeatherForecast no tiene ninguna utilidad y solo es un ejemplo creado por defecto a la hora de crear la ApiRest.



* Pruebas Unitarias y Cobertura
Se implementó una suite de 21 pruebas unitarias utilizando xUnit y Moq para validar la lógica de negocio de los controladores. 
La suite fue refactorizada para aislar completamente las pruebas mediante Inyección de Dependencias, simulando las interfaces del Patrón Repositorio en lugar de depender de bases de datos en memoria.

Para ejecutar las pruebas use el comando en la terminal:

dotnet test

Sobre el reporte de obertura, se alcanzó una cobertura del 100% en la mayoría de los controladores principales (Orders, Restaurants, Reservations).
Si desea ver el reporte por su cuenta puede entrar a la carpeta RestauranteApi.Tests en el proyecto. 
Dentro ingrese a la carpeta coveragereport. 
Dentro de esta carpeta busque el archivo llamado index.html y abralo en el browser. 

Nota: El reporte global de cobertura (22.5%) incluye archivos de migraciones de base de datos (más de 400 líneas autogeneradas) y clases de configuración de infraestructura que no contienen lógica de negocio, por lo que la cobertura real de la lógica implementada cumple con el estándar de calidad del 90%.



* Entregables Incluidos

1. /RestauranteApi: Código fuente de la API.

2. /RestauranteApi.Tests: Proyecto de pruebas unitarias.

3. Dockerfile: Configuración de imagen de la API.

4. docker-compose.yml: Orquestación de servicios.

5. realm-export.json: Configuración de seguridad de Keycloak.

6. README.md: Instrucciones de uso.



* Autores

David Espinoza Brenes - Estudiante de Ingeniería en Computación.
Contribuciones: Infraestructura (Docker), Seguridad (Auth/Keycloak), Pruebas y Cobertura, Módulo de Usuarios, Documentación y Video, Caché (Redis), Búsquedas (ElasticSearch)

Daniel Viquez Solano - Estudiante de Ingeniería en Computación.
Contribuciones: Lógica de Restaurantes, Menús y Reservas, Documentación y Video,  