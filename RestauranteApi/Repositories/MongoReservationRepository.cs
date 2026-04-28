using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoReservationRepository : IReservationRepository
    {
        private readonly IMongoCollection<Reservation> _coleccion;

        public MongoReservationRepository(IMongoDatabase database)
        {
            // Conecta con la "tabla" (colección) de reservaciones en Mongo
            _coleccion = database.GetCollection<Reservation>("reservations");
        }

        public async Task<Reservation> CreateAsync(Reservation res)
        {
            // Inserta la reservación
            await _coleccion.InsertOneAsync(res);
            return res; // Retornamos el objeto completo
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            // Busca la primera reservación que coincida con el ID
            return await _coleccion.Find(r => r.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(Reservation res)
        {
            // Elimina la reservación usando el ID del objeto que recibe
            await _coleccion.DeleteOneAsync(r => r.Id == res.Id);
        }
    }
}