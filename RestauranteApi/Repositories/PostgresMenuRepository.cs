/*
Se encarga de manejar las operaciones relacionadas con los menús en la base
de datos PostgreSQL.

Esta clase funciona como una capa intermedia entre la API y la base de datos.
En lugar de que los controladores trabajen directamente con Entity Framework,
utilizan este repositorio para crear, consultar, actualizar o eliminar menús.

También permite verificar si un restaurante existe antes de asociarle un menú,
lo cual ayuda a mantener la integridad de los datos. De esta forma, se evita
registrar menús para restaurantes que no estén almacenados en el sistema.

El uso de IMenuRepository permite mantener separada la lógica de acceso a datos
de la lógica principal de la aplicación, haciendo que el código sea más ordenado,
fácil de mantener y más flexible ante futuros cambios.
*/

using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class PostgresMenuRepository : IMenuRepository
    {
        private readonly AppDbContext _context;

        public PostgresMenuRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> RestaurantExistsAsync(int restaurantId)
        {
            return await _context.Restaurants.AnyAsync(r => r.Id == restaurantId);
        }

        public async Task<Menu> CreateAsync(Menu menu)
        {
            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();
            return menu;
        }

        public async Task<Menu?> GetByIdAsync(int id)
        {
            return await _context.Menus.FindAsync(id);
        }

        public async Task UpdateAsync(Menu menu)
        {
            _context.Entry(menu).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> MenuExistsAsync(int id)
        {
            return await _context.Menus.AnyAsync(e => e.Id == id);
        }

        public async Task DeleteAsync(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu != null)
            {
                _context.Menus.Remove(menu);
                await _context.SaveChangesAsync();
            }
        }
    }
}