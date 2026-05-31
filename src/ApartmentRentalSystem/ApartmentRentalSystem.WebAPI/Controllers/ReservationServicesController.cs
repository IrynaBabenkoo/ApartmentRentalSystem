using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationServicesController : ControllerBase
{
    private readonly ApartmentContext _context;

    public ReservationServicesController(ApartmentContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var result = await _context.ReservationServices
            .Include(rs => rs.Service)
            .Select(rs => new
            {
                rs.Id,
                rs.ReservationId,
                rs.ServiceId,
                ServiceName = rs.Service.Name,
                ServicePrice = rs.Service.Price
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("reservation/{reservationId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByReservation(int reservationId)
    {
        var result = await _context.ReservationServices
            .Include(rs => rs.Service)
            .Where(rs => rs.ReservationId == reservationId)
            .Select(rs => new
            {
                rs.Id,
                rs.ReservationId,
                rs.ServiceId,
                ServiceName = rs.Service.Name,
                ServicePrice = rs.Service.Price
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] ReservationServiceCreateDto dto)
    {
        if (dto.ReservationId <= 0)
        {
            return BadRequest("Бронювання не вибрано.");
        }

        if (dto.ServiceId <= 0)
        {
            return BadRequest("Додаткову послугу не вибрано.");
        }

        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == dto.ReservationId);

        if (reservation == null)
        {
            return NotFound("Бронювання не знайдено.");
        }

        var service = await _context.AdditionalServices
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId);

        if (service == null)
        {
            return NotFound("Додаткову послугу не знайдено.");
        }

        var alreadyExists = await _context.ReservationServices
            .AnyAsync(rs =>
                rs.ReservationId == dto.ReservationId &&
                rs.ServiceId == dto.ServiceId);

        if (!alreadyExists)
        {
            var reservationService = new ReservationService
            {
                ReservationId = dto.ReservationId,
                ServiceId = dto.ServiceId
            };

            _context.ReservationServices.Add(reservationService);
            await _context.SaveChangesAsync();
        }

        var totalPrice = await RecalculateReservationTotalAsync(dto.ReservationId);

        return Ok(new
        {
            ReservationId = dto.ReservationId,
            ServiceId = dto.ServiceId,
            ServiceName = service.Name,
            ServicePrice = service.Price,
            TotalPrice = totalPrice
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var reservationService = await _context.ReservationServices
            .FirstOrDefaultAsync(rs => rs.Id == id);

        if (reservationService == null)
        {
            return NotFound();
        }

        var reservationId = reservationService.ReservationId;

        _context.ReservationServices.Remove(reservationService);
        await _context.SaveChangesAsync();

        await RecalculateReservationTotalAsync(reservationId);

        return NoContent();
    }

    private async Task<decimal> RecalculateReservationTotalAsync(int reservationId)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
        {
            return 0m;
        }

        decimal basePrice;

        if (reservation.UnitAmountSnapshot.HasValue)
        {
            basePrice = reservation.UnitAmountSnapshot.Value * reservation.UnitsCount;
        }
        else
        {
            basePrice = reservation.TotalPrice ?? 0m;
        }

        var servicesTotal = await _context.ReservationServices
            .Include(rs => rs.Service)
            .Where(rs => rs.ReservationId == reservationId)
            .SumAsync(rs => rs.Service.Price);

        reservation.TotalPrice = basePrice + servicesTotal;

        await _context.SaveChangesAsync();

        return reservation.TotalPrice ?? 0m;
    }
}

public class ReservationServiceCreateDto
{
    public int ReservationId { get; set; }

    public int ServiceId { get; set; }
}