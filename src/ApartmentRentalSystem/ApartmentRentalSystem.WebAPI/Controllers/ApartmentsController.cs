using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApartmentsController : ControllerBase
{
    private readonly ApartmentContext _context;
    private readonly IWebHostEnvironment _environment;

    public ApartmentsController(ApartmentContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(
        [FromQuery] string? city,
        [FromQuery] int? maxGuests,
        [FromQuery] bool? isActive)
    {
        var query = _context.Apartments
            .Include(a => a.HousingType)
            .Include(a => a.Pricings)
                .ThenInclude(p => p.PriceType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(a => a.City.ToLower().Contains(city.ToLower()));

        if (maxGuests.HasValue)
            query = query.Where(a => a.MaxGuests >= maxGuests.Value);

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        var result = await query
            .OrderByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.City,
                a.Address,
                a.MaxGuests,
                a.IsActive,
                a.Description,
                a.Area,
                a.ImagePath,
                HousingType = a.HousingType.Name,
                Price = a.Pricings
                    .OrderByDescending(p => p.ValidFrom)
                    .Select(p => new
                    {
                        p.Amount,
                        p.Currency,
                        TimeUnitId = p.PriceType.UnitId
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var a = await _context.Apartments
            .Include(a => a.HousingType)
            .Include(a => a.Pricings)
                .ThenInclude(p => p.PriceType)
            .Include(a => a.ApartmentAmenities)
                .ThenInclude(aa => aa.Amenity)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (a == null)
            return NotFound();

        return Ok(new
        {
            a.Id,
            a.Title,
            a.City,
            a.Address,
            a.MaxGuests,
            a.IsActive,
            a.Description,
            a.Area,
            a.ImagePath,
            HousingTypeId = a.HousingTypeId,
            HousingType = a.HousingType.Name,
            Pricings = a.Pricings
                .OrderByDescending(p => p.ValidFrom)
                .Select(p => new
                {
                    p.Amount,
                    p.Currency,
                    TimeUnitId = p.PriceType.UnitId
                }),
            AmenityIds = a.ApartmentAmenities.Select(aa => aa.AmenityId),
            Amenities = a.ApartmentAmenities.Select(aa => aa.Amenity.Name)
        });
    }

    [HttpGet("{id}/availability")]
    public async Task<ActionResult<object>> GetAvailability(int id)
    {
        var apartment = await _context.Apartments.FindAsync(id);
        if (apartment == null)
            return NotFound("Апартамент не знайдено.");

        var reservations = await _context.Reservations
            .Where(r => r.ApartmentId == id)
            .OrderBy(r => r.StartAt)
            .Select(r => new { r.StartAt, r.EndAt, r.StatusId })
            .ToListAsync();

        return Ok(new
        {
            apartmentId = id,
            apartmentTitle = apartment.Title,
            bookedPeriods = reservations
        });
    }

    [HttpGet("host/{hostId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByHost(int hostId)
    {
        var result = await _context.Apartments
            .Include(a => a.HousingType)
            .Include(a => a.Pricings)
                .ThenInclude(p => p.PriceType)
            .Where(a => a.HostId == hostId)
            .OrderByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.City,
                a.Address,
                a.MaxGuests,
                a.IsActive,
                a.Description,
                a.Area,
                a.ImagePath,
                HousingType = a.HousingType.Name,
                Price = a.Pricings
                    .OrderByDescending(p => p.ValidFrom)
                    .Select(p => new
                    {
                        p.Amount,
                        p.Currency,
                        TimeUnitId = p.PriceType.UnitId
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> Create([FromForm] ApartmentCreateDto dto)
    {
        var validationResult = ValidateApartmentDto(dto);
        if (validationResult != null)
            return validationResult;

        string? imagePath;

        try
        {
            imagePath = await SaveApartmentImageAsync(dto.ImageFile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        var apartment = new Apartment
        {
            Title = dto.Title.Trim(),
            City = dto.City.Trim(),
            Address = dto.Address.Trim(),
            MaxGuests = dto.MaxGuests,
            HousingTypeId = dto.HousingTypeId,
            HostId = dto.HostId,
            IsActive = true,
            Description = dto.Description?.Trim(),
            Area = dto.Area,
            ImagePath = imagePath
        };

        _context.Apartments.Add(apartment);
        await _context.SaveChangesAsync();

        await SavePricingAsync(apartment.Id, dto.PriceAmount, dto.Currency, dto.TimeUnitId);
        await SaveAmenitiesAsync(apartment.Id, dto.SelectedAmenityIds);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = apartment.Id }, new
        {
            apartment.Id,
            apartment.ImagePath
        });
    }

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] ApartmentCreateDto dto)
    {
        var apartment = await _context.Apartments
            .Include(a => a.Pricings)
            .Include(a => a.ApartmentAmenities)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apartment == null)
            return NotFound();

        var validationResult = ValidateApartmentDto(dto);
        if (validationResult != null)
            return validationResult;

        apartment.Title = dto.Title.Trim();
        apartment.City = dto.City.Trim();
        apartment.Address = dto.Address.Trim();
        apartment.MaxGuests = dto.MaxGuests;
        apartment.HousingTypeId = dto.HousingTypeId;
        apartment.Description = dto.Description?.Trim();
        apartment.Area = dto.Area;

        if (dto.ImageFile != null && dto.ImageFile.Length > 0)
        {
            try
            {
                var newImagePath = await SaveApartmentImageAsync(dto.ImageFile);
                DeleteApartmentImage(apartment.ImagePath);
                apartment.ImagePath = newImagePath;
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        await SavePricingAsync(apartment.Id, dto.PriceAmount, dto.Currency, dto.TimeUnitId);
        await SaveAmenitiesAsync(apartment.Id, dto.SelectedAmenityIds);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var apartment = await _context.Apartments
            .Include(a => a.Pricings)
            .Include(a => a.ApartmentAmenities)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apartment == null)
            return NotFound();

        DeleteApartmentImage(apartment.ImagePath);

        _context.ApartmentPricings.RemoveRange(apartment.Pricings);
        _context.ApartmentAmenities.RemoveRange(apartment.ApartmentAmenities);
        _context.Apartments.Remove(apartment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var apartment = await _context.Apartments.FindAsync(id);
        if (apartment == null)
            return NotFound();

        apartment.IsActive = !apartment.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { apartment.Id, apartment.IsActive });
    }

    private BadRequestObjectResult? ValidateApartmentDto(ApartmentCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) ||
            string.IsNullOrWhiteSpace(dto.City) ||
            string.IsNullOrWhiteSpace(dto.Address))
        {
            return BadRequest("Заповніть назву, місто та адресу апартаменту.");
        }

        if (dto.HousingTypeId <= 0)
            return BadRequest("Оберіть тип житла.");

        if (dto.MaxGuests <= 0)
            return BadRequest("Кількість гостей має бути більшою за 0.");

        if (dto.PriceAmount <= 0)
            return BadRequest("Вкажіть коректну ціну.");

        if (dto.TimeUnitId <= 0)
            return BadRequest("Оберіть період оплати.");

        return null;
    }

    private async Task SavePricingAsync(int apartmentId, decimal priceAmount, string? currency, int timeUnitId)
    {
        var selectedCurrency = string.IsNullOrWhiteSpace(currency) ? "UAH" : currency.Trim();

        var priceType = await _context.PriceTypes
            .FirstOrDefaultAsync(pt => pt.UnitId == timeUnitId);

        if (priceType == null)
        {
            priceType = new PriceType
            {
                Name = "Ціна за період",
                UnitId = timeUnitId
            };

            _context.PriceTypes.Add(priceType);
            await _context.SaveChangesAsync();
        }

        var currentPricing = await _context.ApartmentPricings
            .Include(p => p.PriceType)
            .Where(p => p.ApartmentId == apartmentId)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync();

        var needNewPricing = currentPricing == null ||
                             currentPricing.Amount != priceAmount ||
                             currentPricing.Currency != selectedCurrency ||
                             currentPricing.PriceType.UnitId != timeUnitId;

        if (needNewPricing)
        {
            _context.ApartmentPricings.Add(new ApartmentPricing
            {
                ApartmentId = apartmentId,
                Amount = priceAmount,
                Currency = selectedCurrency,
                PriceTypeId = priceType.Id,
                ValidFrom = DateTime.UtcNow
            });
        }
    }

    private async Task SaveAmenitiesAsync(int apartmentId, List<int>? selectedAmenityIds)
    {
        var oldAmenities = await _context.ApartmentAmenities
            .Where(aa => aa.ApartmentId == apartmentId)
            .ToListAsync();

        _context.ApartmentAmenities.RemoveRange(oldAmenities);

        if (selectedAmenityIds == null || selectedAmenityIds.Count == 0)
            return;

        foreach (var amenityId in selectedAmenityIds.Distinct())
        {
            _context.ApartmentAmenities.Add(new ApartmentAmenity
            {
                ApartmentId = apartmentId,
                AmenityId = amenityId
            });
        }
    }

    private async Task<string?> SaveApartmentImageAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return null;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Дозволені тільки зображення у форматі JPG, PNG або WEBP.");

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var folderPath = Path.Combine(webRootPath, "images", "apartments");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        return $"/images/apartments/{fileName}";
    }

    private void DeleteApartmentImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        var webRootPath = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
            webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(webRootPath, relativePath);

        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }
}

public class ApartmentCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public int HousingTypeId { get; set; }
    public int HostId { get; set; }
    public string? Description { get; set; }
    public decimal? Area { get; set; }
    public decimal PriceAmount { get; set; }
    public string? Currency { get; set; }
    public int TimeUnitId { get; set; }
    public IFormFile? ImageFile { get; set; }
    public List<int>? SelectedAmenityIds { get; set; }
}
