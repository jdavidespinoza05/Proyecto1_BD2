using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using RestaurantesApi.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestauranteApi.Tests
{
    public class SearchControllerTests
    {
        [Fact]
        public async Task SearchProducts_ReturnsOk_WhenKeywordIsValid()
        {
            // Arrange
            var mockService = new Mock<ISearchService>();
            var resultadosFalsos = new List<ProductoBusqueda> 
            { 
                new ProductoBusqueda { Id = 1, Nombre = "Hamburguesa Clásica" } 
            };
            
            mockService.Setup(s => s.SearchProductsAsync("hamburguesa")).ReturnsAsync(resultadosFalsos);
            
            var controller = new SearchController(mockService.Object);

            // Act
            var result = await controller.SearchProducts("hamburguesa");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var itemsRetornados = Assert.IsAssignableFrom<IEnumerable<ProductoBusqueda>>(okResult.Value);
            Assert.Single(itemsRetornados);
        }

        [Fact]
        public async Task SearchProducts_ReturnsBadRequest_WhenKeywordIsEmpty()
        {
            // Arrange
            var mockService = new Mock<ISearchService>();
            var controller = new SearchController(mockService.Object);

            // Act
            var result = await controller.SearchProducts("");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Debes enviar una palabra clave en el parámetro 'keyword'.", badRequest.Value);
        }

        [Fact]
        public async Task SearchByCategory_ReturnsOk_WhenKeywordIsValid()
        {
            // Arrange
            var mockService = new Mock<ISearchService>();
            var resultadosFalsos = new List<ProductoBusqueda> 
            { 
                new ProductoBusqueda { Id = 3, Nombre = "Refresco de Cola", Categoria = "Bebidas" } 
            };

            mockService.Setup(s => s.SearchByCategoryAsync("Bebidas", "cola")).ReturnsAsync(resultadosFalsos);
            
            var controller = new SearchController(mockService.Object);

            // Act
            var result = await controller.SearchByCategory("Bebidas", "cola");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var itemsRetornados = Assert.IsAssignableFrom<IEnumerable<ProductoBusqueda>>(okResult.Value);
            Assert.Single(itemsRetornados);
        }

        [Fact]
        public async Task SearchByCategory_ReturnsBadRequest_WhenKeywordIsEmpty()
        {
            // Arrange
            var mockService = new Mock<ISearchService>();
            var controller = new SearchController(mockService.Object);

            // Act
            var result = await controller.SearchByCategory("Bebidas", " ");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Debes enviar una palabra clave en el parámetro 'keyword'.", badRequest.Value);
        }

        [Fact]
        public async Task Reindex_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var mockService = new Mock<ISearchService>();
            mockService.Setup(s => s.ReindexAllAsync(It.IsAny<IEnumerable<ProductoBusqueda>>())).ReturnsAsync(true);
            
            var controller = new SearchController(mockService.Object);

            // Act
            var result = await controller.Reindex();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // C# anónimo a string para validar el mensaje
            var mensajePropiedad = okResult.Value?.GetType().GetProperty("Mensaje")?.GetValue(okResult.Value, null);
            Assert.Equal("Índice de ElasticSearch reconstruido con éxito. Reglas de negocio aplicadas.", mensajePropiedad);
        }

        [Fact]
        public async Task Reindex_ReturnsInternalServerError_WhenFails()
        {
            // Arrange
            var mockService = new Mock<ISearchService>();
            mockService.Setup(s => s.ReindexAllAsync(It.IsAny<IEnumerable<ProductoBusqueda>>())).ReturnsAsync(false);
            
            var controller = new SearchController(mockService.Object);

            // Act
            var result = await controller.Reindex();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            
            var mensajePropiedad = objectResult.Value?.GetType().GetProperty("Mensaje")?.GetValue(objectResult.Value, null);
            Assert.Equal("Error interno al comunicarse con ElasticSearch.", mensajePropiedad);
        }
    }
}