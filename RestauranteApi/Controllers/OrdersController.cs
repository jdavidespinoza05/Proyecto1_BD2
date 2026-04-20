using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories; // <-- Nueva referencia

namespace RestaurantesApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class OrdersController : ControllerBase
    {
        // Inyectamos la Interfaz
        private readonly IOrderRepository _repository; 

        public OrdersController(IOrderRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            // Las validaciones de negocio se quedan en el controller
            if (order.TotalAmount <= 0) 
                return BadRequest("El monto del pedido debe ser mayor a cero, compa.");

            // Delegamos el guardado a la base de datos
            await _repository.CreateAsync(order); 

            return CreatedAtAction(nameof(GetOrderDetails), new { id = order.Id }, order);
        }

        [HttpGet("{id}")]
        [Authorize] 
        public async Task<ActionResult<Order>> GetOrderDetails(int id)
        {
            // Delegamos la búsqueda
            var order = await _repository.GetByIdAsync(id); 
            
            if (order == null) return NotFound("Mae, no encontramos ese pedido.");

            return order;
        }
    }
}