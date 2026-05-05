using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories;

namespace RestauranteApi.Tests
{
    public class AuthControllerTests
    {
        // --- TRUCO: Simulador de respuestas HTTP para Keycloak ---
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
            var mockRepo = new Mock<IUsuarioRepository>(); 
            var mockFactory = new Mock<IHttpClientFactory>(); 
            
            var controller = new AuthController(mockFactory.Object, mockRepo.Object);
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
            var mockRepo = new Mock<IUsuarioRepository>();
            var fakeToken = "{\"access_token\":\"token_de_mentira\"}";
            var mockFactory = CreateMockHttpClientFactory(HttpStatusCode.OK, fakeToken);
            
            var controller = new AuthController(mockFactory.Object, mockRepo.Object);
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
            var mockRepo = new Mock<IUsuarioRepository>();
            
            // Usamos exactamente el nombre del método de tu controlador
            mockRepo.Setup(repo => repo.GetByEmailAsync("yaexiste@mail.com"))
                    .ReturnsAsync(new Usuario { Id = 1, Correo = "yaexiste@mail.com", Nombre = "David" });

            var mockFactory = new Mock<IHttpClientFactory>();
            var controller = new AuthController(mockFactory.Object, mockRepo.Object);
            var request = new RegisterRequest { Email = "yaexiste@mail.com" };

            // Act
            var result = await controller.Register(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("El correo ya está registrado", badRequest.Value?.ToString());
        }
    }
}