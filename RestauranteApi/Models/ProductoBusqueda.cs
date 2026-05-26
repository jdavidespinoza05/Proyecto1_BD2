/*
Este archivo define el modelo ProductoBusqueda, utilizado para representar los
productos que se manejan dentro del módulo de búsqueda del sistema.

A diferencia de otros modelos, esta clase no está mapeada directamente a una
tabla relacional de PostgreSQL. Su objetivo principal es estructurar la
información que se envía o se recibe en formato JSON, especialmente para
procesos relacionados con Elasticsearch o endpoints de búsqueda.

Las propiedades utilizan JsonPropertyName para definir cómo debe llamarse cada
campo en el JSON final, usando nombres como id, nombre, descripcion, precio y
categoria. Esto permite mantener una salida clara y compatible con los datos
que consume el servicio de búsqueda.

En general, este modelo funciona como una representación simplificada de un
producto para facilitar búsquedas, indexación y respuestas relacionadas con el
catálogo del sistema.
*/

using System.Text.Json.Serialization;

namespace RestaurantesApi.Models
{
    public class ProductoBusqueda
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("precio")]
        public decimal Precio { get; set; }

        [JsonPropertyName("categoria")]
        public string Categoria { get; set; } = string.Empty;
    }
}