/*
Este archivo define el modelo Restaurant, que representa los restaurantes
registrados dentro del sistema.

La clase se mapea directamente con la tabla "restaurants" de PostgreSQL usando
anotaciones de Entity Framework. Sus propiedades representan columnas como el
identificador, nombre, dirección y teléfono del restaurante.

Los campos Name y Address están marcados como obligatorios, ya que son datos
necesarios para registrar correctamente un restaurante. En cambio, Phone se
define como opcional, permitiendo que un restaurante pueda guardarse aunque no
se tenga un número telefónico registrado.

Este modelo funciona como la base para crear, consultar y administrar la
información principal de los restaurantes dentro de la API.
*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantesApi.Models
{
    [Table("restaurants")]
    public class Restaurant
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("phone")]
        public string? Phone { get; set; }
    }
}