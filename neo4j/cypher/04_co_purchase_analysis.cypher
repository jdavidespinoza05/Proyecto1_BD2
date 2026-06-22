// ============================================================
// PROYECTO 3 - ANÁLISIS DE CO-COMPRA
// ============================================================
//
// Objetivo:
// Obtener los cinco pares de productos que aparecen juntos
// en la mayor cantidad de pedidos completados.
//
// Cada pareja se cuenta una sola vez por pedido.
// La condición product1.id < product2.id evita devolver:
//
// Hamburguesa + Papas
// Papas + Hamburguesa
//
// como si fueran dos combinaciones diferentes.
// ============================================================

// ------------------------------------------------------------
// 1. VALIDACIÓN PREVIA
// Pedidos completados disponibles para el análisis.
// ------------------------------------------------------------

MATCH (order:Order {status: 'Completado'})
RETURN count(order) AS completedOrders;

// ------------------------------------------------------------
// 2. CINCO PARES DE PRODUCTOS MÁS COMPRADOS JUNTOS
// ------------------------------------------------------------

MATCH
(product1:Product)
<-[:CONTAINS]-
(order:Order {status: 'Completado'})
-[:CONTAINS]->
(product2:Product)

WHERE product1.id < product2.id

WITH
product1,
product2,
count(DISTINCT order) AS purchasesTogether

RETURN
product1.id AS product1Id,
product1.name AS product1Name,
product2.id AS product2Id,
product2.name AS product2Name,
purchasesTogether

ORDER BY
purchasesTogether DESC,
product1Id ASC,
product2Id ASC

LIMIT 5;

// ------------------------------------------------------------
// 3. VALIDACIÓN DE LA PAREJA PRINCIPAL
// Hamburguesa urbana y Papas rústicas.
// ------------------------------------------------------------

MATCH
(:Product {id: 7})
<-[:CONTAINS]-
(order:Order {status: 'Completado'})
-[:CONTAINS]->
(:Product {id: 8})

RETURN count(DISTINCT order) AS hamburgerAndFriesOrders;

// ------------------------------------------------------------
// 4. RUTAS PARA VISUALIZACIÓN EN NEO4J BROWSER
//
// Devuelve diez pedidos completados que contienen tanto
// Hamburguesa urbana como Papas rústicas.
//
// Al devolver path, Neo4j Browser muestra los nodos junto
// con las relaciones CONTAINS.
// ------------------------------------------------------------

MATCH path =
(:Product {id: 7})
<-[:CONTAINS]-
(:Order {status: 'Completado'})
-[:CONTAINS]->
(:Product {id: 8})

RETURN path

LIMIT 10;
