/*
Es la implementación en MongoDB del contrato para gestionar las reservas.
Al igual que los demás repositorios de este tipo, se encarga del trabajo 
real en la base de datos, conectándose a la colección "reservations".

Cumple al pie de la letra con lo que exige IReservationRepository: 
utiliza comandos de NoSQL (InsertOneAsync, DeleteOneAsync) para guardar 
nuevas reservaciones, buscar sus detalles por ID y eliminarlas cuando 
el usuario decide cancelar.
*/

using MongoDB.Driver;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class MongoReservationRepository : IReservationRepository
    {
        private readonly IMongoCollection<Reservation> _coleccion;

        public MongoReservationRepository(IMongoDatabase database)
        {
            _coleccion = database.GetCollection<Reservation>("reservations");
        }

        public async Task<Reservation> CreateAsync(Reservation res)
        {
            await _coleccion.InsertOneAsync(res);
            return res; 
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _coleccion.Find(r => r.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(Reservation res)
        {
            await _coleccion.DeleteOneAsync(r => r.Id == res.Id);
        }
    }
}