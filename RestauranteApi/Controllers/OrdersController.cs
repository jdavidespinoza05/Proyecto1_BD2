/*
 * OrdersController
 * Se encarga de gestionar los pedidos de los clientes.
 * Mantiene la misma línea limpia usando IOrderRepository para comunicarse 
 * con la base de datos sin mezclar la lógica en el controlador.
 * A diferencia de los menús, aquí usamos [Authorize] de forma general sin 
 * especificar roles. Esto significa que cualquier usuario que haya iniciado 
 * sesión puede crear o ver sus pedidos (no necesitan ser administradores). 
 * También incluye una pequeña regla de negocio: revisa que el total del 
 * pedido sea mayor a cero antes de intentar guardarlo.
 */

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