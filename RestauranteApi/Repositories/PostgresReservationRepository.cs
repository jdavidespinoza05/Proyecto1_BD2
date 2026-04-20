using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class PostgresReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _context;

        public PostgresReservationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Reservation> CreateAsync(Reservation res)
        {
            _context.Reservations.Add(res);
            await _context.SaveChangesAsync();
            return res;
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _context.Reservations.FindAsync(id);
        }

        public async Task DeleteAsync(Reservation res)
        {
            _context.Reservations.Remove(res);
            await _context.SaveChangesAsync();
        }
    }
}