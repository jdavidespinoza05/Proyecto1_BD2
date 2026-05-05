using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using RestaurantesApi.Controllers;
using RestaurantesApi.Models;
using RestaurantesApi.Repositories;
using System.Threading.Tasks;
using System;

namespace RestauranteApi.Tests
{
    public class ReservationsControllerTests
    {
        [Fact]
        public async Task CreateReservation_ReturnsCreated_WhenDateIsFuture()
        {
            // Arrange
            var mockRepo = new Mock<IReservationRepository>();
            var controller = new ReservationsController(mockRepo.Object);
            
            var futureDate = DateTime.Now.AddDays(1);
            var res = new Reservation { Id = 1, UserId = 1, RestaurantId = 1, ReservationDate = futureDate };

            mockRepo.Setup(repo => repo.CreateAsync(It.IsAny<Reservation>()));

            // Act
            var result = await controller.CreateReservation(res);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var model = Assert.IsType<Reservation>(createdResult.Value);
            Assert.Equal(futureDate, model.ReservationDate);
            
            // Verificamos que se llamó al repositorio para guardar
            mockRepo.Verify(repo => repo.CreateAsync(res), Times.Once);
        }

        [Fact]
        public async Task CreateReservation_ReturnsBadRequest_WhenDateIsPast()
        {
            // Arrange
            var mockRepo = new Mock<IReservationRepository>();
            var controller = new ReservationsController(mockRepo.Object);
            
            var pastDate = DateTime.Now.AddDays(-1);
            var res = new Reservation { Id = 2, ReservationDate = pastDate };

            // Act
            var result = await controller.CreateReservation(res);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            // Validamos contra el mensaje exacto, formal y actualizado
            Assert.Equal("La fecha de reservación no puede ser en el pasado.", badRequest.Value);
            
            // Aseguramos que nunca intentó guardar una reserva inválida
            mockRepo.Verify(repo => repo.CreateAsync(It.IsAny<Reservation>()), Times.Never);
        }

        [Fact]
        public async Task CancelReservation_ReturnsNoContent_WhenExists()
        {
            // Arrange
            var mockRepo = new Mock<IReservationRepository>();
            var reservaFalsa = new Reservation { Id = 5, Status = "Pendiente" };
            
            // Simulamos que encuentra la reserva
            mockRepo.Setup(repo => repo.GetByIdAsync(5)).ReturnsAsync(reservaFalsa);
            mockRepo.Setup(repo => repo.DeleteAsync(It.IsAny<Reservation>()));
            
            var controller = new ReservationsController(mockRepo.Object);

            // Act
            var result = await controller.CancelReservation(5);

            // Assert
            Assert.IsType<NoContentResult>(result);
            // Verificamos que mandó a borrar exactamente el objeto que encontró
            mockRepo.Verify(repo => repo.DeleteAsync(reservaFalsa), Times.Once);
        }

        [Fact]
        public async Task CancelReservation_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            var mockRepo = new Mock<IReservationRepository>();
            
            // Simulamos que no hay nada con el ID 99
            mockRepo.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Reservation)null);
            
            var controller = new ReservationsController(mockRepo.Object);

            // Act
            var result = await controller.CancelReservation(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            // Validamos que si no existe, nunca intenta borrar nada
            mockRepo.Verify(repo => repo.DeleteAsync(It.IsAny<Reservation>()), Times.Never);
        }
    }
}