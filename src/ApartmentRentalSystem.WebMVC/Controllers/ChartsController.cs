using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApartmentRentalSystem.Infrastructure;

namespace ApartmentRentalSystem.WebMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly ApartmentContext _context;

        public ChartsController(ApartmentContext context)
        {
            _context = context;
        }

        public record CountByCityResponseItem(string City, int Count);
        public record CountByHousingTypeResponseItem(string HousingType, int Count);

        [HttpGet("countByCity")]
        public async Task<JsonResult> GetCountByCityAsync(CancellationToken cancellationToken)
        {
            var rawData = await _context.Apartments
                .Where(a => a.IsActive)
                .Select(a => a.City)
                .ToListAsync(cancellationToken);

            var responseItems = rawData
                .Where(city => !string.IsNullOrWhiteSpace(city))
                .GroupBy(city => city)
                .Select(group => new CountByCityResponseItem(group.Key, group.Count()))
                .OrderBy(x => x.City)
                .ToList();

            return new JsonResult(responseItems);
        }

        [HttpGet("countByHousingType")]
        public async Task<JsonResult> GetCountByHousingTypeAsync(CancellationToken cancellationToken)
        {
            var rawData = await _context.Apartments
                .Where(a => a.IsActive)
                .Include(a => a.HousingType)
                .Select(a => a.HousingType != null ? a.HousingType.Name : "Невідомо")
                .ToListAsync(cancellationToken);

            var responseItems = rawData
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .GroupBy(type => type)
                .Select(group => new CountByHousingTypeResponseItem(group.Key, group.Count()))
                .OrderBy(x => x.HousingType)
                .ToList();

            return new JsonResult(responseItems);
        }
    }
}