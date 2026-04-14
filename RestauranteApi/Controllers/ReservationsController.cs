using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReservationsController(AppDbContext context) { _context = context; }

        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<Reservation>> CreateReservation(Reservation res)
        {
            if (res.ReservationDate < DateTime.Now) 
                return BadRequest("No puede reservar en el pasado, compa.");

            _context.Reservations.Add(res);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateReservation), new { id = res.Id }, res);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var res = await _context.Reservations.FindAsync(id);
            if (res == null) return NotFound();

            _context.Reservations.Remove(res);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}