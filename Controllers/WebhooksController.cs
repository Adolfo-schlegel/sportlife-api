using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLife.Data;
using SportLife.DTOs;
using SportLife.Services;
using System.Text;
using System.Text.Json;

namespace SportLife.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMembershipService _membershipService;
    private readonly ILogger<WebhooksController> _logger;
    private readonly IConfiguration _config;

    public WebhooksController(AppDbContext db, IMembershipService membershipService, ILogger<WebhooksController> logger, IConfiguration config)
    {
        _db = db;
        _membershipService = membershipService;
        _logger = logger;
        _config = config;
    }

    [HttpPost("mercadopago")]
    public async Task<IActionResult> MercadoPago([FromBody] JsonElement payload)
    {
        _logger.LogInformation("MercadoPago webhook received: {Payload}", payload.ToString());

        try
        {
            string? type = null;
            string? paymentId = null;

            if (payload.TryGetProperty("type", out var typeEl))
                type = typeEl.GetString();

            if (payload.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("id", out var idEl))
                paymentId = idEl.GetString();

            if (type != "payment" || paymentId == null)
                return Ok();

            // Fetch payment details from MP
            var dbConfig = await _db.MercadoPagoConfigs.FirstOrDefaultAsync();
            var accessToken = dbConfig?.AccessToken ?? _config["MercadoPago:AccessToken"] ?? "";

            var client = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("MercadoPago");
            var mpRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
            mpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await client.SendAsync(mpRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch payment {PaymentId} from MP", paymentId);
                return Ok();
            }

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
            var externalRef = root.TryGetProperty("external_reference", out var extEl) ? extEl.GetString() : null;

            if (status != "approved" || externalRef == null)
                return Ok();

            // external_reference = "userId|planId"
            var parts = externalRef.Split('|');
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId) || !Guid.TryParse(parts[1], out var planId))
            {
                _logger.LogWarning("Invalid external_reference: {Ref}", externalRef);
                return Ok();
            }

            // Check if already processed
            var existing = await _db.Payments
                .AnyAsync(p => p.MercadoPagoPaymentId == paymentId);

            if (!existing)
            {
                await _membershipService.ActivateMembership(userId, planId, paymentId);
                _logger.LogInformation("Membership activated via webhook for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MercadoPago webhook");
        }

        return Ok();
    }
}
