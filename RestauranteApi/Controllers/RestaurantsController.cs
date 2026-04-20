using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories; // <-- Nueva referencia

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController] 
    public class RestaurantsController : ControllerBase
    {
        // Inyectamos la Interfaz, desconectando Entity Framework
        private readonly IRestaurantRepository _repository;

        public RestaurantsController(IRestaurantRepository repository)
        {
            _repository = repository;
        }

        // Consulta pública: Permite listar todos los locales disponibles en el sistema
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurants()
        {
            // La búsqueda se delega al repositorio
            var restaurants = await _repository.GetAllAsync();
            return Ok(restaurants);
        }

        // Registro de locales: Endpoint restringido exclusivamente a usuarios con rol administrativo
        [HttpPost]
        [Authorize(Roles = "admin")] 
        public async Task<ActionResult<Restaurant>> CreateRestaurant(Restaurant restaurant)
        {
            // La persistencia se delega al repositorio (Postgres o Mongo)
            await _repository.CreateAsync(restaurant);

            return CreatedAtAction(nameof(GetRestaurants), new { id = restaurant.Id }, restaurant);
        }
    }
}