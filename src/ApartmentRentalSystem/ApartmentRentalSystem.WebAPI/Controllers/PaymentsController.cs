using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ApartmentContext _context;

    public PaymentsController(ApartmentContext context)
    {
        _context = context;
    }

    // Всі платежі
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var result = await _context.Payments
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Apartment)
            .Include(p => p.Method)
            .Select(p => new {
                p.Id,
                p.Amount,
                p.Currency,
                p.PaidAt,
                PaymentMethod = p.Method.Name,
                Apartment = p.Reservation.Apartment.Title,
                p.ReservationId
            }).ToListAsync();

        return Ok(result);
    }

    // Один платіж
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var p = await _context.Payments
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Apartment)
            .Include(p => p.Method)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p == null) return NotFound();

        return Ok(new
        {
            p.Id,
            p.Amount,
            p.Currency,
            p.PaidAt,
            PaymentMethod = p.Method.Name,
            Apartment = p.Reservation.Apartment.Title,
            p.ReservationId
        });
    }

    // Оплатити бронювання
    [HttpPost]
    public async Task<ActionResult> Pay([FromBody] PaymentCreateDto dto)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == dto.ReservationId);

        if (reservation == null) return NotFound("Бронювання не знайдено");
        if (reservation.Payment != null) return BadRequest("Бронювання вже оплачено");

        var paymentMethod = await _context.PaymentMethods.FindAsync(dto.PaymentMethodId);
        if (paymentMethod == null) return NotFound("Метод оплати не знайдено");

        var payment = new Payment
        {
            ReservationId = dto.ReservationId,
            Amount = reservation.TotalPrice ?? dto.Amount,
            Currency = dto.Currency ?? "UAH",
            PaidAt = DateTime.UtcNow,
            MethodId = dto.PaymentMethodId
        };

        _context.Payments.Add(payment);

        // Змінюємо статус на "Підтверджено"
        var confirmedStatus = await _context.ReservationStatuses
            .FirstOrDefaultAsync(s => s.Name == "Підтверджено");

        if (confirmedStatus != null)
            reservation.StatusId = confirmedStatus.Id;

        // Нараховуємо бали лояльності (1 бал за кожні 100 грн)
        var loyaltyCard = await _context.LoyaltyCards
            .FirstOrDefaultAsync(lc => lc.UserId == reservation.GuestId);

        if (loyaltyCard != null && payment.Amount > 0)
        {
            var pointsToAdd = (int)(payment.Amount / 100);
            loyaltyCard.Points += pointsToAdd;

            _context.LoyaltyTransactions.Add(new LoyaltyTransaction
            {
                CardId = loyaltyCard.Id,
                Points = pointsToAdd,
                Description = $"Нараховано за бронювання #{dto.ReservationId}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = payment.Id },
            new { payment.Id, payment.Amount, payment.PaidAt });
    }

    // Методи оплати
    [HttpGet("methods")]
    public async Task<ActionResult<IEnumerable<object>>> GetMethods()
    {
        var methods = await _context.PaymentMethods
            .Select(m => new { m.Id, m.Name })
            .ToListAsync();
        return Ok(methods);
    }

    // Видалити платіж
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();
        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class PaymentCreateDto
{
    public int ReservationId { get; set; }
    public int PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
}