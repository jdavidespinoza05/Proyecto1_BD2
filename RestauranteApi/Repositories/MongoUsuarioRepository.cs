/*
 * MongoUsuarioRepository
 * Es la implementación en MongoDB del contrato maestro para los usuarios.
 * Básicamente, es el encargado de hacer todo el trabajo de conexión con la 
 * colección "usuarios" en nuestra base de datos NoSQL.
 * Maneja todas las operaciones completas (CRUD) de los perfiles. 
 * Su función estrella es "GetByEmailAsync", que como vimos antes, es la 
 * pieza fundamental que usa el sistema para buscar a las personas al 
 * momento de hacer login o registrarse para que no haya correos duplicados.
 */

using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoUsuarioRepository : IUsuarioRepository
    {
        private readonly IMongoCollection<Usuario> _coleccion;

        public MongoUsuarioRepository(IMongoDatabase database)
        {
            _coleccion = database.GetCollection<Usuario>("usuarios");
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _coleccion.Find(u => u.Correo == email).FirstOrDefaultAsync();
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            return await _coleccion.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            await _coleccion.InsertOneAsync(usuario);
            return usuario;
        }

        public async Task UpdateAsync(Usuario usuario)
        {
            await _coleccion.ReplaceOneAsync(u => u.Id == usuario.Id, usuario);
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            return await _coleccion.Find(u => u.Id == id).AnyAsync();
        }

        public async Task DeleteAsync(Usuario usuario)
        {
            await _coleccion.DeleteOneAsync(u => u.Id == usuario.Id);
        }
    }
}