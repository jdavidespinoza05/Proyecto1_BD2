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