using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoReservationRepository : IReservationRepository
    {
        public Task<Reservation> CreateAsync(Reservation res)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task<Reservation?> GetByIdAsync(int id)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }

        public Task DeleteAsync(Reservation res)
        {
            throw new NotImplementedException("Falta implementar MongoDB");
        }
    }
}