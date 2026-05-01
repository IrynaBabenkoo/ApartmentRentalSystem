using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ApartmentRentalSystem.WebMVC.Infrastructure.Services
{
    public class ApartmentExportService : IExportService<Apartment>
    {
        private const string RootWorksheetName = "Apartments";

        private static readonly IReadOnlyList<string> HeaderNames = new[]
        {
            "Id",
            "Назва",
            "Місто",
            "Адреса",
            "Тип житла",
            "Макс. гостей",
            "Площа",
            "Опис",
            "Ціна",
            "Валюта",
            "Одиниця часу",
            "Активне",
            "Зручності"
        };

        private readonly ApartmentContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApartmentExportService(
            ApartmentContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (!stream.CanWrite)
                throw new ArgumentException("Потік недоступний для запису.");

            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new ArgumentException("Не вдалося визначити поточного користувача.");

            var apartments = await _context.Apartments
                .Include(a => a.HousingType)
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                        .ThenInclude(pt => pt.TimeUnit)
                .Include(a => a.ApartmentAmenities)
                    .ThenInclude(aa => aa.Amenity)
                .Where(a => a.HostId == currentUserId)
                .OrderBy(a => a.City)
                .ThenBy(a => a.Title)
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(RootWorksheetName);

            WriteHeader(worksheet);
            WriteApartments(worksheet, apartments);

            workbook.SaveAs(stream);
        }

        private static void WriteHeader(IXLWorksheet worksheet)
        {
            for (int i = 0; i < HeaderNames.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = HeaderNames[i];
            }

            worksheet.Row(1).Style.Font.Bold = true;
        }

        private static void WriteApartments(IXLWorksheet worksheet, ICollection<Apartment> apartments)
        {
            var rowIndex = 2;

            foreach (var apartment in apartments)
            {
                WriteApartment(worksheet, apartment, rowIndex);
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();
        }

        private static void WriteApartment(IXLWorksheet worksheet, Apartment apartment, int rowIndex)
        {
            var currentPricing = apartment.Pricings
                .Where(p => p.ValidTo == null)
                .OrderByDescending(p => p.ValidFrom)
                .FirstOrDefault()
                ?? apartment.Pricings.OrderByDescending(p => p.ValidFrom).FirstOrDefault();

            var amenities = string.Join(", ",
                apartment.ApartmentAmenities
                    .Select(x => x.Amenity?.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OrderBy(x => x));

            var column = 1;

            worksheet.Cell(rowIndex, column++).Value = apartment.Id;
            worksheet.Cell(rowIndex, column++).Value = apartment.Title;
            worksheet.Cell(rowIndex, column++).Value = apartment.City;
            worksheet.Cell(rowIndex, column++).Value = apartment.Address;
            worksheet.Cell(rowIndex, column++).Value = apartment.HousingType?.Name ?? "";
            worksheet.Cell(rowIndex, column++).Value = apartment.MaxGuests;
            worksheet.Cell(rowIndex, column++).Value = apartment.Area?.ToString() ?? "";
            worksheet.Cell(rowIndex, column++).Value = apartment.Description ?? "";
            worksheet.Cell(rowIndex, column++).Value = currentPricing?.Amount.ToString() ?? "";
            worksheet.Cell(rowIndex, column++).Value = currentPricing?.Currency ?? "";
            worksheet.Cell(rowIndex, column++).Value = currentPricing?.PriceType?.TimeUnit?.Name ?? "";
            worksheet.Cell(rowIndex, column++).Value = apartment.IsActive ? "Так" : "Ні";
            worksheet.Cell(rowIndex, column++).Value = amenities;
        }
    }
}