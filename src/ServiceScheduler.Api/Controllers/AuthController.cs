using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Api.Data;
using ServiceScheduler.Api.Models;
using ServiceScheduler.Api.Services;
using ServiceScheduler.Shared.DTOs;

namespace ServiceScheduler.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(LoginDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict("Email already registered.");

        var user = new AppUser
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new AuthResponseDto
        {
            Token = tokenService.GenerateToken(user),
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        return Ok(new AuthResponseDto
        {
            Token = tokenService.GenerateToken(user),
            Email = user.Email,
            Role = user.Role
        });
    }
}
