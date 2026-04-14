using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using RestaurantesApi.Data;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using System.Threading.Tasks;
using System;

namespace RestauranteApi.Tests
{
    public class MenusControllerTests
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
        public async Task CreateMenu_ReturnsCreated_WhenRestaurantExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            // Ocupamos que el restaurante 1 exista para que el controlador nos deje pasar
            db.Restaurants.Add(new Restaurant { Id = 1, Name = "Soda TEC", Address = "Cartago" });
            await db.SaveChangesAsync();

            var controller = new MenusController(db);
            var nuevoPlato = new Menu { Id = 1, Name = "Chifrijo", Price = 3500, RestaurantId = 1 };

            // Act
            var result = await controller.CreateMenu(nuevoPlato);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var model = Assert.IsType<Menu>(createdResult.Value);
            Assert.Equal("Chifrijo", model.Name);
        }

        [Fact]
        public async Task CreateMenu_ReturnsBadRequest_WhenRestaurantMissing()
        {
            // Arrange
            var db = GetDatabaseContext();
            var controller = new MenusController(db);
            var platoHuerfano = new Menu { Id = 2, Name = "Casado", Price = 3000, RestaurantId = 999 };

            // Act
            var result = await controller.CreateMenu(platoHuerfano);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("El restaurante no existe.", badRequest.Value);
        }

        [Fact]
        public async Task GetMenuDetails_ReturnsMenu_WhenExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            db.Menus.Add(new Menu { Id = 10, Name = "Tres Leches", Price = 1500, RestaurantId = 1 });
            await db.SaveChangesAsync();
            var controller = new MenusController(db);

            // Act
            var result = await controller.GetMenuDetails(10);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Menu>>(result);
            Assert.NotNull(actionResult.Value);
            Assert.Equal("Tres Leches", actionResult.Value.Name);
        }

        [Fact]
        public async Task UpdateMenu_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            var db = GetDatabaseContext();
            var menu = new Menu { Id = 5, Name = "Pizza", Price = 8000, RestaurantId = 1 };
            db.Menus.Add(menu);
            await db.SaveChangesAsync();

            var controller = new MenusController(db);
            menu.Price = 8500;

            // Act
            var result = await controller.UpdateMenu(5, menu);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var menuInDb = await db.Menus.FindAsync(5);
            Assert.Equal(8500, menuInDb!.Price);
        }

        [Fact]
        public async Task DeleteMenu_ReturnsNoContent_WhenExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            db.Menus.Add(new Menu { Id = 7, Name = "Sopa", Price = 2000, RestaurantId = 1 });
            await db.SaveChangesAsync();
            var controller = new MenusController(db);

            // Act
            var result = await controller.DeleteMenu(7);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(db.Menus);
        }
    }
}