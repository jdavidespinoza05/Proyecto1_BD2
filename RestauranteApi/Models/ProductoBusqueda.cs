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