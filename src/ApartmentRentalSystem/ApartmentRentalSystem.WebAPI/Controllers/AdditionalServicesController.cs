using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdditionalServicesController : ControllerBase
{
    private readonly ApartmentContext _context;

    public AdditionalServicesController(ApartmentContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdditionalService>>> GetAll()
    {
        return await _context.AdditionalServices.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdditionalService>> GetById(int id)
    {
        var service = await _context.AdditionalServices.FindAsync(id);
        if (service == null) return NotFound();
        return service;
    }

    [HttpPost]
    public async Task<ActionResult<AdditionalService>> Create(AdditionalService service)
    {
        _context.AdditionalServices.Add(service);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = service.Id }, service);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, AdditionalService service)
    {
        if (id != service.Id) return BadRequest();
        _context.Entry(service).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.AdditionalServices.FindAsync(id);
        if (service == null) return NotFound();
        _context.AdditionalServices.Remove(service);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}