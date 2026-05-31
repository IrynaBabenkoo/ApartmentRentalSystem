using ApartmentRentalSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AmenitiesController : ControllerBase
{
    private readonly ApartmentContext _context;

    public AmenitiesController(ApartmentContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var result = await _context.Amenities
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name
            })
            .ToListAsync();

        return Ok(result);
    }
}
