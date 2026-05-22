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
    public async Task<ActionResult<IEnumerable<Review>>> GetAll()
    {
        return await _context.Reviews
            .Include(r => r.Author)
            .Include(r => r.Reservation)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Review>> GetById(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Author)
            .Include(r => r.Reservation)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (review == null) return NotFound();
        return review;
    }

    [HttpPost]
    public async Task<ActionResult<Review>> Create(Review review)
    {
        review.CreatedAt = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Review review)
    {
        if (id != review.Id) return BadRequest();
        _context.Entry(review).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound();
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}