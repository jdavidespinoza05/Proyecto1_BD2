using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class PostgresRestaurantRepository : IRestaurantRepository
    {
        private readonly AppDbContext _context;

        public PostgresRestaurantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Restaurant>> GetAllAsync()
        {
            return await _context.Restaurants.ToListAsync();
        }

        public async Task<Restaurant> CreateAsync(Restaurant restaurant)
        {
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            return restaurant;
        }
    }
}