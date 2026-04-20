using ApartmentRentalSystem.Domain.Entities;

namespace ApartmentRentalSystem.WebMVC.Infrastructure.Services
{
    public interface IImportService<TEntity>
        where TEntity : Entity
    {
        Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken);
    }
}