using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoRestaurantRepository : IRestaurantRepository
    {
        private readonly IMongoCollection<Restaurant> _coleccion;

        public MongoRestaurantRepository(IMongoDatabase database)
        {
            // Conecta con la "tabla" (colección) de restaurantes en Mongo
            _coleccion = database.GetCollection<Restaurant>("restaurants");
        }

        public async Task<IEnumerable<Restaurant>> GetAllAsync()
        {
            // En Mongo, Find con un filtro vacío {} trae todos los registros
            return await _coleccion.Find(_ => true).ToListAsync();
        }

        public async Task<Restaurant> CreateAsync(Restaurant restaurant)
        {
            // Inserta el restaurante y lo devuelve (como pide tu Task<Restaurant>)
            await _coleccion.InsertOneAsync(restaurant);
            return restaurant; 
        }

        // NOTA: Si tu interfaz IRestaurantRepository tiene GetById, Update o Delete, 
        // aquí te dejo cómo serían para que los agregues si te hacen falta:

        public async Task<Restaurant?> GetByIdAsync(int id)
        {
            return await _coleccion.Find(r => r.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Restaurant restaurant)
        {
            await _coleccion.ReplaceOneAsync(r => r.Id == restaurant.Id, restaurant);
        }

        public async Task DeleteAsync(int id) // O DeleteAsync(Restaurant restaurant) dependiendo de tu interfaz
        {
            await _coleccion.DeleteOneAsync(r => r.Id == id);
        }
    }
}