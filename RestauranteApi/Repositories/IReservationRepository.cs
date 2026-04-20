using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public interface IReservationRepository
    {
        Task<Reservation> CreateAsync(Reservation res);
        Task<Reservation?> GetByIdAsync(int id);
        Task DeleteAsync(Reservation res);
    }
}