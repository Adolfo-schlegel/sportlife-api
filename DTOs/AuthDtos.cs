namespace SportLife.DTOs;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Role, Guid UserId, string Name);

public record UserDto(Guid Id, string Name, string Email, string Role, DateTime CreatedAt);
public record UpdateUserRequest(string? Name, string? Email, string? Role);

public record PlanDto(Guid Id, string Name, decimal Price, int DurationDays, bool Active);
public record CreatePlanRequest(string Name, decimal Price, int DurationDays);
public record UpdatePlanRequest(string? Name, decimal? Price, int? DurationDays, bool? Active);

public record MembershipDto(Guid Id, Guid UserId, string UserName, Guid? PlanId, string? PlanName, DateTime? ExpiresAt, string Status);
public record CreateMembershipRequest(Guid UserId, Guid PlanId);

public record PaymentDto(Guid Id, Guid UserId, string UserName, Guid PlanId, string PlanName, string? MercadoPagoPaymentId, string? PreferenceId, string Status, decimal Amount, DateTime CreatedAt, DateTime? ApprovedAt);
public record CreatePaymentPreferenceRequest(Guid PlanId);
public record CreatePreferenceRequest(string Title, decimal Amount, Guid PlanId, Guid UserId, string UserEmail);
public record PreferenceResponse(string PreferenceId, string InitPoint);

public record WebhookPayload(string? Type, string? Action, WebhookData? Data);
public record WebhookData(string? Id);

public record DashboardStats(int TotalMembers, int ActiveMembers, int ExpiredMembers, decimal MonthlyRevenue, int TotalPayments);
