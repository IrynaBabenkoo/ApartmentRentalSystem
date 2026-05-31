using ApartmentRentalSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeUnitsController : ControllerBase
{
    private readonly ApartmentContext _context;

    public TimeUnitsController(ApartmentContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var result = await _context.TimeUnits
            .OrderBy(t => t.Id)
            .Select(t => new
            {
                t.Id,
                t.Name
            })
            .ToListAsync();

        return Ok(result);
    }
}
