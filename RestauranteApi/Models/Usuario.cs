using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantesApi.Models
{
    [Table("users")] // Obligamos a que busque la tabla "users"
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("keycloak_id")]
        public string? KeycloakId { get; set; } // Para que David guarde el ID de Keycloak aquí

        [Required]
        [Column("name")] // En el SQL puso 'name', no 'Nombre'
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Column("email")] // En el SQL puso 'email', no 'Correo'
        public string Correo { get; set; } = string.Empty;

        [Required]
        [Column("role")] // En el SQL puso 'role', no 'Rol'
        public string Rol { get; set; } = "cliente"; 
    }
}