using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Models;
using RestaurantesApi.Data;

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController] 
    public class RestaurantsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RestaurantsController(AppDbContext context)
        {
            _context = context;
        }

        // Consulta pública: Permite listar todos los locales disponibles en el sistema
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurants()
        {
            return await _context.Restaurants.ToListAsync();
        }

        // Registro de locales: Endpoint restringido exclusivamente a usuarios con rol administrativo
        [HttpPost]
        [Authorize(Roles = "admin")] 
        public async Task<ActionResult<Restaurant>> CreateRestaurant(Restaurant restaurant)
        {
            // Persistencia en PostgreSQL mediante el contexto de Entity Framework
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRestaurants), new { id = restaurant.Id }, restaurant);
        }
    }
}