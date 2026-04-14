using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;
using Moq;
using RestaurantesApi.Data;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;

namespace RestauranteApi.Tests
{
    public class UsersControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new AppDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact]
        public async Task GetMe_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            var email = "david@estudiantec.cr";
            db.Usuarios.Add(new Usuario { Id = 1, Correo = email, Nombre = "David" });
            await db.SaveChangesAsync();

            var controller = new UsersController(db);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email) }));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            // Act
            var result = await controller.GetMe();

            // ASSERT CORREGIDO:
            // Accedemos a .Result porque el método devuelve ActionResult<T>
            var okResult = Assert.IsType<OkObjectResult>(result.Result); 
            var returnedUser = Assert.IsType<Usuario>(okResult.Value);
            Assert.Equal(email, returnedUser.Correo);
        }

        [Fact]
        public async Task UpdateUser_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var db = GetDatabaseContext();
            var usuario = new Usuario { Id = 1, Nombre = "Original", Correo = "test@test.com" };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            var controller = new UsersController(db);
            usuario.Nombre = "Actualizado";

            // Act
            var result = await controller.UpdateUser(1, usuario);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var userInDb = await db.Usuarios.FindAsync(1);
            Assert.Equal("Actualizado", userInDb.Nombre);
        }

        [Fact]
        public async Task DeleteUser_ReturnsOk_WhenUserExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            db.Usuarios.Add(new Usuario { Id = 1, Nombre = "ABorrar", Correo = "borrar@test.com" });
            await db.SaveChangesAsync();

            var controller = new UsersController(db);

            // Act
            var result = await controller.DeleteUser(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Empty(db.Usuarios);
        }
    }
}