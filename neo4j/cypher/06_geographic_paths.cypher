// ============================================================
// PROYECTO 3 - CONEXIONES GEOGRÁFICAS Y CAMINOS MÍNIMOS
// ============================================================
//
// Se crean relaciones bidireccionales CONNECTED_TO entre
// ubicaciones.
//
// Cada relación almacena:
// - distanceKm;
// - estimatedMinutes;
// - connectionId.
//
// También se validan:
// - cantidad de conexiones;
// - ausencia de autorrelaciones;
// - conectividad completa;
// - camino mínimo por saltos;
// - camino con menor distancia total.
// ============================================================


// ------------------------------------------------------------
// 1. CARGA DE CONEXIONES GEOGRÁFICAS
// ------------------------------------------------------------

LOAD CSV WITH HEADERS
FROM 'file:///location_connections.csv' AS row

WITH row

WHERE
    row.origin_location_id IS NOT NULL
    AND row.destination_location_id IS NOT NULL
    AND row.origin_location_id <> row.destination_location_id

MATCH (
    origin:Location {
        id: toInteger(row.origin_location_id)
    }
)

MATCH (
    destination:Location {
        id: toInteger(row.destination_location_id)
    }
)

MERGE (origin)-[forward:CONNECTED_TO]->(destination)

SET
    forward.connectionId = toInteger(row.id),
    forward.distanceKm = toFloat(row.distance_km),
    forward.estimatedMinutes =
        toInteger(row.estimated_minutes)

MERGE (destination)-[reverse:CONNECTED_TO]->(origin)

SET
    reverse.connectionId = toInteger(row.id),
    reverse.distanceKm = toFloat(row.distance_km),
    reverse.estimatedMinutes =
        toInteger(row.estimated_minutes);


// ------------------------------------------------------------
// 2. VALIDACIÓN DE CANTIDAD DE RELACIONES
// ------------------------------------------------------------

MATCH (:Location)-[connection:CONNECTED_TO]->(:Location)

RETURN count(connection) AS totalDirectedConnections;


// ------------------------------------------------------------
// 3. VALIDACIÓN DE AUTORRELACIONES
// ------------------------------------------------------------

MATCH (location:Location)-[:CONNECTED_TO]->(location)

RETURN count(*) AS selfConnections;


// ------------------------------------------------------------
// 4. VALIDACIÓN DE CONECTIVIDAD
//
// Se comprueba que las otras 19 ubicaciones puedan alcanzarse
// desde Cartago Centro.
// ------------------------------------------------------------

MATCH (origin:Location {id: 1})
MATCH (destination:Location)

WHERE destination.id <> origin.id

OPTIONAL MATCH path =
    shortestPath(
        (origin)-[:CONNECTED_TO*1..10]->(destination)
    )

RETURN
    count(destination) AS totalDestinations,
    count(path) AS reachableDestinations;


// ------------------------------------------------------------
// 5. CAMINO MÍNIMO POR CANTIDAD DE SALTOS
//
// Origen: Cartago Centro.
// Destino: San Ramón Centro.
//
// shortestPath selecciona la ruta con menor cantidad de
// relaciones CONNECTED_TO.
// ------------------------------------------------------------

MATCH
    (origin:Location {id: 1}),
    (destination:Location {id: 111})

MATCH path =
    shortestPath(
        (origin)-[:CONNECTED_TO*1..10]->(destination)
    )

RETURN
    [location IN nodes(path) | location.name] AS route,
    length(path) AS totalHops,
    reduce(
        totalDistance = 0.0,
        connection IN relationships(path) |
        totalDistance + connection.distanceKm
    ) AS totalDistanceKm,
    reduce(
        totalMinutes = 0,
        connection IN relationships(path) |
        totalMinutes + connection.estimatedMinutes
    ) AS totalEstimatedMinutes;


// ------------------------------------------------------------
// 6. CAMINO CON MENOR DISTANCIA TOTAL
//
// Se evalúan rutas simples de hasta ocho conexiones.
//
// Se suman los pesos distanceKm y estimatedMinutes de todas
// las relaciones de cada ruta.
//
// La condición con all() y single() impide que una misma
// ubicación aparezca más de una vez en la ruta.
// ------------------------------------------------------------

MATCH
    (origin:Location {id: 1}),
    (destination:Location {id: 111})

MATCH path =
    (origin)-[:CONNECTED_TO*1..8]->(destination)

WHERE all(
    currentLocation IN nodes(path)
    WHERE single(
        repeatedLocation IN nodes(path)
        WHERE repeatedLocation = currentLocation
    )
)

WITH
    path,

    reduce(
        totalDistance = 0.0,
        connection IN relationships(path) |
        totalDistance + connection.distanceKm
    ) AS totalDistanceKm,

    reduce(
        totalMinutes = 0,
        connection IN relationships(path) |
        totalMinutes + connection.estimatedMinutes
    ) AS totalEstimatedMinutes

ORDER BY
    totalDistanceKm ASC,
    totalEstimatedMinutes ASC

LIMIT 1

RETURN
    [location IN nodes(path) | location.name] AS optimizedRoute,
    length(path) AS totalHops,
    totalDistanceKm,
    totalEstimatedMinutes;


// ------------------------------------------------------------
// 7. RUTA VISUAL PARA NEO4J BROWSER
// ------------------------------------------------------------

MATCH
    (origin:Location {id: 1}),
    (destination:Location {id: 111})

MATCH path =
    shortestPath(
        (origin)-[:CONNECTED_TO*1..10]->(destination)
    )

RETURN path;