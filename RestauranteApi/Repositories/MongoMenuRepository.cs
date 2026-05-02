using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoMenuRepository : IMenuRepository
    {
        private readonly IMongoCollection<Menu> _coleccion;
        private readonly IMongoCollection<Restaurant> _restaurantesColeccion; // Para validar si el restaurante existe

        public MongoMenuRepository(IMongoDatabase database)
        {
            // Conecta con las colecciones en Mongo
            _coleccion = database.GetCollection<Menu>("menus");
            _restaurantesColeccion = database.GetCollection<Restaurant>("restaurants");
        }

        public async Task<bool> RestaurantExistsAsync(int restaurantId)
        {
            // Busca si existe al menos un restaurante con ese ID
            return await _restaurantesColeccion.Find(r => r.Id == restaurantId).AnyAsync();
        }

        public async Task<Menu> CreateAsync(Menu menu)
        {
            // Inserta el menú en la base de datos
            await _coleccion.InsertOneAsync(menu);
            return menu; // Retornamos el objeto porque la interfaz pide Task<Menu>
        }

        public async Task<Menu?> GetByIdAsync(int id)
        {
            // Busca el primer menú que coincida con el ID
            return await _coleccion.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Menu menu)
        {
            // Reemplaza todo el documento del menú basándose en su ID
            await _coleccion.ReplaceOneAsync(m => m.Id == menu.Id, menu);
        }

        public async Task<bool> MenuExistsAsync(int id)
        {
            // Verifica si algún menú tiene este ID
            return await _coleccion.Find(m => m.Id == id).AnyAsync();
        }

        public async Task DeleteAsync(int id)
        {
            // Elimina el menú basándose en su ID
            await _coleccion.DeleteOneAsync(m => m.Id == id);
        }
    }
}