using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoUsuarioRepository : IUsuarioRepository
    {
        public Task<Usuario?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<Usuario?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<Usuario> CreateAsync(Usuario usuario) => throw new NotImplementedException();
        public Task UpdateAsync(Usuario usuario) => throw new NotImplementedException();
        public Task<bool> UserExistsAsync(int id) => throw new NotImplementedException();
        public Task DeleteAsync(Usuario usuario) => throw new NotImplementedException();
    }
}