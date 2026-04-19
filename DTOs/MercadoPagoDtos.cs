namespace SportLife.DTOs;

public record MercadoPagoConfigDto(
    string AccessToken,
    string PublicKey,
    string WebhookSecret,
    string NotificationUrl,
    string SuccessUrl,
    string FailureUrl,
    string PendingUrl,
    bool IsTestMode,
    DateTime UpdatedAt
);

public record UpdateMercadoPagoConfigRequest(
    string? AccessToken,
    string? PublicKey,
    string? WebhookSecret,
    string? NotificationUrl,
    string? SuccessUrl,
    string? FailureUrl,
    string? PendingUrl,
    bool IsTestMode
);

public record TestConnectionResponse(bool Connected, string? Error);
