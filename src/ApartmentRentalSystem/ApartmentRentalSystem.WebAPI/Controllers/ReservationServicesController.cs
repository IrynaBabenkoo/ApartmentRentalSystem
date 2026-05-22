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
    public async Task<ActionResult<IEnumerable<ReservationService>>> GetAll()
    {
        return await _context.ReservationServices
            .Include(rs => rs.Reservation)
            .Include(rs => rs.Service)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReservationService>> GetById(int id)
    {
        var rs = await _context.ReservationServices
            .Include(rs => rs.Reservation)
            .Include(rs => rs.Service)
            .FirstOrDefaultAsync(rs => rs.Id == id);
        if (rs == null) return NotFound();
        return rs;
    }

    [HttpPost]
    public async Task<ActionResult<ReservationService>> Create(ReservationService rs)
    {
        _context.ReservationServices.Add(rs);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = rs.Id }, rs);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ReservationService rs)
    {
        if (id != rs.Id) return BadRequest();
        _context.Entry(rs).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rs = await _context.ReservationServices.FindAsync(id);
        if (rs == null) return NotFound();
        _context.ReservationServices.Remove(rs);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
