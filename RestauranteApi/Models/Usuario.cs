/*
Este archivo define el modelo Usuario, que representa los usuarios registrados
en la base de datos local del sistema.

La clase se mapea con la tabla "users" de PostgreSQL mediante anotaciones de
Entity Framework. Sus propiedades representan datos como el identificador local,
el identificador proveniente de Keycloak, el nombre, el correo electrónico y el
rol del usuario.

El campo KeycloakId permite relacionar el usuario almacenado localmente con la
cuenta creada en Keycloak. Esto es importante porque Keycloak se encarga de la
autenticación, mientras que la base de datos local conserva información básica
del perfil del usuario para uso interno de la aplicación.

Los campos Nombre, Correo y Rol son obligatorios. Además, el rol tiene como
valor inicial "cliente", lo que permite asignar un perfil básico por defecto a
los usuarios recién registrados.

Este modelo permite mantener organizada la información de usuarios dentro del
sistema, separando los datos de autenticación administrados por Keycloak de los
datos propios de la aplicación.
*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantesApi.Models
{
    [Table("users")] 
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("keycloak_id")]
        public string? KeycloakId { get; set; } 

        [Required]
        [Column("name")] 
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Column("email")] 
        public string Correo { get; set; } = string.Empty;

        [Required]
        [Column("role")] 
        public string Rol { get; set; } = "cliente"; 
    }
}