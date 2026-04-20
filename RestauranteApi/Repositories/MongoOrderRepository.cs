using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoOrderRepository : IOrderRepository
    {
        public Task<Order> CreateAsync(Order order)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task<Order?> GetByIdAsync(int id)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }
    }
}