using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoUsuarioRepository : IUsuarioRepository
    {
        private readonly IMongoCollection<Usuario> _coleccion;

        public MongoUsuarioRepository(IMongoDatabase database)
        {
            // Conecta con la "tabla" (colección) de usuarios en Mongo
            _coleccion = database.GetCollection<Usuario>("usuarios");
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            // Busca el primer usuario que coincida con el correo
            return await _coleccion.Find(u => u.Correo == email).FirstOrDefaultAsync();
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            // Busca el primer usuario que coincida con el ID
            return await _coleccion.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            // Inserta el usuario
            await _coleccion.InsertOneAsync(usuario);
            return usuario; // Retornamos el objeto completo
        }

        public async Task UpdateAsync(Usuario usuario)
        {
            // Reemplaza todo el documento basándose en el ID
            await _coleccion.ReplaceOneAsync(u => u.Id == usuario.Id, usuario);
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            // Verifica si algún usuario tiene este ID
            return await _coleccion.Find(u => u.Id == id).AnyAsync();
        }

        public async Task DeleteAsync(Usuario usuario)
        {
            // Elimina el usuario usando el ID del objeto que recibe
            await _coleccion.DeleteOneAsync(u => u.Id == usuario.Id);
        }
    }
}