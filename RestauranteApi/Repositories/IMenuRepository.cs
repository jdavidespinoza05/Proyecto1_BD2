using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public interface IMenuRepository
    {
        Task<bool> RestaurantExistsAsync(int restaurantId);
        Task<Menu> CreateAsync(Menu menu);
        Task<Menu?> GetByIdAsync(int id);
        Task UpdateAsync(Menu menu);
        Task<bool> MenuExistsAsync(int id);
        Task DeleteAsync(int id);
    }
}