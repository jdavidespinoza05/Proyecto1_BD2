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
    public class ReservationsControllerTests
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
        public async Task CreateReservation_ReturnsCreated_WhenDateIsFuture()
        {
            // Arrange
            var db = GetDatabaseContext();
            var controller = new ReservationsController(db);
            // Ponemos una fecha para mañana
            var futureDate = DateTime.Now.AddDays(1);
            var res = new Reservation { Id = 1, UserId = 1, RestaurantId = 1, ReservationDate = futureDate };

            // Act
            var result = await controller.CreateReservation(res);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var model = Assert.IsType<Reservation>(createdResult.Value);
            Assert.Equal(futureDate, model.ReservationDate);
        }

        [Fact]
        public async Task CreateReservation_ReturnsBadRequest_WhenDateIsPast()
        {
            // Arrange
            var db = GetDatabaseContext();
            var controller = new ReservationsController(db);
            // Ponemos una fecha de ayer
            var pastDate = DateTime.Now.AddDays(-1);
            var res = new Reservation { Id = 2, ReservationDate = pastDate };

            // Act
            var result = await controller.CreateReservation(res);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("No puede reservar en el pasado, compa.", badRequest.Value);
        }

        [Fact]
        public async Task CancelReservation_ReturnsNoContent_WhenExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            db.Reservations.Add(new Reservation { Id = 5, Status = "Pendiente" });
            await db.SaveChangesAsync();
            var controller = new ReservationsController(db);

            // Act
            var result = await controller.CancelReservation(5);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(db.Reservations);
        }

        [Fact]
        public async Task CancelReservation_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            var db = GetDatabaseContext();
            var controller = new ReservationsController(db);

            // Act
            var result = await controller.CancelReservation(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}