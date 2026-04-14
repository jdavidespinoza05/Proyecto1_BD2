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
    public class OrdersControllerTests
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
        public async Task CreateOrder_ReturnsCreated_WhenValid()
        {
            // Arrange
            var db = GetDatabaseContext();
            var controller = new OrdersController(db);
            var nuevaOrden = new Order { Id = 1, UserId = 1, RestaurantId = 1, TotalAmount = 15000 };

            // Act
            var result = await controller.CreateOrder(nuevaOrden);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var model = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(15000, model.TotalAmount);
        }

        [Fact]
        public async Task CreateOrder_ReturnsBadRequest_WhenAmountIsZero()
        {
            // Arrange
            var db = GetDatabaseContext();
            var controller = new OrdersController(db);
            var ordenMala = new Order { Id = 2, TotalAmount = 0 };

            // Act
            var result = await controller.CreateOrder(ordenMala);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("El monto del pedido debe ser mayor a cero, compa.", badRequest.Value);
        }

        [Fact]
        public async Task GetOrderDetails_ReturnsOrder_WhenExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            db.Orders.Add(new Order { Id = 10, TotalAmount = 5000 });
            await db.SaveChangesAsync();
            var controller = new OrdersController(db);

            // Act
            var result = await controller.GetOrderDetails(10);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Order>>(result);
            var model = Assert.IsType<Order>(actionResult.Value);
            Assert.Equal(10, model.Id);
        }

        [Fact]
        public async Task GetOrderDetails_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            var controller = new OrdersController(db);

            // Act
            var result = await controller.GetOrderDetails(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Mae, no encontramos ese pedido.", notFound.Value);
        }
    }
}