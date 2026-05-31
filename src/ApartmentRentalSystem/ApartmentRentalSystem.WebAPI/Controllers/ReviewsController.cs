using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ApartmentContext _context;

    public ReviewsController(ApartmentContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var reviews = await _context.Reviews
            .Include(r => r.Author)
            .Include(r => r.Reservation)
                .ThenInclude(res => res.Apartment)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.ReservationId,
                r.AuthorId,
                Author = r.Author.FullName,
                ApartmentId = r.Reservation.ApartmentId,
                Apartment = r.Reservation.Apartment.Title,
                City = r.Reservation.Apartment.City,
                r.Rating,
                r.Comment,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Author)
            .Include(r => r.Reservation)
                .ThenInclude(res => res.Apartment)
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.ReservationId,
                r.AuthorId,
                Author = r.Author.FullName,
                ApartmentId = r.Reservation.ApartmentId,
                Apartment = r.Reservation.Apartment.Title,
                City = r.Reservation.Apartment.City,
                r.Rating,
                r.Comment,
                r.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (review == null)
        {
            return NotFound("Відгук не знайдено.");
        }

        return Ok(review);
    }

    [HttpGet("apartment/{apartmentId}")]
    public async Task<ActionResult<object>> GetByApartment(int apartmentId)
    {
        var apartment = await _context.Apartments
            .FirstOrDefaultAsync(a => a.Id == apartmentId);

        if (apartment == null)
        {
            return NotFound("Апартамент не знайдено.");
        }

        var reviews = await _context.Reviews
            .Include(r => r.Author)
            .Include(r => r.Reservation)
            .Where(r => r.Reservation.ApartmentId == apartmentId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.ReservationId,
                r.AuthorId,
                Author = r.Author.FullName,
                r.Rating,
                r.Comment,
                r.CreatedAt
            })
            .ToListAsync();

        var averageRating = reviews.Any()
            ? Math.Round(reviews.Average(r => r.Rating), 1)
            : 0;

        return Ok(new
        {
            ApartmentId = apartment.Id,
            Apartment = apartment.Title,
            City = apartment.City,
            AverageRating = averageRating,
            ReviewsCount = reviews.Count,
            Reviews = reviews
        });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] ReviewCreateDto dto)
    {
        if (dto.ReservationId <= 0)
        {
            return BadRequest("Бронювання не вибрано.");
        }

        if (dto.AuthorId <= 0)
        {
            return BadRequest("Користувач не визначений.");
        }

        if (dto.Rating < 1 || dto.Rating > 5)
        {
            return BadRequest("Оцінка має бути від 1 до 5.");
        }

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest("Коментар не може бути порожнім.");
        }

        var reservation = await _context.Reservations
            .Include(r => r.Apartment)
            .Include(r => r.Status)
            .FirstOrDefaultAsync(r => r.Id == dto.ReservationId);

        if (reservation == null)
        {
            return NotFound("Бронювання не знайдено.");
        }

        if (reservation.GuestId != dto.AuthorId)
        {
            return BadRequest("Відгук може залишити тільки орендар цього бронювання.");
        }

        var statusName = reservation.Status?.Name?.Trim().ToLower() ?? "";

        var canReview =
            statusName == "оплачено" ||
            statusName == "підтверджено" ||
            statusName == "paid" ||
            statusName == "confirmed";

        if (!canReview)
        {
            return BadRequest("Відгук можна залишити тільки після оплати або підтвердження бронювання.");
        }

        var alreadyExists = await _context.Reviews
            .AnyAsync(r => r.ReservationId == dto.ReservationId);

        if (alreadyExists)
        {
            return BadRequest("Для цього бронювання вже залишено відгук.");
        }

        var review = new Review
        {
            ReservationId = dto.ReservationId,
            AuthorId = dto.AuthorId,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = review.Id },
            new
            {
                review.Id,
                review.ReservationId,
                review.AuthorId,
                ApartmentId = reservation.ApartmentId,
                Apartment = reservation.Apartment.Title,
                review.Rating,
                review.Comment,
                review.CreatedAt
            });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ReviewUpdateDto dto)
    {
        var review = await _context.Reviews.FindAsync(id);

        if (review == null)
        {
            return NotFound("Відгук не знайдено.");
        }

        if (dto.Rating < 1 || dto.Rating > 5)
        {
            return BadRequest("Оцінка має бути від 1 до 5.");
        }

        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return BadRequest("Коментар не може бути порожнім.");
        }

        review.Rating = dto.Rating;
        review.Comment = dto.Comment.Trim();

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews.FindAsync(id);

        if (review == null)
        {
            return NotFound("Відгук не знайдено.");
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class ReviewCreateDto
{
    public int ReservationId { get; set; }

    public int AuthorId { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;
}

public class ReviewUpdateDto
{
    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;
}