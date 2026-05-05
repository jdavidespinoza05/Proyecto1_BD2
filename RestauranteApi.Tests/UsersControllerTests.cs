using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Moq;
using Xunit;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories;

namespace RestauranteApi.Tests
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetMe_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var mockRepo = new Mock<IUsuarioRepository>();
            var email = "david@estudiantec.cr";
            var expectedUser = new Usuario { Id = 1, Correo = email, Nombre = "David" };

            // Simulamos que el repositorio encuentra al usuario por su correo
            mockRepo.Setup(repo => repo.GetByEmailAsync(email)).ReturnsAsync(expectedUser);

            var controller = new UsersController(mockRepo.Object);

            // Simulamos el Token JWT inyectando el claim del correo en el contexto HTTP
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email) }));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            // Act
            var result = await controller.GetMe();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<Usuario>(okResult.Value);
            Assert.Equal(email, returnedUser.Correo);
            
            // Verificamos que se consultó la base de datos correctamente
            mockRepo.Verify(repo => repo.GetByEmailAsync(email), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var mockRepo = new Mock<IUsuarioRepository>();
            var controller = new UsersController(mockRepo.Object);
            var usuario = new Usuario { Id = 1, Nombre = "Actualizado", Correo = "test@test.com" };

            // Simulamos la operación de actualización (que devuelve Task)
            mockRepo.Setup(repo => repo.UpdateAsync(It.IsAny<Usuario>()));

            // Act
            var result = await controller.UpdateUser(1, usuario);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            
            // Verificamos que el repositorio recibió la orden de actualizar
            mockRepo.Verify(repo => repo.UpdateAsync(usuario), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_ReturnsOk_WhenUserExists()
        {
            // Arrange
            var mockRepo = new Mock<IUsuarioRepository>();
            var usuarioABorrar = new Usuario { Id = 1, Nombre = "ABorrar", Correo = "borrar@test.com" };

            // 1. Simulamos que la búsqueda inicial encuentra al usuario
            mockRepo.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(usuarioABorrar);
            
            // 2. Simulamos la operación de borrado
            mockRepo.Setup(repo => repo.DeleteAsync(It.IsAny<Usuario>()));

            var controller = new UsersController(mockRepo.Object);

            // Act
            var result = await controller.DeleteUser(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            // Verificamos que se mandó a borrar exactamente el objeto que se encontró previamente
            mockRepo.Verify(repo => repo.DeleteAsync(usuarioABorrar), Times.Once);
        }
    }
}