using ApartmentRentalSystem.Domain.Entities;
using ApartmentRentalSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ApartmentRentalSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApartmentContext _context;
    private const int TenantRoleId = 1;

    public AuthController(ApartmentContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var normalizedEmail = dto.Email.Trim().ToLower();

        var exists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

        if (exists)
            return BadRequest("Користувач з таким email вже існує.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            Phone = dto.Phone?.Trim() ?? string.Empty,
            RoleId = dto.RoleId ?? TenantRoleId,
            Password = dto.Password.Trim()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (user.RoleId == TenantRoleId)
        {
            var hasCard = await _context.LoyaltyCards.AnyAsync(c => c.UserId == user.Id);
            if (!hasCard)
            {
                _context.LoyaltyCards.Add(new LoyaltyCard
                {
                    UserId = user.Id,
                    Points = 0,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
        }

        return Ok(new
        {
            message = "Реєстрація успішна",
            user.Id,
            user.FullName,
            user.Email,
            user.RoleId
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var normalizedEmail = dto.Email.Trim().ToLower();

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == normalizedEmail &&
                u.Password == dto.Password.Trim());

        if (user == null)
            return Unauthorized("Невірний email або пароль.");

        return Ok(new
        {
            message = "Вхід виконано успішно",
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.RoleId,
            Role = user.Role.Name
        });
    }
}

public class RegisterDto
{
    [Required(ErrorMessage = "Вкажіть ім'я та прізвище.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть email.")]
    [EmailAddress(ErrorMessage = "Некоректний email.")]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public int? RoleId { get; set; }

    [Required(ErrorMessage = "Вкажіть пароль.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required(ErrorMessage = "Вкажіть email.")]
    [EmailAddress(ErrorMessage = "Некоректний email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль.")]
    public string Password { get; set; } = string.Empty;
}