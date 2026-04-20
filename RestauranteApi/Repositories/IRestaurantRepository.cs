using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public interface IRestaurantRepository
    {
        Task<IEnumerable<Restaurant>> GetAllAsync();
        Task<Restaurant> CreateAsync(Restaurant restaurant);
    }
}