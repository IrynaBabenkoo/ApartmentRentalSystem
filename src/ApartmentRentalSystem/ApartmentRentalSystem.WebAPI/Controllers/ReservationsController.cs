using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly ApartmentContext _context;

    public ReservationsController(ApartmentContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var result = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Guest)
            .Include(r => r.Status)
            .Select(r => new {
                r.Id,
                r.StartAt,
                r.EndAt,
                r.TotalPrice,
                r.UnitsCount,
                Apartment = r.Apartment.Title,
                ApartmentCity = r.Apartment.City,
                Guest = r.Guest.FullName,
                Status = r.Status.Name
            }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("guest/{guestId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByGuest(int guestId)
    {
        var result = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Status)
            .Where(r => r.GuestId == guestId)
            .Select(r => new {
                r.Id,
                r.StartAt,
                r.EndAt,
                r.TotalPrice,
                r.UnitsCount,
                Apartment = r.Apartment.Title,
                ApartmentCity = r.Apartment.City,
                Status = r.Status.Name
            }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("host/{hostId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByHost(string hostId)
    {
        var result = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Guest)
            .Include(r => r.Status)
            .Where(r => r.Apartment.HostId == hostId)
            .Select(r => new {
                r.Id,
                r.StartAt,
                r.EndAt,
                r.TotalPrice,
                Apartment = r.Apartment.Title,
                Guest = r.Guest.FullName,
                GuestPhone = r.Guest.Phone,
                Status = r.Status.Name
            }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var r = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Guest)
            .Include(r => r.Status)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return NotFound();

        return Ok(new
        {
            r.Id,
            r.StartAt,
            r.EndAt,
            r.TotalPrice,
            r.UnitsCount,
            Apartment = r.Apartment.Title,
            ApartmentCity = r.Apartment.City,
            Guest = r.Guest.FullName,
            Status = r.Status.Name,
            IsPaid = r.Payment != null
        });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] ReservationCreateDto dto)
    {
        var apartment = await _context.Apartments
            .Include(a => a.Pricings)
            .FirstOrDefaultAsync(a => a.Id == dto.ApartmentId);

        if (apartment == null) return NotFound("Апартамент не знайдено");

        var status = await _context.ReservationStatuses
            .FirstOrDefaultAsync(s => s.Name == "Очікує підтвердження");

        var timeUnit = await _context.TimeUnits.FirstOrDefaultAsync();

        var pricing = apartment.Pricings
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefault();

        var totalPrice = pricing != null
            ? pricing.Amount * dto.UnitsCount
            : 0;

        var reservation = new Reservation
        {
            ApartmentId = dto.ApartmentId,
            GuestId = dto.GuestId,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            UnitsCount = dto.UnitsCount,
            StatusId = status?.Id ?? 1,
            UnitId = timeUnit?.Id ?? 1,
            TotalPrice = totalPrice,
            UnitAmountSnapshot = pricing?.Amount,
            CurrencySnapshot = pricing?.Currency
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = reservation.Id },
            new { reservation.Id, reservation.TotalPrice });
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        var cancelStatus = await _context.ReservationStatuses
            .FirstOrDefaultAsync(s => s.Name == "Скасовано");

        reservation.StatusId = cancelStatus?.Id ?? reservation.StatusId;
        await _context.SaveChangesAsync();

        return Ok(new { reservation.Id, Status = "Скасовано" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();
        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class ReservationCreateDto
{
    public int ApartmentId { get; set; }
    public int GuestId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int UnitsCount { get; set; }
}
