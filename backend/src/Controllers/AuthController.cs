using Expense.Api.Data;
using Expense.Api.Dtos;
using Expense.Api.Models;
using Expense.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Expense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IPasswordHasher _hasher;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(AppDbContext db, IJwtTokenService jwt, IPasswordHasher hasher, IValidator<LoginRequest> loginValidator)
    {
        _db = db;
        _jwt = jwt;
        _hasher = hasher;
        _loginValidator = loginValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest dto, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var user = await _db.Users.Include(u => u.RefreshTokens).SingleOrDefaultAsync(u => u.Username == dto.Username, ct);
        if (user == null || !_hasher.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        var access = _jwt.GenerateAccessToken(user);
        var refresh = _jwt.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refresh,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow,
            Revoked = false
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        return Ok(new LoginResponse
        {
            AccessToken = access,
            RefreshToken = refresh,
            Username = user.Username,
            Role = user.Role.ToString()
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest dto, CancellationToken ct)
    {
        var token = await _db.RefreshTokens.Include(t => t.User).SingleOrDefaultAsync(t => t.Token == dto.RefreshToken && !t.Revoked, ct);
        if (token == null || token.ExpiresAtUtc < DateTime.UtcNow || token.User == null)
        {
            return Unauthorized(new { error = "Invalid refresh token" });
        }

        var access = _jwt.GenerateAccessToken(token.User);
        var newRefresh = _jwt.GenerateRefreshToken();
        token.Revoked = true;
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = token.UserId,
            Token = newRefresh,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow,
            Revoked = false
        };
        _db.RefreshTokens.Add(replacement);
        await _db.SaveChangesAsync(ct);

        return Ok(new LoginResponse
        {
            AccessToken = access,
            RefreshToken = newRefresh,
            Username = token.User.Username,
            Role = token.User.Role.ToString()
        });
    }
}

