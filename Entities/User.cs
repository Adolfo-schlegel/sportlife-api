namespace SportLife.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "member";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
