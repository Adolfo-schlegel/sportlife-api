using Microsoft.EntityFrameworkCore;
using SportLife.Data;
using SportLife.DTOs;
using SportLife.Entities;

namespace SportLife.Services;

public class MembershipService : IMembershipService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MembershipService> _logger;

    public MembershipService(AppDbContext db, ILogger<MembershipService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MembershipDto?> GetUserMembership(Guid userId)
    {
        var membership = await _db.Memberships
            .Include(m => m.User)
            .Include(m => m.Plan)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (membership == null) return null;

        // Update status based on expiry
        if (membership.ExpiresAt.HasValue && membership.ExpiresAt.Value < DateTime.UtcNow && membership.Status == "active")
        {
            membership.Status = "expired";
            await _db.SaveChangesAsync();
        }

        return new MembershipDto(
            membership.Id,
            membership.UserId,
            membership.User?.Name ?? "",
            membership.PlanId,
            membership.Plan?.Name,
            membership.ExpiresAt,
            membership.Status
        );
    }

    public async Task<Membership> ActivateMembership(Guid userId, Guid planId, string mpPaymentId)
    {
        var plan = await _db.MembershipPlans.FindAsync(planId)
            ?? throw new Exception("Plan not found");

        var existing = await _db.Memberships
            .Where(m => m.UserId == userId && m.Status == "active")
            .FirstOrDefaultAsync();

        DateTime baseDate = DateTime.UtcNow;
        if (existing != null && existing.ExpiresAt.HasValue && existing.ExpiresAt.Value > baseDate)
        {
            baseDate = existing.ExpiresAt.Value;
            existing.Status = "superseded";
        }

        var newExpiry = baseDate.AddDays(plan.DurationDays);

        var membership = new Membership
        {
            UserId = userId,
            PlanId = planId,
            ExpiresAt = newExpiry,
            Status = "active"
        };

        _db.Memberships.Add(membership);

        // Update payment record
        var payment = await _db.Payments
            .Where(p => p.UserId == userId && p.PlanId == planId && p.Status == "pending")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (payment != null)
        {
            payment.Status = "approved";
            payment.MercadoPagoPaymentId = mpPaymentId;
            payment.ApprovedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Membership activated for user {UserId}, expires {ExpiresAt}", userId, newExpiry);

        return membership;
    }
}
