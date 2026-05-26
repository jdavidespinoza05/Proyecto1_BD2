/*
Se encarga de manejar las operaciones relacionadas con los usuarios en la base
de datos PostgreSQL.

Esta clase permite buscar usuarios por correo electrónico o por identificador,
crear nuevos usuarios, actualizar su información, verificar si existen y
eliminarlos cuando sea necesario.

Funciona como una capa intermedia entre la lógica de la aplicación y la base de
datos local. Esto es útil porque el sistema también utiliza Keycloak para la
autenticación, pero mantiene ciertos datos básicos del usuario dentro de la base
de datos propia del proyecto.

Al usar IUsuarioRepository, se separa la lógica de acceso a datos del resto de
la aplicación. Esto hace que el código sea más claro, más fácil de probar y más
sencillo de modificar si en el futuro cambia la forma en que se almacenan los
usuarios.
*/

using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestaurantesApi.Repositories
{
    public class PostgresUsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _context;

        public PostgresUsuarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            return await _context.Usuarios.FindAsync(id);
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task UpdateAsync(Usuario usuario)
        {
            _context.Entry(usuario).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            return await _context.Usuarios.AnyAsync(e => e.Id == id);
        }

        public async Task DeleteAsync(Usuario usuario)
        {
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
        }
    }
}