namespace SportLife.Entities;

public class MembershipPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
