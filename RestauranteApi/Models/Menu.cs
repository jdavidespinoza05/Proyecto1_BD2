using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantesApi.Models
{
    // Le decimos que la tabla en PostgreSQL se va a llamar "menus"
    [Table("menus")] 
    public class Menu
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty; // Ej: "Chifrijo" o "Casado"

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("price")]
        public decimal Price { get; set; }

        // La Llave Foránea: A qué restaurante pertenece este platillo
        [Required]
        [Column("restaurant_id")]
        public int RestaurantId { get; set; }

        [ForeignKey("RestaurantId")]
        public Restaurant? Restaurant { get; set; }
    }
}