using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed; // <-- Nueva librería para caché
using System.Text.Json; // <-- Para convertir objetos a texto
using RestaurantesApi.Models;
using RestaurantesApi.Repositories; 

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController] 
    public class RestaurantsController : ControllerBase
    {
        // Inyectamos la Interfaz de DB y el Caché de Redis
        private readonly IRestaurantRepository _repository;
        private readonly IDistributedCache _cache; 

        public RestaurantsController(IRestaurantRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache; // Asignamos la memoria
        }

        // Consulta pública: Permite listar todos los locales con alta velocidad usando Caché
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurants()
        {
            string cacheKey = "lista_restaurantes_activos";

            // 1. Intentar buscar la lista en la memoria Redis primero
            var restaurantesCache = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(restaurantesCache))
            {
                // ¡Estaba en caché! Lo convertimos de texto JSON a objeto y lo devolvemos instantáneamente
                var restaurantesGuardados = JsonSerializer.Deserialize<IEnumerable<Restaurant>>(restaurantesCache);
                return Ok(restaurantesGuardados);
            }

            // 2. Si no estaba en caché (primera vez o expiró), vamos a la base de datos delegando al repositorio
            var restaurants = await _repository.GetAllAsync();

            // 3. Guardamos los datos en Redis para la próxima vez (configuramos que expire en 5 minutos)
            var opcionesCache = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); 

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(restaurants), opcionesCache);

            return Ok(restaurants);
        }

        // Registro de locales: Endpoint restringido exclusivamente a usuarios con rol administrativo
        [HttpPost]
        [Authorize(Roles = "admin")] 
        public async Task<ActionResult<Restaurant>> CreateRestaurant(Restaurant restaurant)
        {
            // La persistencia se delega al repositorio (Postgres o Mongo)
            await _repository.CreateAsync(restaurant);

            // IMPORTANTE: Borramos la caché vieja porque ahora hay un restaurante nuevo.
            // Así garantizamos que la próxima búsqueda traiga los datos frescos.
            await _cache.RemoveAsync("lista_restaurantes_activos");

            return CreatedAtAction(nameof(GetRestaurants), new { id = restaurant.Id }, restaurant);
        }
    }
}