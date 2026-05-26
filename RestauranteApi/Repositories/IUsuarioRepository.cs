/*
 * IUsuarioRepository
 * Es el contrato maestro para gestionar los perfiles de los usuarios 
 * en nuestra base de datos local.
 * A diferencia de otras interfaces más pequeñas, esta exige todas las 
 * operaciones completas (CRUD): crear, buscar, actualizar, borrar y validar.
 * El método "GetByEmailAsync" es la verdadera estrella aquí: es súper 
 * importante porque lo usa el AuthController para asegurarse de que nadie 
 * se registre dos veces con el mismo correo, y también lo usa el UsersController 
 * (en el endpoint "me") para buscar al usuario automáticamente usando la 
 * información oculta en su token de sesión.
 */

using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario> CreateAsync(Usuario usuario); // <-- Agregado para el AuthController
        Task UpdateAsync(Usuario usuario);
        Task<bool> UserExistsAsync(int id);
        Task DeleteAsync(Usuario usuario);
    }
}