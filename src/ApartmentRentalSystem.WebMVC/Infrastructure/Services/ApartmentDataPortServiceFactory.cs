using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace ApartmentRentalSystem.WebMVC.Infrastructure.Services
{
    public class ApartmentDataPortServiceFactory : IDataPortServiceFactory<Apartment>
    {
        private readonly ApartmentContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApartmentDataPortServiceFactory(
            ApartmentContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public IImportService<Apartment> GetImportService(string contentType)
        {
            if (contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return new ApartmentImportService(_context, _httpContextAccessor);
            }

            throw new NotImplementedException($"Імпорт для типу {contentType} не реалізовано.");
        }

        public IExportService<Apartment> GetExportService(string contentType)
        {
            if (contentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return new ApartmentExportService(_context, _httpContextAccessor);
            }

            throw new NotImplementedException($"Експорт для типу {contentType} не реалізовано.");
        }
    }
}