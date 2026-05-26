/*
Este es nuestro "cocinero experto en NoSQL". Es el archivo que firma el 
contrato IMenuRepository y contiene el código real para gestionar 
los menús específicamente dentro de MongoDB.
En lugar de usar tablas y filas como Postgres, aquí la lógica se conecta 
a "colecciones" de documentos.

Un detalle clave de su implementación es que, para poder cumplir con 
la regla de "validar si un restaurante existe" antes de crear un plato, 
este repositorio necesita conectarse tanto a la colección de "menus" 
como a la de "restaurants".

Traduce todas las tareas que pedía la interfaz a comandos nativos 
de Mongo, como InsertOneAsync o ReplaceOneAsync.
*/

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
            _coleccion = database.GetCollection<Menu>("menus");
            _restaurantesColeccion = database.GetCollection<Restaurant>("restaurants");
        }

        public async Task<bool> RestaurantExistsAsync(int restaurantId)
        {
            return await _restaurantesColeccion.Find(r => r.Id == restaurantId).AnyAsync();
        }

        public async Task<Menu> CreateAsync(Menu menu)
        {
            await _coleccion.InsertOneAsync(menu);
            return menu; 
        }

        public async Task<Menu?> GetByIdAsync(int id)
        {
            return await _coleccion.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Menu menu)
        {
            await _coleccion.ReplaceOneAsync(m => m.Id == menu.Id, menu);
        }

        public async Task<bool> MenuExistsAsync(int id)
        {
            return await _coleccion.Find(m => m.Id == id).AnyAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _coleccion.DeleteOneAsync(m => m.Id == id);
        }
    }
}