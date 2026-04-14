using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenusController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MenusController(AppDbContext context)
        {
            _context = context;
        }

        //Creación de platillos protegida: Solo usuarios con rol de "admin" pueden ejecutar esta acción
        [HttpPost]
        [Authorize(Roles = "admin")] 
        public async Task<ActionResult<Menu>> CreateMenu(Menu menu)
        {
            //Validación de integridad referencial: Verifica que el restaurante asociado exista
            var restaurantExists = await _context.Restaurants.AnyAsync(r => r.Id == menu.RestaurantId);
            if (!restaurantExists) return BadRequest("El restaurante no existe.");

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMenuDetails), new { id = menu.Id }, menu);
        }

        //Endpoint público: Permite a cualquier usuario autenticado consultar detalles de un platillo
        [HttpGet("{id}")]
        public async Task<ActionResult<Menu>> GetMenuDetails(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound("Ese plato no se encuentra en el menú.");
            
            return menu;
        }

        //Actualización de recursos: Implementa lógica de concurrencia y restricción por rol administrativo
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateMenu(int id, Menu menu)
        {
            if (id != menu.Id) return BadRequest("El ID de la ruta no coincide con el del platillo.");

            _context.Entry(menu).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Menus.Any(e => e.Id == id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        //Eliminación física: Acción restringida a administradores para la gestión del catálogo
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}