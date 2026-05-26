/*
Se encarga de manejar las operaciones relacionadas con los restaurantes en la
base de datos PostgreSQL.

Esta clase permite obtener la lista completa de restaurantes registrados y crear
nuevos restaurantes dentro del sistema. Funciona como una capa de acceso a datos
que utiliza AppDbContext para comunicarse con la base de datos por medio de
Entity Framework.

La idea es que la lógica relacionada con la persistencia de restaurantes quede
centralizada en un solo lugar, evitando repetir consultas o código de base de
datos en otras partes del proyecto.

Al implementar IRestaurantRepository, se mantiene una estructura más ordenada y
se facilita el mantenimiento del sistema en caso de que más adelante se agreguen
nuevas operaciones, validaciones o incluso otro motor de base de datos.
*/

using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class PostgresRestaurantRepository : IRestaurantRepository
    {
        private readonly AppDbContext _context;

        public PostgresRestaurantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Restaurant>> GetAllAsync()
        {
            return await _context.Restaurants.ToListAsync();
        }

        public async Task<Restaurant> CreateAsync(Restaurant restaurant)
        {
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            return restaurant;
        }
    }
}