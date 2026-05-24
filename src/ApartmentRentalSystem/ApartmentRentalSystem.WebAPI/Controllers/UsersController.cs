using ApartmentRentalSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var result = await _context.Users
            .Include(u => u.Role)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Phone,
                Role = u.Role.Name,
                u.RoleId
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound("Користувача не знайдено.");

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            Role = user.Role.Name,
            user.RoleId
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Користувача не знайдено.");

        var normalizedEmail = dto.Email.Trim().ToLower();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Id != id && u.Email.ToLower() == normalizedEmail);

        if (emailExists)
            return BadRequest("Інший користувач уже має такий email.");

        user.FullName = dto.FullName.Trim();
        user.Email = normalizedEmail;
        user.Phone = dto.Phone?.Trim() ?? string.Empty;

        if (dto.RoleId.HasValue)
            user.RoleId = dto.RoleId.Value;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("Користувача не знайдено.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class UpdateUserDto
{
    [Required(ErrorMessage = "Вкажіть ім'я та прізвище.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть email.")]
    [EmailAddress(ErrorMessage = "Некоректний email.")]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public int? RoleId { get; set; }
}