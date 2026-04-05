namespace SportLife.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string? MercadoPagoPaymentId { get; set; }
    public string? PreferenceId { get; set; }
    public string Status { get; set; } = "pending";
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public User? User { get; set; }
    public MembershipPlan? Plan { get; set; }
}
