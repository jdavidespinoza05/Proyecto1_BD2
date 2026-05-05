using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestauranteApi.Tests
{
    public class RestaurantsControllerTests
    {
        [Fact]
        public async Task GetRestaurants_ReturnsFromDatabase_WhenCacheIsEmpty()
        {
            // Arrange
            var mockRepo = new Mock<IRestaurantRepository>();
            var mockCache = new Mock<IDistributedCache>();

            var listaRestaurantes = new List<Restaurant> 
            {
                new Restaurant { Id = 1, Name = "Restaurante Central", Address = "Cartago" },
                new Restaurant { Id = 2, Name = "Sucursal Norte", Address = "San José" }
            };

            // 1. Simulamos que la memoria caché está vacía (devuelve null)
            mockCache.Setup(c => c.GetAsync("lista_restaurantes_activos", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((byte[]?)null);

            // 2. Simulamos que la base de datos devuelve los 2 restaurantes
            mockRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(listaRestaurantes);

            var controller = new RestaurantsController(mockRepo.Object, mockCache.Object);

            // Act
            var result = await controller.GetRestaurants();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsAssignableFrom<IEnumerable<Restaurant>>(okResult.Value);
            
            Assert.Equal(2, ((List<Restaurant>)returnedList).Count);

            // Verificaciones críticas de Arquitectura:
            // Aseguramos que SÍ se llamó a la base de datos porque la caché estaba vacía
            mockRepo.Verify(repo => repo.GetAllAsync(), Times.Once);
            
            // Aseguramos que SÍ se mandó a guardar la nueva consulta a Redis
            mockCache.Verify(c => c.SetAsync(
                "lista_restaurantes_activos",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetRestaurants_ReturnsFromCache_WhenCacheHasData()
        {
            // Arrange
            var mockRepo = new Mock<IRestaurantRepository>();
            var mockCache = new Mock<IDistributedCache>();

            var listaCacheada = new List<Restaurant> 
            {
                new Restaurant { Id = 3, Name = "Restaurante Alta Velocidad", Address = "Heredia" }
            };
            
            // Preparamos los bytes exactos que Redis devolvería
            var json = JsonSerializer.Serialize(listaCacheada);
            var bytes = Encoding.UTF8.GetBytes(json);

            // Simulamos que la memoria caché SÍ tiene los datos guardados
            mockCache.Setup(c => c.GetAsync("lista_restaurantes_activos", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(bytes);

            var controller = new RestaurantsController(mockRepo.Object, mockCache.Object);

            // Act
            var result = await controller.GetRestaurants();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsAssignableFrom<IEnumerable<Restaurant>>(okResult.Value);
            
            Assert.Single((List<Restaurant>)returnedList);

            // Verificación crítica de Arquitectura:
            // Aseguramos que NUNCA se llamó a la base de datos porque se usó la caché
            mockRepo.Verify(repo => repo.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateRestaurant_SavesToDatabase_AndClearsCache()
        {
            // Arrange
            var mockRepo = new Mock<IRestaurantRepository>();
            var mockCache = new Mock<IDistributedCache>();
            
            var controller = new RestaurantsController(mockRepo.Object, mockCache.Object);
            var nuevo = new Restaurant { Id = 10, Name = "Restaurante de Prueba", Address = "Sede Principal" };

            // Act
            var result = await controller.CreateRestaurant(nuevo);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var model = Assert.IsType<Restaurant>(createdResult.Value);
            
            Assert.NotNull(model);
            Assert.Equal("Restaurante de Prueba", model.Name);
            
            // Verificamos que se delegó el guardado a la base de datos
            mockRepo.Verify(repo => repo.CreateAsync(nuevo), Times.Once);
            
            // Verificamos que se borró la memoria caché para evitar datos obsoletos
            mockCache.Verify(c => c.RemoveAsync("lista_restaurantes_activos", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}