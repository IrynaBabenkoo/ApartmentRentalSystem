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

    private static DateTime ToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var reservations = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Guest)
            .Include(r => r.Status)
            .OrderByDescending(r => r.StartAt)
            .ToListAsync();

        var result = reservations.Select(r => new
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
            Services = _context.ReservationServices
                .Include(rs => rs.Service)
                .Where(rs => rs.ReservationId == r.Id)
                .Select(rs => new
                {
                    rs.ServiceId,
                    Name = rs.Service.Name,
                    Price = rs.Service.Price
                })
                .ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("guest/{guestId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByGuest(int guestId)
    {
        var reservations = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Status)
            .Where(r => r.GuestId == guestId)
            .OrderByDescending(r => r.StartAt)
            .ToListAsync();

        var result = reservations.Select(r => new
        {
            r.Id,
            r.StartAt,
            r.EndAt,
            r.TotalPrice,
            r.UnitsCount,
            Apartment = r.Apartment.Title,
            ApartmentCity = r.Apartment.City,
            Status = r.Status.Name,
            Services = _context.ReservationServices
                .Include(rs => rs.Service)
                .Where(rs => rs.ReservationId == r.Id)
                .Select(rs => new
                {
                    rs.ServiceId,
                    Name = rs.Service.Name,
                    Price = rs.Service.Price
                })
                .ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("host/{hostId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByHost(int hostId)
    {
        var reservations = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Guest)
            .Include(r => r.Status)
            .Where(r => r.Apartment.HostId == hostId)
            .OrderByDescending(r => r.StartAt)
            .ToListAsync();

        var result = reservations.Select(r => new
        {
            r.Id,
            r.StartAt,
            r.EndAt,
            r.TotalPrice,
            r.UnitsCount,
            Apartment = r.Apartment.Title,
            Guest = r.Guest.FullName,
            GuestPhone = r.Guest.Phone,
            Status = r.Status.Name,
            Services = _context.ReservationServices
                .Include(rs => rs.Service)
                .Where(rs => rs.ReservationId == r.Id)
                .Select(rs => new
                {
                    rs.ServiceId,
                    Name = rs.Service.Name,
                    Price = rs.Service.Price
                })
                .ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Guest)
            .Include(r => r.Status)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        var services = await _context.ReservationServices
            .Include(rs => rs.Service)
            .Where(rs => rs.ReservationId == reservation.Id)
            .Select(rs => new
            {
                rs.ServiceId,
                Name = rs.Service.Name,
                Price = rs.Service.Price
            })
            .ToListAsync();

        return Ok(new
        {
            reservation.Id,
            reservation.StartAt,
            reservation.EndAt,
            reservation.TotalPrice,
            reservation.UnitsCount,
            Apartment = reservation.Apartment.Title,
            ApartmentCity = reservation.Apartment.City,
            Guest = reservation.Guest.FullName,
            Status = reservation.Status.Name,
            IsPaid = reservation.Payment != null,
            Services = services
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] ReservationCreateDto dto)
    {
        if (dto.ApartmentId <= 0)
        {
            return BadRequest("Апартамент не вибрано.");
        }

        if (dto.GuestId <= 0)
        {
            return BadRequest("Орендар не визначений.");
        }

        if (dto.UnitsCount <= 0)
        {
            return BadRequest("Кількість днів / одиниць має бути більшою за 0.");
        }

        if (dto.EndAt <= dto.StartAt)
        {
            return BadRequest("Дата завершення має бути пізнішою за дату початку.");
        }

        var apartment = await _context.Apartments
            .Include(a => a.Pricings)
            .FirstOrDefaultAsync(a => a.Id == dto.ApartmentId);

        if (apartment == null)
        {
            return NotFound("Апартамент не знайдено.");
        }

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
            StartAt = ToUtc(dto.StartAt),
            EndAt = ToUtc(dto.EndAt),
            UnitsCount = dto.UnitsCount,
            StatusId = status?.Id ?? 1,
            UnitId = timeUnit?.Id ?? 1,
            TotalPrice = totalPrice,
            UnitAmountSnapshot = pricing?.Amount,
            CurrencySnapshot = pricing?.Currency
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = reservation.Id },
            new
            {
                reservation.Id,
                reservation.TotalPrice
            });
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);

        if (reservation == null)
        {
            return NotFound();
        }

        var cancelStatus = await _context.ReservationStatuses
            .FirstOrDefaultAsync(s => s.Name == "Скасовано");

        reservation.StatusId = cancelStatus?.Id ?? reservation.StatusId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            reservation.Id,
            Status = "Скасовано"
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        var reservationServices = await _context.ReservationServices
            .Where(rs => rs.ReservationId == id)
            .ToListAsync();

        if (reservationServices.Any())
        {
            _context.ReservationServices.RemoveRange(reservationServices);
        }

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