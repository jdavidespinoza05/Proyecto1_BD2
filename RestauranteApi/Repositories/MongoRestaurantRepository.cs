/*
 * MongoRestaurantRepository
 * Es nuestro trabajador encargado de la colección "restaurants" en MongoDB.
 * Cumple con lo que exige la interfaz actual: traer toda la lista de 
 * restaurantes (usando un filtro vacío para obtenerlos todos, que es justo 
 * lo que el Controller necesita para su caché) y guardar nuevos locales.
 * Un detalle particular de este archivo es que ya trae preparados y listos 
 * los métodos adicionales (buscar, actualizar y eliminar) por si en algún 
 * momento deciden expandir el contrato de la interfaz y agregar esas funciones.
 */

using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoRestaurantRepository : IRestaurantRepository
    {
        private readonly IMongoCollection<Restaurant> _coleccion;

        public MongoRestaurantRepository(IMongoDatabase database)
        {
            _coleccion = database.GetCollection<Restaurant>("restaurants");
        }

        public async Task<IEnumerable<Restaurant>> GetAllAsync()
        {
            return await _coleccion.Find(_ => true).ToListAsync();
        }

        public async Task<Restaurant> CreateAsync(Restaurant restaurant)
        {
            await _coleccion.InsertOneAsync(restaurant);
            return restaurant; 
        }

        public async Task<Restaurant?> GetByIdAsync(int id)
        {
            return await _coleccion.Find(r => r.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Restaurant restaurant)
        {
            await _coleccion.ReplaceOneAsync(r => r.Id == restaurant.Id, restaurant);
        }

        public async Task DeleteAsync(int id) 
        {
            await _coleccion.DeleteOneAsync(r => r.Id == id);
        }
    }
}