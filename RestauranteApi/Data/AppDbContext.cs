using Microsoft.EntityFrameworkCore;
using RestaurantesApi.Models;

namespace RestaurantesApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Estas son las variables en C#
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Usuario>().ToTable("users");           
            modelBuilder.Entity<Restaurant>().ToTable("restaurants");
            modelBuilder.Entity<Menu>().ToTable("menus");
            modelBuilder.Entity<Reservation>().ToTable("reservations");
            modelBuilder.Entity<Order>().ToTable("orders");

            base.OnModelCreating(modelBuilder);
        }
    }
}