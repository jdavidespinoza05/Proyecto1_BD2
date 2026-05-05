using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories;
using System.Threading.Tasks;

namespace RestauranteApi.Tests
{
    public class MenusControllerTests
    {
        [Fact]
        public async Task CreateMenu_ReturnsCreated_WhenRestaurantExists()
        {
            // Arrange
            var mockRepo = new Mock<IMenuRepository>();
            var nuevoPlato = new Menu { Id = 1, Name = "Chifrijo", Price = 3500, RestaurantId = 1 };
            
            // Este método devuelve un booleano, así que sí ocupamos ReturnsAsync
            mockRepo.Setup(repo => repo.RestaurantExistsAsync(1)).ReturnsAsync(true);
            
            // CreateAsync devuelve Task (void). Solo lo simulamos sin Returns. Moq hace el resto.
            mockRepo.Setup(repo => repo.CreateAsync(It.IsAny<Menu>()));

            var controller = new MenusController(mockRepo.Object);

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
            var mockRepo = new Mock<IMenuRepository>();
            
            mockRepo.Setup(repo => repo.RestaurantExistsAsync(999)).ReturnsAsync(false);

            var controller = new MenusController(mockRepo.Object);
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
            var mockRepo = new Mock<IMenuRepository>();
            var platoFalso = new Menu { Id = 10, Name = "Tres Leches", Price = 1500, RestaurantId = 1 };
            
            // Este método devuelve un Menu, así que sí ocupamos ReturnsAsync
            mockRepo.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(platoFalso);
            
            var controller = new MenusController(mockRepo.Object);

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
            var mockRepo = new Mock<IMenuRepository>();
            var menu = new Menu { Id = 5, Name = "Pizza", Price = 8500, RestaurantId = 1 };
            
            // UpdateAsync es Task. Solo lo "preparamos".
            mockRepo.Setup(repo => repo.UpdateAsync(It.IsAny<Menu>()));

            var controller = new MenusController(mockRepo.Object);

            // Act
            var result = await controller.UpdateMenu(5, menu);

            // Assert
            Assert.IsType<NoContentResult>(result);
            mockRepo.Verify(repo => repo.UpdateAsync(menu), Times.Once); 
        }

        [Fact]
        public async Task DeleteMenu_ReturnsNoContent_WhenExists()
        {
            // Arrange
            var mockRepo = new Mock<IMenuRepository>();
            var platoFalso = new Menu { Id = 7, Name = "Sopa", Price = 2000, RestaurantId = 1 };
            
            mockRepo.Setup(repo => repo.GetByIdAsync(7)).ReturnsAsync(platoFalso);
            // DeleteAsync es Task. 
            mockRepo.Setup(repo => repo.DeleteAsync(7));
            
            var controller = new MenusController(mockRepo.Object);

            // Act
            var result = await controller.DeleteMenu(7);

            // Assert
            Assert.IsType<NoContentResult>(result);
            mockRepo.Verify(repo => repo.DeleteAsync(7), Times.Once);
        }
    }
}