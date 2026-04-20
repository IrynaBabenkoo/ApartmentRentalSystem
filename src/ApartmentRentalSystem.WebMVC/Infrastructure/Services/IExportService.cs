using ApartmentRentalSystem.Domain.Entities;

namespace ApartmentRentalSystem.WebMVC.Infrastructure.Services
{
    public interface IExportService<TEntity>
        where TEntity : Entity
    {
        Task WriteToAsync(Stream stream, CancellationToken cancellationToken);
    }
}
