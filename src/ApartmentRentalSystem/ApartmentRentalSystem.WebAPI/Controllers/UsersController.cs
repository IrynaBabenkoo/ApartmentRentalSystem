using ApartmentRentalSystem.Infrastructure;
using ApartmentRentalSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApartmentContext _context;

    public UsersController(ApartmentContext context)
    {
        _context = context;
    }

    // Всі користувачі
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var result = await _context.Users
            .Include(u => u.Role)
            .Select(u => new {
                u.Id,
                u.FullName,
                u.Email,
                u.Phone,
                Role = u.Role.Name
            }).ToListAsync();

        return Ok(result);
    }

    // Один користувач
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var u = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (u == null) return NotFound();

        return Ok(new
        {
            u.Id,
            u.FullName,
            u.Email,
            u.Phone,
            Role = u.Role.Name
        });
    }

    // Реєстрація
    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] UserRegisterDto dto)
    {
        var exists = await _context.Users
            .AnyAsync(u => u.Email == dto.Email);

        if (exists) return BadRequest("Користувач з таким email вже існує");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone ?? string.Empty,
            RoleId = dto.RoleId ?? 1
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Автоматично створюємо картку лояльності для орендаря
        if (user.RoleId == 1)
        {
            _context.LoyaltyCards.Add(new LoyaltyCard
            {
                UserId = user.Id,
                Points = 0,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new { user.Id, user.FullName, user.Email });
    }

    // Вхід (простий — без JWT)
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] UserLoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null) return Unauthorized("Користувача не знайдено");

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            Role = user.Role.Name
        });
    }

    // Редагувати
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserRegisterDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.Phone = dto.Phone ?? string.Empty;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Видалити
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class UserRegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int? RoleId { get; set; }
}

public class UserLoginDto
{
    public string Email { get; set; } = string.Empty;
}