using SportLife.DTOs;
using SportLife.Entities;

namespace SportLife.Services;

public interface IMembershipService
{
    Task<MembershipDto?> GetUserMembership(Guid userId);
    Task<Membership> ActivateMembership(Guid userId, Guid planId, string mpPaymentId);
}
