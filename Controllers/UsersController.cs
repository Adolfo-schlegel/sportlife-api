using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLife.Data;
using SportLife.DTOs;
using SportLife.Services;
using System.Security.Claims;

namespace SportLife.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMembershipService _membershipService;

    public UsersController(AppDbContext db, IMembershipService membershipService)
    {
        _db = db;
        _membershipService = membershipService;
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role, u.CreatedAt))
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id, user.Name, user.Email, user.Role, user.CreatedAt));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id, user.Name, user.Email, user.Role, user.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (req.Name != null) user.Name = req.Name;
        if (req.Email != null) user.Email = req.Email.ToLower();
        if (req.Role != null) user.Role = req.Role;

        await _db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.Name, user.Email, user.Role, user.CreatedAt));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/membership")]
    public async Task<IActionResult> GetMembership(Guid id)
    {
        var requesterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (requesterId != id && role != "admin")
            return Forbid();

        var membership = await _membershipService.GetUserMembership(id);
        if (membership == null) return NotFound(new { message = "No membership found" });
        return Ok(membership);
    }
}
