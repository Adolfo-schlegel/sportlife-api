namespace SportLife.Entities;

public class Membership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? PlanId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
    public MembershipPlan? Plan { get; set; }
}
