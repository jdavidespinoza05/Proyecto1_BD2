using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories; // ¡Importante agregar esto!

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenusController : ControllerBase
    {
        private readonly IMenuRepository _repository;

        // Inyectamos la Interfaz, no el AppDbContext
        public MenusController(IMenuRepository repository)
        {
            _repository = repository;
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
            var menu = await _repository.GetByIdAsync(id);
            if (menu == null) return NotFound("Ese plato no se encuentra en el menú.");
            
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

            return NoContent();
        }
    }
}