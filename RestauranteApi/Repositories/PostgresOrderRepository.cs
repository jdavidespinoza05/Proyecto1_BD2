/*
Se encarga de manejar las operaciones relacionadas con las órdenes en la base
de datos PostgreSQL.

Esta clase permite registrar nuevas órdenes y consultar órdenes existentes por
medio de su identificador. Funciona como una capa de acceso a datos, evitando
que otras partes de la aplicación tengan que interactuar directamente con
Entity Framework o con el contexto de base de datos.

Su propósito es mantener organizada la lógica de persistencia de las órdenes,
dejando que los controladores o servicios solo llamen métodos específicos como
crear una orden o buscarla por ID.

Al implementar IOrderRepository, el sistema mantiene una estructura más limpia
y separa mejor las responsabilidades entre la lógica de negocio y el acceso a
la base de datos.
*/

using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class PostgresOrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public PostgresOrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }
    }
}