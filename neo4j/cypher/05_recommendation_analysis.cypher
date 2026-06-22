// ============================================================
// PROYECTO 3 - RECOMENDACIONES Y USUARIOS INFLUYENTES
// ============================================================
//
// Se cargan relaciones RECOMMENDS entre usuarios y se analizan:
//
// 1. Usuarios que realizan más recomendaciones.
// 2. Usuarios que reciben más recomendaciones.
// 3. Influencia total dentro de la red.
// ============================================================


// ------------------------------------------------------------
// 1. CARGA DE RECOMENDACIONES
// ------------------------------------------------------------

LOAD CSV WITH HEADERS FROM 'file:///recommendations.csv' AS row
WITH row
WHERE
    row.recommender_user_id IS NOT NULL
    AND row.recommended_user_id IS NOT NULL

MATCH (
    recommender:User {
        id: toInteger(row.recommender_user_id)
    }
)

MATCH (
    recommended:User {
        id: toInteger(row.recommended_user_id)
    }
)

WHERE recommender.id <> recommended.id

MERGE (recommender)-[recommendation:RECOMMENDS]->(recommended)

SET
    recommendation.recommendationId = toInteger(row.id),
    recommendation.recommendationDate =
        date(row.recommendation_date);


// ------------------------------------------------------------
// 2. VALIDACIÓN DE CANTIDAD
// ------------------------------------------------------------

MATCH (:User)-[recommendation:RECOMMENDS]->(:User)

RETURN count(recommendation) AS totalRecommendations;


// ------------------------------------------------------------
// 3. USUARIOS QUE MÁS RECOMIENDAN A OTROS
// ------------------------------------------------------------

MATCH (user:User)-[:RECOMMENDS]->(recommended:User)

WITH
    user,
    count(DISTINCT recommended) AS recommendationsMade

RETURN
    user.id AS userId,
    user.name AS userName,
    recommendationsMade

ORDER BY
    recommendationsMade DESC,
    userId ASC

LIMIT 5;


// ------------------------------------------------------------
// 4. USUARIOS QUE MÁS RECOMENDACIONES RECIBEN
// ------------------------------------------------------------

MATCH (user:User)<-[:RECOMMENDS]-(recommender:User)

WITH
    user,
    count(DISTINCT recommender) AS recommendationsReceived

RETURN
    user.id AS userId,
    user.name AS userName,
    recommendationsReceived

ORDER BY
    recommendationsReceived DESC,
    userId ASC

LIMIT 5;


// ------------------------------------------------------------
// 5. INDICADOR DE INFLUENCIA
//
// Se suman:
// - recomendaciones realizadas;
// - recomendaciones recibidas.
//
// Es una métrica sencilla y explicable para la simulación.
// ------------------------------------------------------------

MATCH (user:User)

OPTIONAL MATCH (user)-[:RECOMMENDS]->(recommended:User)

WITH
    user,
    count(DISTINCT recommended) AS recommendationsMade

OPTIONAL MATCH (user)<-[:RECOMMENDS]-(recommender:User)

WITH
    user,
    recommendationsMade,
    count(DISTINCT recommender) AS recommendationsReceived

RETURN
    user.id AS userId,
    user.name AS userName,
    recommendationsMade,
    recommendationsReceived,
    recommendationsMade + recommendationsReceived AS influenceScore

ORDER BY
    influenceScore DESC,
    recommendationsReceived DESC,
    userId ASC

LIMIT 5;