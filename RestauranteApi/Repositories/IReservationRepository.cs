/*
 Es el contrato que define las reglas para manejar las reservaciones 
 de mesas en el sistema.
 Exige que cualquier base de datos que usemos (Postgres o Mongo) 
 sepa hacer tres cosas específicas: crear una reserva nueva, buscar 
 una reserva por su ID y eliminarla (para poder cancelarla).
 
 Algo interesante de la lógica de negocio aquí es que no tiene un 
 método para "actualizar". Esto significa que el sistema está diseñado 
 para que, si te equivocas o quieres cambiar la fecha, tengas que 
 cancelar la reserva (Delete) y crear una nueva (Create).
*/

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