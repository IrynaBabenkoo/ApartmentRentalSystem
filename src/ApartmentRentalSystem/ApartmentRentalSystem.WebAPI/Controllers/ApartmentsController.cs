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

    public ApartmentsController(ApartmentContext context)
    {
        _context = context;
    }

    // Всі апартаменти з фільтрацією
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(
        [FromQuery] string? city,
        [FromQuery] int? maxGuests,
        [FromQuery] bool? isActive)
    {
        var query = _context.Apartments
            .Include(a => a.HousingType)
            .Include(a => a.Pricings)
            .AsQueryable();

        if (!string.IsNullOrEmpty(city))
            query = query.Where(a => a.City.ToLower().Contains(city.ToLower()));

        if (maxGuests.HasValue)
            query = query.Where(a => a.MaxGuests >= maxGuests.Value);

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        var result = await query.Select(a => new {
            a.Id,
            a.Title,
            a.City,
            a.Address,
            a.MaxGuests,
            a.IsActive,
            a.Description,
            a.Area,
            HousingType = a.HousingType.Name,
            Price = a.Pricings
                .OrderByDescending(p => p.ValidFrom)
                .Select(p => new { p.Amount, p.Currency })
                .FirstOrDefault()
        }).ToListAsync();

        return Ok(result);
    }

    // Один апартамент
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var a = await _context.Apartments
            .Include(a => a.HousingType)
            .Include(a => a.Pricings)
            .Include(a => a.ApartmentAmenities)
                .ThenInclude(aa => aa.Amenity)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (a == null) return NotFound();

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
            HousingType = a.HousingType.Name,
            Pricings = a.Pricings.Select(p => new { p.Amount, p.Currency }),
            Amenities = a.ApartmentAmenities.Select(aa => aa.Amenity.Name)
        });
    }

    // Доступні дати
    [HttpGet("{id}/availability")]
    public async Task<ActionResult<object>> GetAvailability(int id)
    {
        var reservations = await _context.Reservations
            .Where(r => r.ApartmentId == id)
            .Select(r => new { r.StartAt, r.EndAt, r.StatusId })
            .ToListAsync();

        return Ok(new { apartmentId = id, bookedPeriods = reservations });
    }

    // Апартаменти власника
    [HttpGet("host/{hostId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByHost(string hostId)
    {
        var result = await _context.Apartments
            .Include(a => a.HousingType)
            .Include(a => a.Pricings)
            .Where(a => a.HostId == hostId)
            .Select(a => new {
                a.Id,
                a.Title,
                a.City,
                a.IsActive,
                HousingType = a.HousingType.Name,
                Price = a.Pricings
                    .OrderByDescending(p => p.ValidFrom)
                    .Select(p => new { p.Amount, p.Currency })
                    .FirstOrDefault()
            }).ToListAsync();

        return Ok(result);
    }

    // Додати апартамент
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] ApartmentCreateDto dto)
    {
        var apartment = new Apartment
        {
            Title = dto.Title,
            City = dto.City,
            Address = dto.Address,
            MaxGuests = dto.MaxGuests,
            HousingTypeId = dto.HousingTypeId,
            HostId = dto.HostId,
            IsActive = true,
            Description = dto.Description,
            Area = dto.Area
        };

        _context.Apartments.Add(apartment);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = apartment.Id }, new { apartment.Id });
    }

    // Редагувати апартамент
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ApartmentCreateDto dto)
    {
        var apartment = await _context.Apartments.FindAsync(id);
        if (apartment == null) return NotFound();

        apartment.Title = dto.Title;
        apartment.City = dto.City;
        apartment.Address = dto.Address;
        apartment.MaxGuests = dto.MaxGuests;
        apartment.HousingTypeId = dto.HousingTypeId;
        apartment.Description = dto.Description;
        apartment.Area = dto.Area;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Видалити апартамент
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var apartment = await _context.Apartments
            .Include(a => a.Pricings)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apartment == null) return NotFound();

        _context.ApartmentPricings.RemoveRange(apartment.Pricings);
        _context.Apartments.Remove(apartment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Змінити активність (керування доступністю)
    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var apartment = await _context.Apartments.FindAsync(id);
        if (apartment == null) return NotFound();

        apartment.IsActive = !apartment.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { apartment.Id, apartment.IsActive });
    }
}

public class ApartmentCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public int HousingTypeId { get; set; }
    public string HostId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Area { get; set; }
}