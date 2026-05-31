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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var result = await _context.Payments
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Apartment)
            .Include(p => p.Method)
            .Select(p => new
            {
                p.Id,
                p.Amount,
                p.Currency,
                p.PaidAt,
                PaymentMethod = p.Method.Name,
                Apartment = p.Reservation.Apartment.Title,
                p.ReservationId
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Reservation)
                .ThenInclude(r => r.Apartment)
            .Include(p => p.Method)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound("Оплату не знайдено.");
        }

        return Ok(new
        {
            payment.Id,
            payment.Amount,
            payment.Currency,
            payment.PaidAt,
            PaymentMethod = payment.Method.Name,
            Apartment = payment.Reservation.Apartment.Title,
            payment.ReservationId
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Pay([FromBody] PaymentCreateDto dto)
    {
        if (dto.ReservationId <= 0)
        {
            return BadRequest("Бронювання не вибрано.");
        }

        if (dto.PaymentMethodId <= 0)
        {
            return BadRequest("Метод оплати не вибрано.");
        }

        if (dto.PointsToUse < 0)
        {
            return BadRequest("Кількість бонусів для списання не може бути від’ємною.");
        }

        var reservation = await _context.Reservations
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == dto.ReservationId);

        if (reservation == null)
        {
            return NotFound("Бронювання не знайдено.");
        }

        if (reservation.Payment != null)
        {
            return BadRequest("Бронювання вже оплачено.");
        }

        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == dto.PaymentMethodId);

        if (paymentMethod == null)
        {
            return NotFound("Метод оплати не знайдено.");
        }

        decimal originalAmount = reservation.TotalPrice ?? dto.Amount;

        if (originalAmount <= 0)
        {
            return BadRequest("Сума бронювання має бути більшою за 0.");
        }

        var loyaltyCard = await _context.LoyaltyCards
            .FirstOrDefaultAsync(lc => lc.UserId == reservation.GuestId);

        int actualPointsToUse = 0;

        if (dto.PointsToUse > 0)
        {
            if (loyaltyCard == null)
            {
                return BadRequest("Бонусну карту користувача не знайдено.");
            }

            int maxByPercent = (int)Math.Floor(originalAmount * 0.30m);

            actualPointsToUse = Math.Min(dto.PointsToUse, loyaltyCard.Points);
            actualPointsToUse = Math.Min(actualPointsToUse, maxByPercent);

            if (actualPointsToUse < 0)
            {
                actualPointsToUse = 0;
            }
        }

        decimal discountAmount = actualPointsToUse;
        decimal finalAmount = originalAmount - discountAmount;

        if (finalAmount < 0)
        {
            finalAmount = 0;
        }

        var payment = new Payment
        {
            ReservationId = dto.ReservationId,
            Amount = finalAmount,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "UAH" : dto.Currency,
            PaidAt = DateTime.UtcNow,
            MethodId = dto.PaymentMethodId
        };

        _context.Payments.Add(payment);

        var confirmedStatus = await _context.ReservationStatuses
            .FirstOrDefaultAsync(s => s.Name == "Підтверджено");

        if (confirmedStatus != null)
        {
            reservation.StatusId = confirmedStatus.Id;
        }

        int pointsAdded = 0;

        if (loyaltyCard != null)
        {
            if (actualPointsToUse > 0)
            {
                loyaltyCard.Points -= actualPointsToUse;

                _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                {
                    CardId = loyaltyCard.Id,
                    Points = -actualPointsToUse,
                    Description = $"Списано бонуси за бронювання #{dto.ReservationId}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (finalAmount > 0)
            {
                pointsAdded = (int)(finalAmount / 100m);

                if (pointsAdded > 0)
                {
                    loyaltyCard.Points += pointsAdded;

                    _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                    {
                        CardId = loyaltyCard.Id,
                        Points = pointsAdded,
                        Description = $"Нараховано за бронювання #{dto.ReservationId}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = payment.Id },
            new
            {
                payment.Id,
                payment.ReservationId,
                OriginalAmount = originalAmount,
                DiscountPoints = actualPointsToUse,
                DiscountAmount = discountAmount,
                PaidAmount = payment.Amount,
                payment.Currency,
                payment.PaidAt,
                PaymentMethod = paymentMethod.Name,
                PointsAdded = pointsAdded,
                CurrentPoints = loyaltyCard?.Points ?? 0
            });
    }

    [HttpGet("methods")]
    public async Task<ActionResult<IEnumerable<object>>> GetMethods()
    {
        var methods = await _context.PaymentMethods
            .Select(m => new
            {
                m.Id,
                m.Name
            })
            .ToListAsync();

        return Ok(methods);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var payment = await _context.Payments.FindAsync(id);

        if (payment == null)
        {
            return NotFound("Оплату не знайдено.");
        }

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

    public int PointsToUse { get; set; }
}