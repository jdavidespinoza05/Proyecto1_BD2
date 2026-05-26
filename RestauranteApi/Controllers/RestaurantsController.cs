/*
 * RestaurantsController
 * Se encarga de gestionar la información de los restaurantes.
 * Aparte de usar su repositorio (IRestaurantRepository), este controlador 
 * tiene la particularidad de implementar caché distribuida (IDistributedCache) 
 * para hacer que la API responda mucho más rápido.
 * Al pedir la lista de restaurantes, primero busca en la caché; si la 
 * encuentra, la devuelve de inmediato. Si no, va a la base de datos y 
 * guarda esos resultados en memoria por 5 minutos.
 * Crear un restaurante es exclusivo para administradores y tiene un 
 * detalle clave: al agregar uno nuevo, el sistema borra la caché 
 * automáticamente para forzar que la lista se actualice en la próxima consulta.
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories; 

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController] 
    public class RestaurantsController : ControllerBase
    {
        private readonly IRestaurantRepository _repository;
        private readonly IDistributedCache _cache; 

        public RestaurantsController(IRestaurantRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache; 
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurants()
        {
            string cacheKey = "lista_restaurantes_activos";

            var restaurantesCache = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(restaurantesCache))
            {
                var restaurantesGuardados = JsonSerializer.Deserialize<IEnumerable<Restaurant>>(restaurantesCache);
                return Ok(restaurantesGuardados);
            }

            var restaurants = await _repository.GetAllAsync();

            var opcionesCache = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); 

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(restaurants), opcionesCache);

            return Ok(restaurants);
        }

        [HttpPost]
        [Authorize(Roles = "admin")] 
        public async Task<ActionResult<Restaurant>> CreateRestaurant(Restaurant restaurant)
        {
            await _repository.CreateAsync(restaurant);

            await _cache.RemoveAsync("lista_restaurantes_activos");

            return CreatedAtAction(nameof(GetRestaurants), new { id = restaurant.Id }, restaurant);
        }
    }
}