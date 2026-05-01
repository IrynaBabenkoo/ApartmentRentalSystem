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

        public record AveragePriceByCityResponseItem(string City, decimal AveragePrice);

        public record OffersByCityResponseItem(double Lat, double Lon, string City, int Count);

        private static readonly Dictionary<string, (double Lat, double Lon)> CityCoords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Київ"] = (50.45, 30.52),
                ["Одеса"] = (46.48, 30.73),
                ["Львів"] = (49.84, 24.03),
                ["Харків"] = (49.99, 36.23),
                ["Дніпро"] = (48.46, 35.04),
                ["Вінниця"] = (49.23, 28.47),
                ["Ужгород"] = (48.62, 22.30),
                ["Яремче"] = (48.46, 24.56),
                ["Івано-Франківськ"] = (48.92, 24.71),
                ["Буковель"] = (48.36, 24.39),
            };

        [HttpGet("averagePriceByCity")]
        public async Task<JsonResult> GetAveragePriceByCityAsync(
            [FromQuery] string? housingType,
            CancellationToken cancellationToken)
        {
            var apartments = await _context.Apartments
                .Where(a => a.IsActive)
                .Include(a => a.HousingType)
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                        .ThenInclude(pt => pt.TimeUnit)
                .ToListAsync(cancellationToken);

            var filteredApartments = apartments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(housingType))
            {
                filteredApartments = filteredApartments.Where(a =>
                    a.HousingType != null &&
                    a.HousingType.Name.Equals(housingType, StringComparison.OrdinalIgnoreCase));
            }

            var responseItems = filteredApartments
                .Select(a => new
                {
                    City = a.City?.Trim(),
                    NightPrice = a.Pricings
                        .Where(p =>
                            p.PriceType != null &&
                            p.PriceType.TimeUnit != null &&
                            IsNightUnit(p.PriceType.TimeUnit.Name))
                        .OrderByDescending(p => p.ValidFrom)
                        .Select(p => (decimal?)p.Amount)
                        .FirstOrDefault()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.City) && x.NightPrice.HasValue)
                .GroupBy(x => x.City!)
                .Select(group => new AveragePriceByCityResponseItem(
                    group.Key,
                    Math.Round(group.Average(x => x.NightPrice!.Value), 0)
                ))
                .OrderBy(x => x.City)
                .ToList();

            return new JsonResult(responseItems);
        }

        [HttpGet("offersByCity")]
        public async Task<JsonResult> GetOffersByCityAsync(CancellationToken cancellationToken)
        {
            var rawItems = await _context.Apartments
                .Where(a => a.IsActive && !string.IsNullOrWhiteSpace(a.City))
                .GroupBy(a => a.City)
                .Select(group => new
                {
                    City = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);

            var responseItems = rawItems
                .Where(x => !string.IsNullOrWhiteSpace(x.City) && CityCoords.ContainsKey(x.City))
                .Select(x => new OffersByCityResponseItem(
                    CityCoords[x.City].Lat,
                    CityCoords[x.City].Lon,
                    x.City,
                    x.Count
                ))
                .OrderBy(x => x.City)
                .ToList();

            return new JsonResult(responseItems);
        }

        private static bool IsNightUnit(string unitName)
        {
            var normalized = unitName.Trim().ToLowerInvariant();

            return normalized == "доба"
                   || normalized == "ніч"
                   || normalized == "day"
                   || normalized == "night";
        }
    }
}