using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories; // <-- Nueva referencia

namespace RestaurantesApi.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        // Inyectamos la Interfaz
        private readonly IReservationRepository _repository; 

        public ReservationsController(IReservationRepository repository) 
        { 
            _repository = repository; 
        }

        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<Reservation>> CreateReservation(Reservation res)
        {
            // La lógica de negocio se mantiene
            if (res.ReservationDate < DateTime.Now) 
                return BadRequest("No puede reservar en el pasado.");

            // Delegamos a la base de datos
            await _repository.CreateAsync(res); 

            return CreatedAtAction(nameof(CreateReservation), new { id = res.Id }, res);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelReservation(int id)
        {
            // Buscamos a través del repositorio
            var res = await _repository.GetByIdAsync(id); 
            if (res == null) return NotFound();

            // Borramos a través del repositorio
            await _repository.DeleteAsync(res); 

            return NoContent();
        }
    }
}