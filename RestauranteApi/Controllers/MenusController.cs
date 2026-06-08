/*
Se encarga de gestionar los platillos (menús) de los restaurantes.

Al igual que en autenticación, usamos un repositorio (IMenuRepository) para 
mantener el acceso a la base de datos separado del controlador.
Contiene las operaciones básicas (CRUD): crear, buscar, editar y eliminar.

Un detalle clave de seguridad en este archivo es que cualquier usuario puede 
ver los detalles de un plato, pero protegemos con [Authorize(Roles = "admin")] 
los métodos de crear, modificar o borrar. Además, antes de agregar un platillo 
nuevo, siempre verificamos que el restaurante asociado realmente exista.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed; // <-- Librería de Caché
using System.Text.Json; // <-- Para serializar a JSON
using RestaurantesApi.Models;
using RestaurantesApi.Repositories;

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenusController : ControllerBase
    {
        private readonly IMenuRepository _repository;
        private readonly IDistributedCache _cache; 

        public MenusController(IMenuRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache; 
        }

        [HttpPost]
        [Authorize(Roles = "admin")] 
        public async Task<ActionResult<Menu>> CreateMenu(Menu menu)
        {
            var restaurantExists = await _repository.RestaurantExistsAsync(menu.RestaurantId);
            if (!restaurantExists) return BadRequest("El restaurante no existe.");

            await _repository.CreateAsync(menu);

            return CreatedAtAction(nameof(GetMenuDetails), new { id = menu.Id }, menu);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Menu>> GetMenuDetails(int id)
        {
            string cacheKey = $"menu_detalle_{id}";

            var menuCache = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(menuCache))
            {
                var menuGuardado = JsonSerializer.Deserialize<Menu>(menuCache);
                return Ok(menuGuardado);
            }

            var menu = await _repository.GetByIdAsync(id);
            if (menu == null) return NotFound("Ese plato no se encuentra en el menú.");
            
            var opcionesCache = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(menu), opcionesCache);

            return menu;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateMenu(int id, Menu menu)
        {
            if (id != menu.Id) return BadRequest("El ID de la ruta no coincide con el del platillo.");

            try
            {
                await _repository.UpdateAsync(menu);

                await _cache.RemoveAsync($"menu_detalle_{id}");
            }
            catch (Exception)
            {
                if (!await _repository.MenuExistsAsync(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await _repository.GetByIdAsync(id);
            if (menu == null) return NotFound();

            await _repository.DeleteAsync(id);

            await _cache.RemoveAsync($"menu_detalle_{id}");

            return NoContent();
        }
    }
}