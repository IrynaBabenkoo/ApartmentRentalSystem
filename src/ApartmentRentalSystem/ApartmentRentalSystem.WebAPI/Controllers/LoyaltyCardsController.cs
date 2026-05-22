using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoyaltyCardsController : ControllerBase
{
    private readonly ApartmentContext _context;

    public LoyaltyCardsController(ApartmentContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoyaltyCard>>> GetAll()
    {
        return await _context.LoyaltyCards.Include(lc => lc.User).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LoyaltyCard>> GetById(int id)
    {
        var card = await _context.LoyaltyCards.Include(lc => lc.User)
            .FirstOrDefaultAsync(lc => lc.Id == id);
        if (card == null) return NotFound();
        return card;
    }

    [HttpPost]
    public async Task<ActionResult<LoyaltyCard>> Create(LoyaltyCard card)
    {
        card.CreatedAt = DateTime.UtcNow;
        _context.LoyaltyCards.Add(card);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, LoyaltyCard card)
    {
        if (id != card.Id) return BadRequest();
        _context.Entry(card).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var card = await _context.LoyaltyCards.FindAsync(id);
        if (card == null) return NotFound();
        _context.LoyaltyCards.Remove(card);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
