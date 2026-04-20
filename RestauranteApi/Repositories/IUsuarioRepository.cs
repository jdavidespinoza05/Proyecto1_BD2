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