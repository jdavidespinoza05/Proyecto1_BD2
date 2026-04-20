using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories;
using System.Security.Claims;

namespace RestaurantesApi.Controllers
{
    [Route("users")] 
    [ApiController]
    [Authorize] 
    public class UsersController : ControllerBase
    {
        private readonly IUsuarioRepository _repository; 

        public UsersController(IUsuarioRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("me")]
        public async Task<ActionResult<Usuario>> GetMe()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value 
                             ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (string.IsNullOrEmpty(emailClaim))
                return BadRequest(new { mensaje = "El token no contiene un correo válido." });

            var usuario = await _repository.GetByEmailAsync(emailClaim);

            if (usuario == null) 
                return NotFound(new { mensaje = "Usuario no encontrado en la base de datos local." });

            return Ok(usuario);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, Usuario usuario)
        {
            if (id != usuario.Id) 
                return BadRequest(new { mensaje = "El ID de la ruta no coincide." });

            try
            {
                await _repository.UpdateAsync(usuario); 
            }
            catch (Exception)
            {
                if (!await _repository.UserExistsAsync(id)) 
                    return NotFound(new { mensaje = "El usuario no existe." });
                throw;
            }

            return Ok(new { mensaje = "Usuario actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var usuario = await _repository.GetByIdAsync(id); 
            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });

            await _repository.DeleteAsync(usuario); 

            return Ok(new { mensaje = "Usuario eliminado con éxito." });
        }
    }
}