/*
Se encarga de manejar las operaciones relacionadas con las reservaciones en la
base de datos PostgreSQL.

Esta clase permite crear nuevas reservaciones, buscar una reservación específica
por su identificador y eliminar reservaciones cuando sea necesario. Su función
principal es centralizar el acceso a los datos de reservas para que el resto de
la aplicación no dependa directamente de Entity Framework.

Al trabajar mediante IReservationRepository, el controlador o servicio que use
esta clase no necesita saber cómo se guardan los datos internamente, sino que
solo utiliza métodos definidos para cada operación.

Esto ayuda a mantener el proyecto más ordenado, con una separación clara entre
la lógica de negocio y la lógica de base de datos.
*/

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