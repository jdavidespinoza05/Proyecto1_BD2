using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;
using RestaurantesApi.Controllers;
using RestaurantesApi.Data;
using RestaurantesApi.Models;

namespace RestauranteApi.Tests
{
    public class AuthControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // --- TRUCO: Simulador de respuestas HTTP ---
        private Mock<IHttpClientFactory> CreateMockHttpClientFactory(HttpStatusCode statusCode, string content)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content),
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return mockFactory;
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenUsernameEmpty()
        {
            // Arrange
            var db = GetDatabaseContext();
            var mockFactory = new Mock<IHttpClientFactory>();
            var controller = new AuthController(mockFactory.Object, db);
            var request = new LoginRequest { Username = "" };

            // Act
            var result = await controller.Login(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenKeycloakSucceeds()
        {
            // Arrange
            var db = GetDatabaseContext();
            var fakeToken = "{\"access_token\":\"token_de_mentira\"}";
            var mockFactory = CreateMockHttpClientFactory(HttpStatusCode.OK, fakeToken);
            var controller = new AuthController(mockFactory.Object, db);
            var request = new LoginRequest { Username = "david", Password = "123" };

            // Act
            var result = await controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(fakeToken, okResult.Value);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUserExistsInDb()
        {
            // Arrange
            var db = GetDatabaseContext();
            db.Usuarios.Add(new Usuario { Id = 1, Correo = "yaexiste@mail.com", Nombre = "David" });
            await db.SaveChangesAsync();

            var mockFactory = new Mock<IHttpClientFactory>();
            var controller = new AuthController(mockFactory.Object, db);
            var request = new RegisterRequest { Email = "yaexiste@mail.com" };

            // Act
            var result = await controller.Register(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            // Verificamos que el mensaje sea el que puso tu compa
            Assert.Contains("El correo ya está registrado", badRequest.Value?.ToString());
        }
    }
}