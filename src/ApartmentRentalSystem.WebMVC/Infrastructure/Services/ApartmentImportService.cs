using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Infrastructure;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ApartmentRentalSystem.WebMVC.Infrastructure.Services
{
    public class ApartmentImportService : IImportService<Apartment>
    {
        private const int HeaderRow = 1;

        private readonly ApartmentContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApartmentImportService(
            ApartmentContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Потік недоступний для читання.", nameof(stream));

            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new ApartmentImportException("Не вдалося визначити поточного користувача для імпорту.");

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
                throw new ApartmentImportException("Excel-файл не містить аркушів.");

            foreach (var row in worksheet.RowsUsed().Skip(HeaderRow))
            {
                if (RowIsEmpty(row))
                    continue;

                await AddOrUpdateApartmentAsync(row, currentUserId, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task AddOrUpdateApartmentAsync(IXLRow row, string ownerId, CancellationToken cancellationToken)
        {
            var apartmentId = GetApartmentId(row);

            Apartment? apartment = null;

            if (apartmentId.HasValue)
            {
                apartment = await _context.Apartments
                    .Include(a => a.ApartmentAmenities)
                    .Include(a => a.Pricings)
                    .FirstOrDefaultAsync(a => a.Id == apartmentId.Value, cancellationToken);

                if (apartment is null)
                    throw new ApartmentImportException($"Оголошення з Id={apartmentId.Value} не знайдено.");

                if (apartment.HostId != ownerId)
                    throw new ApartmentImportException($"Оголошення з Id={apartmentId.Value} не належить поточному власнику.");
            }

            var housingType = await GetHousingTypeAsync(GetHousingTypeName(row), cancellationToken);
            var timeUnit = await GetTimeUnitAsync(GetTimeUnitName(row), cancellationToken);
            var priceType = await GetPriceTypeAsync(timeUnit.Id, cancellationToken);

            if (apartment is null)
            {
                apartment = new Apartment
                {
                    Title = GetTitle(row),
                    City = GetCity(row),
                    Address = GetAddress(row),
                    HousingTypeId = housingType.Id,
                    MaxGuests = GetMaxGuests(row),
                    Area = GetArea(row),
                    Description = GetDescription(row),
                    IsActive = GetIsActive(row),
                    HostId = ownerId,
                    ImagePath = null
                };

                _context.Apartments.Add(apartment);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                apartment.Title = GetTitle(row);
                apartment.City = GetCity(row);
                apartment.Address = GetAddress(row);
                apartment.HousingTypeId = housingType.Id;
                apartment.MaxGuests = GetMaxGuests(row);
                apartment.Area = GetArea(row);
                apartment.Description = GetDescription(row);
                apartment.IsActive = GetIsActive(row);
            }

            await UpdatePricingAsync(apartment, priceType.Id, GetPrice(row), GetCurrency(row), cancellationToken);
            await SyncAmenitiesAsync(apartment, GetAmenities(row), cancellationToken);
        }

        private async Task UpdatePricingAsync(
            Apartment apartment,
            int priceTypeId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken)
        {
            var currentPricing = apartment.Pricings
                .OrderByDescending(p => p.ValidFrom)
                .FirstOrDefault(p => p.ValidTo == null);

            if (currentPricing is null)
            {
                currentPricing = new ApartmentPricing
                {
                    ApartmentId = apartment.Id,
                    PriceTypeId = priceTypeId,
                    Amount = amount,
                    Currency = currency,
                    ValidFrom = DateTime.UtcNow
                };

                _context.ApartmentPricings.Add(currentPricing);
                return;
            }

            currentPricing.PriceTypeId = priceTypeId;
            currentPricing.Amount = amount;
            currentPricing.Currency = currency;
        }

        private async Task SyncAmenitiesAsync(
            Apartment apartment,
            IReadOnlyCollection<string> amenityNames,
            CancellationToken cancellationToken)
        {
            var normalizedAmenityNames = amenityNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var targetAmenityIds = new HashSet<int>();

            foreach (var amenityName in normalizedAmenityNames)
            {
                var amenity = await _context.Amenities
                    .FirstOrDefaultAsync(a => a.Name == amenityName, cancellationToken);

                if (amenity is null)
                {
                    amenity = new Amenity
                    {
                        Name = amenityName
                    };

                    _context.Amenities.Add(amenity);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                targetAmenityIds.Add(amenity.Id);
            }

            var currentLinks = apartment.ApartmentAmenities.ToList();

            foreach (var link in currentLinks)
            {
                if (!targetAmenityIds.Contains(link.AmenityId))
                {
                    _context.ApartmentAmenities.Remove(link);
                }
            }

            var existingAmenityIds = apartment.ApartmentAmenities
                .Select(x => x.AmenityId)
                .ToHashSet();

            foreach (var amenityId in targetAmenityIds)
            {
                if (!existingAmenityIds.Contains(amenityId))
                {
                    _context.ApartmentAmenities.Add(new ApartmentAmenity
                    {
                        ApartmentId = apartment.Id,
                        AmenityId = amenityId
                    });
                }
            }
        }

        private async Task<HousingType> GetHousingTypeAsync(string name, CancellationToken cancellationToken)
        {
            var housingType = await _context.HousingTypes
                .FirstOrDefaultAsync(h => h.Name == name, cancellationToken);

            if (housingType is null)
            {
                housingType = new HousingType
                {
                    Name = name
                };

                _context.HousingTypes.Add(housingType);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return housingType;
        }

        private async Task<TimeUnit> GetTimeUnitAsync(string name, CancellationToken cancellationToken)
        {
            var timeUnit = await _context.TimeUnits
                .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

            if (timeUnit is null)
                throw new ApartmentImportException($"Одиницю часу '{name}' не знайдено у базі.");

            return timeUnit;
        }

        private async Task<PriceType> GetPriceTypeAsync(int unitId, CancellationToken cancellationToken)
        {
            var priceType = await _context.PriceTypes
                .FirstOrDefaultAsync(p => p.UnitId == unitId, cancellationToken);

            if (priceType is null)
            {
                priceType = await _context.PriceTypes.FirstOrDefaultAsync(cancellationToken);
            }

            if (priceType is null)
                throw new ApartmentImportException("У таблиці PriceTypes немає жодного запису.");

            return priceType;
        }

        private static bool RowIsEmpty(IXLRow row)
        {
            return row.Cells(1, 13).All(c => string.IsNullOrWhiteSpace(c.GetValue<string>()));
        }

        private static int? GetApartmentId(IXLRow row)
        {
            var raw = row.Cell(1).GetValue<string>().Trim();

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (int.TryParse(raw, out var id))
                return id;

            throw new ApartmentImportException($"Некоректне значення Id: '{raw}'.");
        }

        private static string GetTitle(IXLRow row) => row.Cell(2).GetValue<string>().Trim();
        private static string GetCity(IXLRow row) => row.Cell(3).GetValue<string>().Trim();
        private static string GetAddress(IXLRow row) => row.Cell(4).GetValue<string>().Trim();
        private static string GetHousingTypeName(IXLRow row) => row.Cell(5).GetValue<string>().Trim();
        private static int GetMaxGuests(IXLRow row) => row.Cell(6).GetValue<int>();

        private static decimal? GetArea(IXLRow row)
        {
            var raw = row.Cell(7).GetValue<string>().Trim();

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;

            if (decimal.TryParse(raw, NumberStyles.Any, new CultureInfo("uk-UA"), out value))
                return value;

            return null;
        }

        private static string GetDescription(IXLRow row) => row.Cell(8).GetValue<string>().Trim();

        private static decimal GetPrice(IXLRow row)
        {
            var raw = row.Cell(9).GetValue<string>().Trim();

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;

            if (decimal.TryParse(raw, NumberStyles.Any, new CultureInfo("uk-UA"), out value))
                return value;

            throw new ApartmentImportException($"Некоректна ціна: '{raw}'.");
        }

        private static string GetCurrency(IXLRow row)
        {
            var value = row.Cell(10).GetValue<string>().Trim();
            return string.IsNullOrWhiteSpace(value) ? "UAH" : value;
        }

        private static string GetTimeUnitName(IXLRow row)
        {
            var value = row.Cell(11).GetValue<string>().Trim();

            if (string.IsNullOrWhiteSpace(value))
                throw new ApartmentImportException("Не вказано одиницю часу.");

            return value;
        }

        private static bool GetIsActive(IXLRow row)
        {
            var value = row.Cell(12).GetValue<string>().Trim().ToLower();
            return value is "так" or "true" or "1" or "yes";
        }

        private static IReadOnlyCollection<string> GetAmenities(IXLRow row)
        {
            var raw = row.Cell(13).GetValue<string>().Trim();

            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}