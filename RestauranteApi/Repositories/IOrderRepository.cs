/*
Es el contrato que dicta las reglas para manejar los pedidos (órdenes) 
de los clientes en la base de datos.
A diferencia de los menús, aquí la lista de tareas es súper corta: 
solo exige poder crear un pedido nuevo y buscar uno existente por su ID. 

Tiene mucho sentido en la vida real, ya que normalmente un pedido, 
una vez facturado o enviado a la cocina, no se debería estar borrando 
ni modificando libremente.
*/

using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order);
        Task<Order?> GetByIdAsync(int id);
    }
}