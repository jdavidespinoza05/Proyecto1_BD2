using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoOrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<Order> _coleccion;

        public MongoOrderRepository(IMongoDatabase database)
        {
            // Conecta con la "tabla" (colección) de órdenes/pedidos en Mongo
            _coleccion = database.GetCollection<Order>("orders");
        }

        public async Task<Order> CreateAsync(Order order)
        {
            // Inserta la orden en la base de datos
            await _coleccion.InsertOneAsync(order);
            return order; // Retornamos el objeto con sus datos (como pide la interfaz)
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            // Busca la primera orden que coincida con el ID
            return await _coleccion.Find(o => o.Id == id).FirstOrDefaultAsync();
        }
    }
}