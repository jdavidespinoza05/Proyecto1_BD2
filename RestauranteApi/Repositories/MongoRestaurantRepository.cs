using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoRestaurantRepository : IRestaurantRepository
    {
        public Task<IEnumerable<Restaurant>> GetAllAsync()
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task<Restaurant> CreateAsync(Restaurant restaurant)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }
    }
}