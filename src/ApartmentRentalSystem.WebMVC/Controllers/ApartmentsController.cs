using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.WebMVC.Models;
using ApartmentRentalSystem.WebMVC.Infrastructure.Services;

namespace ApartmentRentalSystem.WebMVC.Controllers
{
    public class ApartmentsController : Controller
    {
        private readonly ApartmentContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDataPortServiceFactory<Apartment> _apartmentDataPortServiceFactory;

        public ApartmentsController(
    ApartmentContext context,
    IWebHostEnvironment webHostEnvironment,
    IDataPortServiceFactory<Apartment> apartmentDataPortServiceFactory)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _apartmentDataPortServiceFactory = apartmentDataPortServiceFactory;
        }

        // GET: Apartments
        public async Task<IActionResult> Index(string? searchCity, decimal? maxPrice)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isOwner = User.Identity?.IsAuthenticated == true && User.IsInRole("Owner");

            var query = _context.Apartments
                .Include(a => a.HousingType)
                .Include(a => a.Host)
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                        .ThenInclude(pt => pt.TimeUnit)
                .AsQueryable();

            if (isOwner)
            {
                query = query.Where(a => a.HostId == userId);

                ViewBag.PageTitle = "Мої об'єкти";
                ViewBag.PageSubtitle = "Керуйте своїми оголошеннями, редагуйте інформацію та додавайте нові об'єкти.";
            }
            else
            {
                query = query.Where(a => a.IsActive);

                ViewBag.PageTitle = "Каталог житла";
                ViewBag.PageSubtitle = "Переглядайте доступні варіанти та знаходьте житло для своєї поїздки.";
            }

            ViewBag.IsOwner = isOwner;
            ViewBag.SearchCity = searchCity;
            ViewBag.MaxPrice = maxPrice;

            if (!string.IsNullOrWhiteSpace(searchCity))
            {
                query = query.Where(a => a.City.ToLower().Contains(searchCity.ToLower()));
            }

            if (maxPrice.HasValue && maxPrice > 0)
            {
                query = query.Where(a => a.Pricings.Any(p => p.Amount <= maxPrice.Value));
            }

            var apartments = await query
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return View(apartments);
        }

        // GET: Apartments/Create
        public IActionResult Create()
        {
            ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name");
            ViewData["TimeUnitId"] = new SelectList(_context.TimeUnits, "Id", "Name");
            ViewBag.Amenities = _context.Amenities.OrderBy(a => a.Name).ToList();

            return View(new ApartmentCreateViewModel());
        }

        // POST: Apartments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApartmentCreateViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? imagePath = null;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "images", "apartments");

                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                imagePath = "/images/apartments/" + fileName;
            }

            if (ModelState.IsValid && !string.IsNullOrEmpty(userId))
            {
                var apartment = new Apartment
                {
                    Title = model.Title,
                    City = model.City,
                    Address = model.Address,
                    MaxGuests = model.MaxGuests,
                    HousingTypeId = model.HousingTypeId,
                    HostId = userId,
                    IsActive = true,
                    ImagePath = imagePath,
                    Description = model.Description,
                    Area = model.Area
                };

                _context.Apartments.Add(apartment);
                await _context.SaveChangesAsync();

                var priceType = await _context.PriceTypes
                    .FirstOrDefaultAsync(pt => pt.UnitId == model.TimeUnitId);

                if (priceType == null)
                {
                    priceType = new PriceType
                    {
                        Name = "Ціна за період",
                        UnitId = model.TimeUnitId
                    };

                    _context.PriceTypes.Add(priceType);
                    await _context.SaveChangesAsync();
                }

                var pricing = new ApartmentPricing
                {
                    ApartmentId = apartment.Id,
                    Amount = model.PriceAmount,
                    Currency = model.Currency,
                    ValidFrom = DateTime.UtcNow,
                    PriceTypeId = priceType.Id
                };

                _context.ApartmentPricings.Add(pricing);

                if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Any())
                {
                    foreach (var amenityId in model.SelectedAmenityIds.Distinct())
                    {
                        _context.ApartmentAmenities.Add(new ApartmentAmenity
                        {
                            ApartmentId = apartment.Id,
                            AmenityId = amenityId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name", model.HousingTypeId);
            ViewData["TimeUnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", model.TimeUnitId);
            ViewBag.Amenities = _context.Amenities.OrderBy(a => a.Name).ToList();

            return View(model);
        }

        // GET: Apartments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var apartment = await _context.Apartments
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                .Include(a => a.ApartmentAmenities)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return NotFound();

            var currentPricing = apartment.Pricings
                .OrderByDescending(p => p.ValidFrom)
                .FirstOrDefault();

            var viewModel = new ApartmentCreateViewModel
            {
                Title = apartment.Title,
                City = apartment.City,
                Address = apartment.Address,
                MaxGuests = apartment.MaxGuests,
                HousingTypeId = apartment.HousingTypeId,
                PriceAmount = currentPricing?.Amount ?? 0,
                Currency = currentPricing?.Currency ?? "UAH",
                TimeUnitId = currentPricing?.PriceType?.UnitId ?? 0,
                Description = apartment.Description,
                Area = apartment.Area,
                SelectedAmenityIds = apartment.ApartmentAmenities.Select(aa => aa.AmenityId).ToList()
            };

            ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name", viewModel.HousingTypeId);
            ViewData["TimeUnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", viewModel.TimeUnitId);
            ViewBag.Amenities = _context.Amenities.OrderBy(a => a.Name).ToList();

            return View(viewModel);
        }

        // POST: Apartments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ApartmentCreateViewModel model)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                .Include(a => a.ApartmentAmenities)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return NotFound();

            if (model.HousingTypeId <= 0)
                ModelState.AddModelError(nameof(model.HousingTypeId), "Оберіть тип житла.");

            if (model.TimeUnitId <= 0)
                ModelState.AddModelError(nameof(model.TimeUnitId), "Оберіть одиницю часу.");

            if (!ModelState.IsValid)
            {
                ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name", model.HousingTypeId);
                ViewData["TimeUnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", model.TimeUnitId);
                ViewBag.Amenities = _context.Amenities.OrderBy(a => a.Name).ToList();

                return View(model);
            }

            try
            {
                apartment.Title = model.Title;
                apartment.City = model.City;
                apartment.Address = model.Address;
                apartment.MaxGuests = model.MaxGuests;
                apartment.HousingTypeId = model.HousingTypeId;
                apartment.Description = model.Description;
                apartment.Area = model.Area;

                var priceType = await _context.PriceTypes
                    .FirstOrDefaultAsync(pt => pt.UnitId == model.TimeUnitId);

                if (priceType == null)
                {
                    priceType = new PriceType
                    {
                        Name = "Ціна за період",
                        UnitId = model.TimeUnitId
                    };

                    _context.PriceTypes.Add(priceType);
                    await _context.SaveChangesAsync();
                }

                var currentPricing = apartment.Pricings
                    .OrderByDescending(p => p.ValidFrom)
                    .FirstOrDefault();

                var pricingChanged =
                    currentPricing == null ||
                    currentPricing.Amount != model.PriceAmount ||
                    currentPricing.Currency != model.Currency ||
                    currentPricing.PriceTypeId != priceType.Id;

                if (pricingChanged)
                {
                    _context.ApartmentPricings.Add(new ApartmentPricing
                    {
                        ApartmentId = apartment.Id,
                        Amount = model.PriceAmount,
                        Currency = model.Currency,
                        ValidFrom = DateTime.UtcNow,
                        PriceTypeId = priceType.Id
                    });
                }

                _context.ApartmentAmenities.RemoveRange(apartment.ApartmentAmenities);

                if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Any())
                {
                    foreach (var amenityId in model.SelectedAmenityIds.Distinct())
                    {
                        _context.ApartmentAmenities.Add(new ApartmentAmenity
                        {
                            ApartmentId = apartment.Id,
                            AmenityId = amenityId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Не вдалося зберегти зміни. Перевірте введені дані та спробуйте ще раз.");
            }

            ViewData["HousingTypeId"] = new SelectList(_context.HousingTypes, "Id", "Name", model.HousingTypeId);
            ViewData["TimeUnitId"] = new SelectList(_context.TimeUnits, "Id", "Name", model.TimeUnitId);
            ViewBag.Amenities = _context.Amenities.OrderBy(a => a.Name).ToList();

            return View(model);
        }

        // GET: Apartments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var apartment = await _context.Apartments
                .Include(a => a.HousingType)
                .Include(a => a.Pricings)
                    .ThenInclude(p => p.PriceType)
                        .ThenInclude(pt => pt.TimeUnit)
                .Include(a => a.ApartmentAmenities)
                    .ThenInclude(aa => aa.Amenity)
                .Include(a => a.Reservations)
                    .ThenInclude(r => r.Status)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return NotFound();

            var today = DateTime.Today;

            var currentConfirmedReservation = apartment.Reservations?
                .Where(r => r.Status != null
                            && r.Status.Name == "Підтверджено"
                            && r.StartAt.Date <= today
                            && r.EndAt.Date >= today)
                .OrderBy(r => r.EndAt)
                .FirstOrDefault();

            var nextConfirmedReservation = apartment.Reservations?
                .Where(r => r.Status != null
                            && r.Status.Name == "Підтверджено"
                            && r.StartAt.Date > today)
                .OrderBy(r => r.StartAt)
                .FirstOrDefault();

            ViewBag.IsBookedNow = currentConfirmedReservation != null;
            ViewBag.BookedUntil = currentConfirmedReservation?.EndAt;
            ViewBag.NextAvailableFrom = currentConfirmedReservation != null
                ? currentConfirmedReservation.EndAt.AddDays(1)
                : (DateTime?)null;

            ViewBag.NextReservationStart = nextConfirmedReservation?.StartAt;

            return View(apartment);
        }

        // POST: Apartments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Pricings)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment != null)
            {
                _context.RemoveRange(apartment.Pricings);
                _context.Apartments.Remove(apartment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ApartmentExists(int id)
        {
            return _context.Apartments.Any(e => e.Id == id);
        }

        [HttpGet]
        public IActionResult Import()
        {
            if (!User.IsInRole("Owner"))
                return Forbid();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile apartmentsFile, CancellationToken cancellationToken)
        {
            if (!User.IsInRole("Owner"))
                return Forbid();

            if (apartmentsFile == null || apartmentsFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Оберіть Excel-файл для імпорту.");
                return View();
            }

            var importService = _apartmentDataPortServiceFactory.GetImportService(apartmentsFile.ContentType);

            using var stream = apartmentsFile.OpenReadStream();
            await importService.ImportFromStreamAsync(stream, cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Export(
            [FromQuery] string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CancellationToken cancellationToken = default)
        {
            if (!User.IsInRole("Owner"))
                return Forbid();

            var exportService = _apartmentDataPortServiceFactory.GetExportService(contentType);

            var memoryStream = new MemoryStream();
            await exportService.WriteToAsync(memoryStream, cancellationToken);
            await memoryStream.FlushAsync(cancellationToken);
            memoryStream.Position = 0;

            return new FileStreamResult(memoryStream, contentType)
            {
                FileDownloadName = $"apartments_{DateTime.UtcNow:yyyy-MM-dd}.xlsx"
            };
        }
    }
}