/*
Este archivo define el modelo Reservation, que representa una reservación
realizada por un usuario en un restaurante.

La clase se mapea con la tabla "reservations" de PostgreSQL mediante
anotaciones de Entity Framework. Cada propiedad representa una columna de la
tabla, incluyendo el identificador de la reserva, el usuario que la realizó, el
restaurante seleccionado, la fecha de la reservación y el estado actual.

El campo Status tiene como valor inicial "Pendiente", lo que indica que la
reservación fue creada pero aún no ha sido confirmada, rechazada o modificada
por el sistema.

Este modelo permite que la API registre y administre las reservas de los
usuarios, manteniendo una estructura clara para relacionar usuarios,
restaurantes y fechas de reservación.
*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantesApi.Models
{
    [Table("reservations")]
    public class Reservation
    {
        [Key][Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("restaurant_id")]
        public int RestaurantId { get; set; }

        [Column("reservation_date")]
        public DateTime ReservationDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Pendiente";
    }
}