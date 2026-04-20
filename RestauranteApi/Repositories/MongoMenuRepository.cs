using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoMenuRepository : IMenuRepository
    {
        // Tu compañero inyectará aquí el driver de MongoDB después

        public Task<bool> RestaurantExistsAsync(int restaurantId)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task<Menu> CreateAsync(Menu menu)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task<Menu?> GetByIdAsync(int id)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task UpdateAsync(Menu menu)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task<bool> MenuExistsAsync(int id)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }
    }
}