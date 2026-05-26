/*
Se encarga de manejar las reservas de los usuarios en los restaurantes.

Sigue el mismo patrón que los demás controladores usando su propio 
repositorio (IReservationRepository) para las operaciones de base de datos.

Cualquier usuario con sesión iniciada ([Authorize]) puede hacer o cancelar 
sus reservas. Como detalle extra, tiene una validación lógica muy importante: 
comprueba que la fecha de la reservación no sea en el pasado antes de 
guardarla en el sistema.
*/

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

            await _repository.DeleteAsync(res); 

            return NoContent();
        }
    }
}