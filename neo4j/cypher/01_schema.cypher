// ============================================================
// PROYECTO 3 - ESQUEMA BASE DE NEO4J
// ============================================================
//
// Este archivo define las restricciones e índices iniciales
// utilizados por el módulo de análisis de grafos y rutas.
//
// El uso de IF NOT EXISTS permite ejecutar este archivo varias
// veces sin crear restricciones o índices duplicados.
// ============================================================


// ------------------------------------------------------------
// RESTRICCIONES DE UNICIDAD
// ------------------------------------------------------------

// Cada usuario debe tener un identificador único.
CREATE CONSTRAINT user_id_unique IF NOT EXISTS
FOR (user:User)
REQUIRE user.id IS UNIQUE;


// Cada pedido debe tener un identificador único.
CREATE CONSTRAINT order_id_unique IF NOT EXISTS
FOR (order:Order)
REQUIRE order.id IS UNIQUE;


// Cada producto debe tener un identificador único.
// Product representa los registros de la tabla menus.
CREATE CONSTRAINT product_id_unique IF NOT EXISTS
FOR (product:Product)
REQUIRE product.id IS UNIQUE;


// Cada restaurante debe tener un identificador único.
CREATE CONSTRAINT restaurant_id_unique IF NOT EXISTS
FOR (restaurant:Restaurant)
REQUIRE restaurant.id IS UNIQUE;


// Cada ubicación utilizada en las rutas debe ser única.
CREATE CONSTRAINT location_id_unique IF NOT EXISTS
FOR (location:Location)
REQUIRE location.id IS UNIQUE;


// Cada repartidor debe tener un identificador único.
CREATE CONSTRAINT driver_id_unique IF NOT EXISTS
FOR (driver:Driver)
REQUIRE driver.id IS UNIQUE;


// ------------------------------------------------------------
// ÍNDICES PARA BÚSQUEDAS FRECUENTES
// ------------------------------------------------------------

// Permite localizar productos rápidamente por nombre.
CREATE INDEX product_name_index IF NOT EXISTS
FOR (product:Product)
ON (product.name);


// Permite filtrar productos por categoría.
CREATE INDEX product_category_index IF NOT EXISTS
FOR (product:Product)
ON (product.category);


// Permite agrupar y consultar pedidos según su estado.
CREATE INDEX order_status_index IF NOT EXISTS
FOR (order:Order)
ON (order.status);


// Permite encontrar ubicaciones por su nombre.
CREATE INDEX location_name_index IF NOT EXISTS
FOR (location:Location)
ON (location.name);


// Permite localizar restaurantes por nombre.
CREATE INDEX restaurant_name_index IF NOT EXISTS
FOR (restaurant:Restaurant)
ON (restaurant.name);