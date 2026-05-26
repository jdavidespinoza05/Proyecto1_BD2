/*
Este archivo define el modelo Menu, que representa los platillos o productos
ofrecidos por un restaurante dentro del sistema.

La clase se mapea directamente con la tabla "menus" de PostgreSQL mediante
anotaciones de Entity Framework. Cada propiedad representa una columna de la
tabla, como el identificador, nombre, descripción, precio y el restaurante al
que pertenece el menú.

También se incluye la relación con Restaurant por medio de una llave foránea.
Esto permite asociar cada platillo con un restaurante específico y mantener la
integridad entre ambas tablas.

En general, este modelo funciona como la estructura principal que usa la API
para crear, consultar y administrar los menús disponibles en el sistema.
*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantesApi.Models
{
    [Table("menus")] 
    public class Menu
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("price")]
        public decimal Price { get; set; }

        [Required]
        [Column("restaurant_id")]
        public int RestaurantId { get; set; }

        [ForeignKey("RestaurantId")]
        public Restaurant? Restaurant { get; set; }
    }
}