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