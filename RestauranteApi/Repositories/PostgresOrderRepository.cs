using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class PostgresOrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public PostgresOrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }
    }
}