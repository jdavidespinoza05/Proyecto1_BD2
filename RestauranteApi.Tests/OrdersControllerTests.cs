using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories; 
using System.Threading.Tasks;

namespace RestauranteApi.Tests
{
    public class OrdersControllerTests
    {
        [Fact]
        public async Task CreateOrder_ReturnsCreated_WhenValid()
        {
            // Arrange
            var mockRepo = new Mock<IOrderRepository>();
            var controller = new OrdersController(mockRepo.Object);
            var nuevaOrden = new Order { Id = 1, UserId = 1, RestaurantId = 1, TotalAmount = 15000 };

            mockRepo.Setup(repo => repo.CreateAsync(It.IsAny<Order>()));

            // Act
            var result = await controller.CreateOrder(nuevaOrden);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var model = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(15000, model.TotalAmount);
            
            mockRepo.Verify(repo => repo.CreateAsync(nuevaOrden), Times.Once);
        }

        [Fact]
        public async Task CreateOrder_ReturnsBadRequest_WhenAmountIsZero()
        {
            // Arrange
            var mockRepo = new Mock<IOrderRepository>();
            var controller = new OrdersController(mockRepo.Object);
            var ordenMala = new Order { Id = 2, TotalAmount = 0 };

            // Act
            var result = await controller.CreateOrder(ordenMala);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            // Validamos contra el nuevo mensaje formal
            Assert.Equal("El monto del pedido debe ser mayor a cero.", badRequest.Value);
            
            mockRepo.Verify(repo => repo.CreateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task GetOrderDetails_ReturnsOrder_WhenExists()
        {
            // Arrange
            var mockRepo = new Mock<IOrderRepository>();
            var ordenFalsa = new Order { Id = 10, TotalAmount = 5000 };
            
            mockRepo.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(ordenFalsa);
            
            var controller = new OrdersController(mockRepo.Object);

            // Act
            var result = await controller.GetOrderDetails(10);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Order>>(result);
            Assert.NotNull(actionResult.Value);
            Assert.Equal(10, actionResult.Value.Id);
        }

        [Fact]
        public async Task GetOrderDetails_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            var mockRepo = new Mock<IOrderRepository>();
            
            mockRepo.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((Order)null);
            
            var controller = new OrdersController(mockRepo.Object);

            // Act
            var result = await controller.GetOrderDetails(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            // Validamos contra el nuevo mensaje formal
            Assert.Equal("No se encontró el pedido solicitado.", notFound.Value);
        }
    }
}