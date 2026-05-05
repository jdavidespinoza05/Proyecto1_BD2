using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories;

namespace RestaurantesApi.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationRepository _repository; 

        public ReservationsController(IReservationRepository repository) 
        { 
            _repository = repository; 
        }

        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<Reservation>> CreateReservation(Reservation res)
        {
            // Estandarizamos el mensaje a un tono profesional
            if (res.ReservationDate < DateTime.Now) 
                return BadRequest("La fecha de reservación no puede ser en el pasado.");

            await _repository.CreateAsync(res); 

            return CreatedAtAction(nameof(CreateReservation), new { id = res.Id }, res);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var res = await _repository.GetByIdAsync(id); 
            if (res == null) return NotFound();

            // Aquí notamos que DeleteAsync recibe el objeto 'res' completo
            await _repository.DeleteAsync(res); 

            return NoContent();
        }
    }
}