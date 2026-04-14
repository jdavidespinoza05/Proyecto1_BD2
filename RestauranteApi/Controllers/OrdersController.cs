using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {

            if (order.TotalAmount <= 0) 
                return BadRequest("El monto del pedido debe ser mayor a cero, compa.");

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrderDetails), new { id = order.Id }, order);
        }

        [HttpGet("{id}")]
        [Authorize] 
        public async Task<ActionResult<Order>> GetOrderDetails(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            
            if (order == null) return NotFound("Mae, no encontramos ese pedido.");

            return order;
        }
    }
}