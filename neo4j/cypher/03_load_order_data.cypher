// ============================================================
// PROYECTO 3 - CARGA DE PEDIDOS Y DETALLES EN NEO4J
// ============================================================
//
// Fuente oficial:
// spark/data/raw_data
//
// Se cargan:
// - pedidos;
// - usuario que realizó el pedido;
// - restaurante de origen;
// - ubicación de entrega;
// - productos contenidos en cada pedido.
// ============================================================


// ------------------------------------------------------------
// 1. PEDIDOS Y RELACIONES PRINCIPALES
// ------------------------------------------------------------

LOAD CSV WITH HEADERS FROM 'file:///orders.csv' AS row
WITH row
WHERE row.id IS NOT NULL AND trim(row.id) <> ''

MATCH (user:User {id: toInteger(row.user_id)})
MATCH (restaurant:Restaurant {id: toInteger(row.restaurant_id)})
MATCH (location:Location {id: toInteger(row.delivery_location_id)})

MERGE (order:Order {id: toInteger(row.id)})
SET
    order.orderDate = localdatetime(row.order_date),
    order.totalAmount = toFloat(row.total_amount),
    order.status = row.status

MERGE (user)-[:PLACED]->(order)
MERGE (order)-[:ORDERED_FROM]->(restaurant)
MERGE (order)-[:DELIVER_TO]->(location);


// ------------------------------------------------------------
// 2. PRODUCTOS CONTENIDOS EN CADA PEDIDO
// ------------------------------------------------------------

LOAD CSV WITH HEADERS FROM 'file:///order_items.csv' AS row
WITH row
WHERE row.id IS NOT NULL AND trim(row.id) <> ''

MATCH (order:Order {id: toInteger(row.order_id)})
MATCH (product:Product {id: toInteger(row.menu_id)})

MERGE (order)-[contains:CONTAINS]->(product)
SET
    contains.orderItemId = toInteger(row.id),
    contains.quantity = toInteger(row.quantity),
    contains.unitPrice = toFloat(row.unit_price),
    contains.subtotal = toFloat(row.subtotal);