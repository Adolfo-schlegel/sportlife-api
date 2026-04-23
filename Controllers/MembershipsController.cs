using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLife.Data;
using SportLife.DTOs;
using SportLife.Services;
using System.Security.Claims;

namespace SportLife.Controllers;

[ApiController]
[Route("api/memberships")]
[Authorize]
public class MembershipsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMembershipService _membershipService;

    public MembershipsController(AppDbContext db, IMembershipService membershipService)
    {
        _db = db;
        _membershipService = membershipService;
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var memberships = await _db.Memberships
            .Include(m => m.User)
            .Include(m => m.Plan)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MembershipDto(
                m.Id, m.UserId, m.User!.Name, m.PlanId, m.Plan != null ? m.Plan.Name : null,
                m.ExpiresAt, m.Status))
            .ToListAsync();
        return Ok(memberships);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var membership = await _membershipService.GetUserMembership(userId);
        if (membership == null) return Ok(new { status = "none" });
        return Ok(membership);
    }

    [HttpPost("activate")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Activate([FromBody] CreateMembershipRequest req)
    {
        try
        {
            var membership = await _membershipService.ActivateMembership(req.UserId, req.PlanId, "manual");
            return Ok(new { message = "Membership activated", membershipId = membership.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stats")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalMembers = await _db.Users.CountAsync(u => u.Role == "member");
        var activeMembers = await _db.Memberships
            .Where(m => m.Status == "active" && m.ExpiresAt > now)
            .Select(m => m.UserId)
            .Distinct()
            .CountAsync();
        var expiredMembers = await _db.Memberships
            .Where(m => m.Status == "expired" || (m.Status == "active" && m.ExpiresAt <= now))
            .Select(m => m.UserId)
            .Distinct()
            .CountAsync();
        var monthlyRevenue = await _db.Payments
            .Where(p => p.Status == "approved" && p.ApprovedAt >= monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
        var totalPayments = await _db.Payments.CountAsync(p => p.Status == "approved");

        return Ok(new DashboardStats(totalMembers, activeMembers, expiredMembers, monthlyRevenue, totalPayments));
    }

    [HttpGet("expiring")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetExpiring([FromQuery] int days = 7)
    {
        var now = DateTime.UtcNow;
        var limit = now.AddDays(days);

        var expiring = await _db.Memberships
            .Include(m => m.User)
            .Include(m => m.Plan)
            .Where(m => m.Status == "active" && m.ExpiresAt.HasValue && m.ExpiresAt.Value > now && m.ExpiresAt.Value <= limit)
            .OrderBy(m => m.ExpiresAt)
            .Select(m => new ExpiringMembershipDto(
                m.Id,
                m.UserId,
                m.User!.Name,
                m.PlanId!.Value,
                m.Plan!.Name,
                m.ExpiresAt!.Value,
                (int)(m.ExpiresAt!.Value - now).TotalDays
            ))
            .ToListAsync();

        return Ok(expiring);
    }
}
