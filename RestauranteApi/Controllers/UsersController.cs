using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using RestaurantesApi.Models;
using System.Security.Claims;

namespace RestaurantesApi.Controllers
{
    [Route("users")] 
    [ApiController]
    //Requerimiento de Seguridad: El atributo [Authorize] activa el Middleware de JWT para proteger todos los endpoints
    [Authorize] 
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        //Recuperación de perfil: Extrae la identidad del usuario directamente desde los claims del Token JWT
        [HttpGet("me")]
        public async Task<ActionResult<Usuario>> GetMe()
        {
            // Extracción de claims: Se busca el correo electrónico dentro de la carga útil del token
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value 
                            ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (string.IsNullOrEmpty(emailClaim))
            {
                return BadRequest(new { mensaje = "El token no contiene un correo válido." });
            }

            //Sincronización: Busca los datos extendidos del usuario en la base de datos PostgreSQL local
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == emailClaim);

            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado en la base de datos local." });

            return Ok(usuario);
        }

        //Gestión de datos locales: Actualiza la información del usuario en el sistema de persistencia
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, Usuario usuario)
        {
            if (id != usuario.Id) return BadRequest(new { mensaje = "El ID de la ruta no coincide con el del cuerpo." });

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Usuarios.Any(e => e.Id == id)) return NotFound(new { mensaje = "El usuario no existe." });
                throw;
            }

            return Ok(new { mensaje = "Usuario actualizado correctamente." });
        }

        //Eliminación local: Remueve el registro del usuario de la base de datos del sistema
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario eliminado con éxito de la base de datos." });
        }
    }
}