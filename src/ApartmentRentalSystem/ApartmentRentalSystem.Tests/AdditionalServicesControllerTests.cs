using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApartmentRentalSystem.Tests;

public class AdditionalServicesControllerTests
{
    private static ApartmentContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApartmentContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApartmentContext(options);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllAdditionalServices()
    {
        // Arrange
        await using var context = CreateContext();

        context.AdditionalServices.AddRange(
            new AdditionalService
            {
                Name = "Прибирання",
                Price = 600
            },
            new AdditionalService
            {
                Name = "Ранній заїзд",
                Price = 300
            }
        );

        await context.SaveChangesAsync();

        var controller = new AdditionalServicesController(context);

        // Act
        var result = await controller.GetAll();

        // Assert
        var services = Assert.IsAssignableFrom<IEnumerable<AdditionalService>>(result.Value);
        var servicesList = services.ToList();

        Assert.Equal(2, servicesList.Count);
        Assert.Contains(servicesList, s => s.Name == "Прибирання" && s.Price == 600);
        Assert.Contains(servicesList, s => s.Name == "Ранній заїзд" && s.Price == 300);
    }

    [Fact]
    public async Task GetById_ShouldReturnService_WhenServiceExists()
    {
        // Arrange
        await using var context = CreateContext();

        var service = new AdditionalService
        {
            Name = "Прибирання",
            Price = 600
        };

        context.AdditionalServices.Add(service);
        await context.SaveChangesAsync();

        var controller = new AdditionalServicesController(context);

        // Act
        var result = await controller.GetById(service.Id);

        // Assert
        var returnedService = Assert.IsType<AdditionalService>(result.Value);

        Assert.Equal(service.Id, returnedService.Id);
        Assert.Equal("Прибирання", returnedService.Name);
        Assert.Equal(600, returnedService.Price);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenServiceDoesNotExist()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new AdditionalServicesController(context);

        // Act
        var result = await controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ShouldAddNewAdditionalService()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new AdditionalServicesController(context);

        var service = new AdditionalService
        {
            Name = "Пізній виїзд",
            Price = 400
        };

        // Act
        var result = await controller.Create(service);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdService = Assert.IsType<AdditionalService>(createdResult.Value);

        Assert.Equal("Пізній виїзд", createdService.Name);
        Assert.Equal(400, createdService.Price);

        var serviceFromDb = await context.AdditionalServices.FirstOrDefaultAsync(s => s.Name == "Пізній виїзд");

        Assert.NotNull(serviceFromDb);
        Assert.Equal(400, serviceFromDb.Price);
    }

    [Fact]
    public async Task Update_ShouldChangeAdditionalService_WhenIdIsCorrect()
    {
        // Arrange
        await using var context = CreateContext();

        var service = new AdditionalService
        {
            Name = "Ранній заїзд",
            Price = 300
        };

        context.AdditionalServices.Add(service);
        await context.SaveChangesAsync();

        var controller = new AdditionalServicesController(context);

        service.Name = "Ранній заїзд VIP";
        service.Price = 500;

        // Act
        var result = await controller.Update(service.Id, service);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var updatedService = await context.AdditionalServices.FindAsync(service.Id);

        Assert.NotNull(updatedService);
        Assert.Equal("Ранній заїзд VIP", updatedService.Name);
        Assert.Equal(500, updatedService.Price);
    }

    [Fact]
    public async Task Update_ShouldReturnBadRequest_WhenIdIsIncorrect()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new AdditionalServicesController(context);

        var service = new AdditionalService
        {
            Id = 1,
            Name = "Прибирання",
            Price = 600
        };

        // Act
        var result = await controller.Update(2, service);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldRemoveAdditionalService_WhenServiceExists()
    {
        // Arrange
        await using var context = CreateContext();

        var service = new AdditionalService
        {
            Name = "Прибирання",
            Price = 600
        };

        context.AdditionalServices.Add(service);
        await context.SaveChangesAsync();

        var controller = new AdditionalServicesController(context);

        // Act
        var result = await controller.Delete(service.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        var deletedService = await context.AdditionalServices.FindAsync(service.Id);

        Assert.Null(deletedService);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenServiceDoesNotExist()
    {
        // Arrange
        await using var context = CreateContext();

        var controller = new AdditionalServicesController(context);

        // Act
        var result = await controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}