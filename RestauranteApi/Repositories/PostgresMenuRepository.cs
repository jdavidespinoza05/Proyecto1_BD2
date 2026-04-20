using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class PostgresMenuRepository : IMenuRepository
    {
        private readonly AppDbContext _context;

        public PostgresMenuRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> RestaurantExistsAsync(int restaurantId)
        {
            return await _context.Restaurants.AnyAsync(r => r.Id == restaurantId);
        }

        public async Task<Menu> CreateAsync(Menu menu)
        {
            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();
            return menu;
        }

        public async Task<Menu?> GetByIdAsync(int id)
        {
            return await _context.Menus.FindAsync(id);
        }

        public async Task UpdateAsync(Menu menu)
        {
            _context.Entry(menu).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> MenuExistsAsync(int id)
        {
            return await _context.Menus.AnyAsync(e => e.Id == id);
        }

        public async Task DeleteAsync(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu != null)
            {
                _context.Menus.Remove(menu);
                await _context.SaveChangesAsync();
            }
        }
    }
}