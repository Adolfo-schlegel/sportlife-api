using Microsoft.EntityFrameworkCore;
using SportLife.Entities;

namespace SportLife.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Name).HasColumnName("name");
            e.Property(u => u.Email).HasColumnName("email");
            e.Property(u => u.PasswordHash).HasColumnName("password_hash");
            e.Property(u => u.Role).HasColumnName("role");
            e.Property(u => u.CreatedAt).HasColumnName("created_at");
            e.HasMany(u => u.Memberships).WithOne(m => m.User).HasForeignKey(m => m.UserId);
            e.HasMany(u => u.Payments).WithOne(p => p.User).HasForeignKey(p => p.UserId);
        });

        // MembershipPlans
        modelBuilder.Entity<MembershipPlan>(e =>
        {
            e.ToTable("membership_plans");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Name).HasColumnName("name");
            e.Property(p => p.Price).HasColumnName("price");
            e.Property(p => p.DurationDays).HasColumnName("duration_days");
            e.Property(p => p.Active).HasColumnName("active");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
        });

        // Memberships
        modelBuilder.Entity<Membership>(e =>
        {
            e.ToTable("memberships");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.UserId).HasColumnName("user_id");
            e.Property(m => m.PlanId).HasColumnName("plan_id");
            e.Property(m => m.ExpiresAt).HasColumnName("expires_at");
            e.Property(m => m.Status).HasColumnName("status");
            e.Property(m => m.CreatedAt).HasColumnName("created_at");
            e.HasOne(m => m.Plan).WithMany(p => p.Memberships).HasForeignKey(m => m.PlanId);
        });

        // Payments
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.UserId).HasColumnName("user_id");
            e.Property(p => p.PlanId).HasColumnName("plan_id");
            e.Property(p => p.MercadoPagoPaymentId).HasColumnName("mercado_pago_payment_id");
            e.Property(p => p.PreferenceId).HasColumnName("preference_id");
            e.Property(p => p.Status).HasColumnName("status");
            e.Property(p => p.Amount).HasColumnName("amount");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.ApprovedAt).HasColumnName("approved_at");
            e.HasOne(p => p.Plan).WithMany(pl => pl.Payments).HasForeignKey(p => p.PlanId);
        });
    }
}
