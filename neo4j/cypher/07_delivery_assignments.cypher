// ============================================================
// PROYECTO 3 - REPARTIDORES Y ASIGNACIÓN DE PEDIDOS
// ============================================================
//
// Se cargan:
// - nodos Driver;
// - restaurante para el que trabaja cada repartidor;
// - ubicación inicial;
// - pedidos pendientes asignados.
//
// Relaciones:
// (Driver)-[:WORKS_FOR]->(Restaurant)
// (Driver)-[:STARTS_AT]->(Location)
// (Driver)-[:ASSIGNED_TO]->(Order)
// ============================================================


// ------------------------------------------------------------
// 1. CARGA DE REPARTIDORES
// ------------------------------------------------------------

LOAD CSV WITH HEADERS FROM 'file:///drivers.csv' AS row

WITH row

WHERE row.id IS NOT NULL AND trim(row.id) <> ''

MERGE (driver:Driver {id: toInteger(row.id)})

SET
    driver.name = row.name,
    driver.capacity = toInteger(row.capacity),
    driver.active = toBoolean(row.active)

WITH driver, row

MATCH (
    restaurant:Restaurant {
        id: toInteger(row.restaurant_id)
    }
)

MERGE (driver)-[:WORKS_FOR]->(restaurant)

WITH driver, row

MATCH (
    startLocation:Location {
        id: toInteger(row.start_location_id)
    }
)

MERGE (driver)-[:STARTS_AT]->(startLocation);


// ------------------------------------------------------------
// 2. CARGA DE ASIGNACIONES
// ------------------------------------------------------------

LOAD CSV WITH HEADERS
FROM 'file:///delivery_assignments.csv' AS row

WITH row

WHERE
    row.driver_id IS NOT NULL
    AND row.order_id IS NOT NULL

MATCH (
    driver:Driver {
        id: toInteger(row.driver_id)
    }
)

MATCH (
    order:Order {
        id: toInteger(row.order_id)
    }
)

MERGE (driver)-[assignment:ASSIGNED_TO]->(order)

SET
    assignment.assignmentId = toInteger(row.id),
    assignment.assignedAt = localdatetime(row.assigned_at);


// ------------------------------------------------------------
// 3. CANTIDAD DE REPARTIDORES
// ------------------------------------------------------------

MATCH (driver:Driver)

RETURN count(driver) AS totalDrivers;


// ------------------------------------------------------------
// 4. CANTIDAD DE ASIGNACIONES
// ------------------------------------------------------------

MATCH (:Driver)-[assignment:ASSIGNED_TO]->(:Order)

RETURN count(assignment) AS totalAssignments;


// ------------------------------------------------------------
// 5. DISTRIBUCIÓN POR REPARTIDOR
// ------------------------------------------------------------

MATCH (driver:Driver)

OPTIONAL MATCH (driver)-[:ASSIGNED_TO]->(order:Order)

RETURN
    driver.id AS driverId,
    driver.name AS driverName,
    driver.capacity AS capacity,
    count(order) AS assignedOrders

ORDER BY driverId;


// ------------------------------------------------------------
// 6. PEDIDOS PENDIENTES SIN UNA ASIGNACIÓN EXACTA
//
// Detecta pedidos sin repartidor o con más de uno.
// ------------------------------------------------------------

MATCH (order:Order {status: 'Preparando'})

OPTIONAL MATCH (driver:Driver)-[:ASSIGNED_TO]->(order)

WITH
    order,
    count(driver) AS assignedDrivers

WHERE assignedDrivers <> 1

RETURN count(order) AS invalidPendingOrderAssignments;


// ------------------------------------------------------------
// 7. REPARTIDORES QUE SUPERAN SU CAPACIDAD
// ------------------------------------------------------------

MATCH (driver:Driver)

OPTIONAL MATCH (driver)-[:ASSIGNED_TO]->(order:Order)

WITH
    driver,
    count(order) AS assignedOrders

WHERE assignedOrders > driver.capacity

RETURN count(driver) AS driversOverCapacity;


// ------------------------------------------------------------
// 8. PEDIDOS NO PENDIENTES ASIGNADOS
// ------------------------------------------------------------

MATCH (:Driver)-[:ASSIGNED_TO]->(order:Order)

WHERE order.status <> 'Preparando'

RETURN count(order) AS invalidAssignedOrders;


// ------------------------------------------------------------
// 9. ASIGNACIONES A RESTAURANTES INCORRECTOS
// ------------------------------------------------------------

MATCH
    (driver:Driver)-[:WORKS_FOR]->(
        driverRestaurant:Restaurant
    ),

    (driver)-[:ASSIGNED_TO]->(order:Order)
        -[:ORDERED_FROM]->(
            orderRestaurant:Restaurant
        )

WHERE driverRestaurant.id <> orderRestaurant.id

RETURN count(*) AS invalidRestaurantAssignments;