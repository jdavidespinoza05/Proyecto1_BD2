/*
Es la implementación en MongoDB del contrato para gestionar los pedidos.

Como vimos en su interfaz (IOrderRepository), este archivo es súper 
directo y al grano: solo se conecta a la colección "orders" de la base 
de datos para insertar un pedido nuevo o buscar uno por su ID.

Utiliza las funciones nativas de Mongo (InsertOneAsync y Find) para 
hacer el trabajo real de persistencia, manteniendo así a nuestro 
controlador totalmente libre de esta lógica.
*/

using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoOrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<Order> _coleccion;

        public MongoOrderRepository(IMongoDatabase database)
        {
            _coleccion = database.GetCollection<Order>("orders");
        }

        public async Task<Order> CreateAsync(Order order)
        {
            await _coleccion.InsertOneAsync(order);
            return order; 
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _coleccion.Find(o => o.Id == id).FirstOrDefaultAsync();
        }
    }
}