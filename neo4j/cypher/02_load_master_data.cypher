// ============================================================
// PROYECTO 3 - CARGA DE DATOS MAESTROS EN NEO4J
// ============================================================
//
// Fuente oficial:
// spark/data/raw_data
//
// Se cargan:
// - ubicaciones;
// - usuarios;
// - restaurantes;
// - productos.
//
// MERGE permite ejecutar el script varias veces sin duplicar
// los nodos ni las relaciones.
// ============================================================


// ------------------------------------------------------------
// 1. UBICACIONES
// ------------------------------------------------------------

LOAD CSV WITH HEADERS FROM 'file:///locations.csv' AS row
WITH row
WHERE row.id IS NOT NULL AND trim(row.id) <> ''

MERGE (location:Location {id: toInteger(row.id)})
SET
    location.name = row.name,
    location.province = row.province,
    location.canton = row.canton,
    location.district = row.district,
    location.latitude = toFloat(row.latitude),
    location.longitude = toFloat(row.longitude),
    location.locationType = row.location_type;


// ------------------------------------------------------------
// 2. USUARIOS Y UBICACIONES
// ------------------------------------------------------------

LOAD CSV WITH HEADERS FROM 'file:///users.csv' AS row
WITH row
WHERE row.id IS NOT NULL AND trim(row.id) <> ''

MERGE (user:User {id: toInteger(row.id)})
SET
    user.keycloakId = row.keycloak_id,
    user.name = row.name,
    user.email = row.email,
    user.role = row.role

WITH user, row
MATCH (location:Location {id: toInteger(row.location_id)})
MERGE (user)-[:LOCATED_AT]->(location);


// ------------------------------------------------------------
// 3. RESTAURANTES Y UBICACIONES
// ------------------------------------------------------------

LOAD CSV WITH HEADERS FROM 'file:///restaurants.csv' AS row
WITH row
WHERE row.id IS NOT NULL AND trim(row.id) <> ''

MERGE (restaurant:Restaurant {id: toInteger(row.id)})
SET
    restaurant.name = row.name,
    restaurant.address = row.address,
    restaurant.phone = row.phone

WITH restaurant, row
MATCH (location:Location {id: toInteger(row.location_id)})
MERGE (restaurant)-[:LOCATED_AT]->(location);


// ------------------------------------------------------------
// 4. PRODUCTOS Y RESTAURANTES
// ------------------------------------------------------------

LOAD CSV WITH HEADERS FROM 'file:///menus.csv' AS row
WITH row
WHERE row.id IS NOT NULL AND trim(row.id) <> ''

MERGE (product:Product {id: toInteger(row.id)})
SET
    product.name = row.name,
    product.description = row.description,
    product.price = toFloat(row.price),
    product.category = row.category,
    product.sourceEntity = 'menus'

WITH product, row
MATCH (restaurant:Restaurant {id: toInteger(row.restaurant_id)})
MERGE (restaurant)-[:OFFERS]->(product);