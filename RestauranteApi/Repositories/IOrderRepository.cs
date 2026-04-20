using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order);
        Task<Order?> GetByIdAsync(int id);
    }
}