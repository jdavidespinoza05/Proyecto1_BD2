using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using RestaurantesApi.Data;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace RestauranteApi.Tests
{
    public class RestaurantsControllerTests
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
        public async Task GetRestaurants_ReturnsList()
        {
            // Arrange
            var db = GetDatabaseContext();
            db.Restaurants.Add(new Restaurant { Id = 1, Name = "Soda El Tec", Address = "Cartago" });
            db.Restaurants.Add(new Restaurant { Id = 2, Name = "Piccola Italia", Address = "San José" });
            await db.SaveChangesAsync();

            var controller = new RestaurantsController(db);

            // Act
            var result = await controller.GetRestaurants();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<Restaurant>>>(result);
            Assert.NotNull(actionResult.Value);
            var list = Assert.IsAssignableFrom<IEnumerable<Restaurant>>(actionResult.Value);
            Assert.Equal(2, ((List<Restaurant>)list).Count);
        }

        [Fact]
        public async Task CreateRestaurant_SavesCorrectly()
        {
            // Arrange
            var db = GetDatabaseContext();
            var controller = new RestaurantsController(db);
            var nuevo = new Restaurant { Id = 10, Name = "Restaurante de Prueba", Address = "TEC" };

            // Act
            var result = await controller.CreateRestaurant(nuevo);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var model = Assert.IsType<Restaurant>(createdResult.Value);
            
            Assert.NotNull(model);
            Assert.Equal("Restaurante de Prueba", model.Name);
            
            // Verificar que sí se guardó en la DB "falsa"
            Assert.Equal(1, await db.Restaurants.CountAsync());
        }
    }
}