using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories; 

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _repository; 

        public OrdersController(IOrderRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            if (order.TotalAmount <= 0) 
                return BadRequest("El monto del pedido debe ser mayor a cero.");

            await _repository.CreateAsync(order); 

            return CreatedAtAction(nameof(GetOrderDetails), new { id = order.Id }, order);
        }

        [HttpGet("{id}")]
        [Authorize] 
        public async Task<ActionResult<Order>> GetOrderDetails(int id)
        {
            var order = await _repository.GetByIdAsync(id); 
            
            if (order == null) return NotFound("No se encontró el pedido solicitado.");

            return order;
        }
    }
}