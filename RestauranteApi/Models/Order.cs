/*
Este archivo define el modelo Order, que representa una orden realizada por un
usuario dentro del sistema de restaurantes.

La clase se mapea con la tabla "orders" de PostgreSQL utilizando anotaciones de
Entity Framework. Cada propiedad corresponde a una columna de la tabla, como el
identificador de la orden, el usuario que la realizó, el restaurante asociado,
la fecha de creación, el monto total y el estado actual de la orden.

El campo OrderDate se inicializa automáticamente con la fecha y hora actual en
formato UTC, lo que permite registrar cuándo fue creada la orden. Además, el
estado inicia por defecto como "Preparando", representando el flujo inicial de
una orden dentro del sistema.

Este modelo sirve como base para registrar y consultar órdenes desde la API,
manteniendo organizada la información relacionada con las compras o pedidos de
los usuarios.
*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantesApi.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("restaurant_id")]
        public int RestaurantId { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Preparando";
    }
}