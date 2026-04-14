using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantesApi.Models
{
    // Le decimos a .NET que esta clase mapea a la tabla "restaurants" en PostgreSQL
    [Table("restaurants")]
    public class Restaurant
    {
        [Key] // Esto le dice que este es el ID principal (Serial)
        [Column("id")]
        public int Id { get; set; }

        [Required] // Esto significa que no puede venir vacío (NOT NULL)
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("phone")]
        public string? Phone { get; set; } // El signo de interrogación significa que puede ser nulo
    }
}