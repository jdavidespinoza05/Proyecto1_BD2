Sistema de Reserva Inteligente de Restaurantes
Instituto Tecnológico de Costa Rica Sede Central Cartago 
Curso: Bases de Datos 2

Profesor: Kenneth Obando Rodríguez

Fecha: 28 de marzo, 2026

 

* Descripción del Proyecto
Esta es una API REST desarrollada en .NET 9 diseñada para la gestión interna de reservas y pedidos en restaurantes. El sistema implementa una arquitectura que incluye:

1. Seguridad: Autenticación y autorización basada en JWT utilizando Keycloak.

2. Base de Datos: Persistencia en PostgreSQL 16.

3. Infraestructura: Orquestación completa mediante Docker y Docker Compose.

4. Calidad de Software: 21 pruebas unitarias con reportes de cobertura.



* Arquitectura y Servicios
El sistema se despliega mediante tres contenedores principales interconectados:

1. api_restaurante: Aplicación .NET 9 para usar los servicios REST.

2. base_datos_restaurante: Instancia de PostgreSQL para el almacenamiento de datos.

3. auth_restaurante: Servidor de Keycloak encargado de la gestión de identidades.



* Guía de Instalación y Despliegue

1. Requisitos:
Tener instalado Docker Desktop.
Clonar o descargar este repositorio.

2. Como levantar el entorno:
Ejecute el siguiente comando en la terminal desde la raíz del proyecto (carpeta principal):

docker-compose up --build

Este comando descargará las imágenes, compilará la API y levantará todos los servicios automáticamente.

3. Configuración de Seguridad (Keycloak)
Para que la seguridad funcione, debe importar la configuración del reino:

Ingrese a http://localhost:8080 
(Username: admin / Password: admin)

Haga clic en "Create Realm" y seleccione el botón "Browse" para subir el archivo realm-export.json incluido en la raíz.

Esto cargará automáticamente el cliente restaurantes-api, los roles de admin y cliente, y los usuarios de prueba.



* Documentación de la API (Swagger)
Una vez que el contenedor esté corriendo, la documentación interactiva de Swagger estará disponible en:

http://localhost:5085/swagger/index.html

Desde aquí podrá probar todos los endpoints de:

Auth: Login y Registro.

Users: Gestión del perfil de usuario autenticado.

Restaurants & Menus: CRUD de locales y platillos.

Reservations & Orders: Gestión de pedidos y citas.

Nota: El WeatherForecast no tiene ninguna utilidad y solo es un ejemplo creado por defecto a la hora de crear la ApiRest.



* Pruebas Unitarias y Cobertura
Se implementó una suite de 21 pruebas unitarias utilizando xUnit y Moq para validar la lógica de negocio de los controladores.

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
Contribuciones: Infraestructura (Docker), Seguridad (Auth/Keycloak), Pruebas y Cobertura, Módulo de Usuarios, Documentación y Video, Correcciones de Bugs

Daniel Viquez Solano - Estudiante de Ingeniería en Computación.
Contribuciones: Lógica de Restaurantes, Menús y Reservas