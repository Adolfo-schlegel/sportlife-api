using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLife.Data;
using SportLife.DTOs;
using SportLife.Entities;
using System.Net.Http.Headers;

namespace SportLife.Controllers;

[ApiController]
[Route("api/configurations")]
[Authorize(Roles = "admin")]
public class ConfigurationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public ConfigurationsController(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    [HttpGet("mercadopago")]
    public async Task<IActionResult> GetMercadoPagoConfig()
    {
        var config = await _db.MercadoPagoConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            // Return defaults from appsettings with empty sensitive fields
            return Ok(new MercadoPagoConfigDto(
                AccessToken: "",
                PublicKey: "",
                WebhookSecret: "",
                NotificationUrl: _config["MercadoPago:NotificationUrl"] ?? "",
                SuccessUrl: _config["MercadoPago:SuccessUrl"] ?? "",
                FailureUrl: _config["MercadoPago:FailureUrl"] ?? "",
                PendingUrl: _config["MercadoPago:PendingUrl"] ?? "",
                IsTestMode: true,
                UpdatedAt: DateTime.UtcNow
            ));
        }

        return Ok(new MercadoPagoConfigDto(
            AccessToken: Mask(config.AccessToken),
            PublicKey: config.PublicKey,
            WebhookSecret: Mask(config.WebhookSecret),
            NotificationUrl: config.NotificationUrl,
            SuccessUrl: config.SuccessUrl,
            FailureUrl: config.FailureUrl,
            PendingUrl: config.PendingUrl,
            IsTestMode: config.IsTestMode,
            UpdatedAt: config.UpdatedAt
        ));
    }

    [HttpPut("mercadopago")]
    public async Task<IActionResult> UpdateMercadoPagoConfig([FromBody] UpdateMercadoPagoConfigRequest req)
    {
        var config = await _db.MercadoPagoConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new MercadoPagoConfig();
            _db.MercadoPagoConfigs.Add(config);
        }

        // Only update sensitive fields if not masked
        if (!string.IsNullOrEmpty(req.AccessToken) && !req.AccessToken.Contains("****"))
            config.AccessToken = req.AccessToken;

        if (!string.IsNullOrEmpty(req.WebhookSecret) && !req.WebhookSecret.Contains("****"))
            config.WebhookSecret = req.WebhookSecret;

        if (req.PublicKey != null) config.PublicKey = req.PublicKey;
        if (req.NotificationUrl != null) config.NotificationUrl = req.NotificationUrl;
        if (req.SuccessUrl != null) config.SuccessUrl = req.SuccessUrl;
        if (req.FailureUrl != null) config.FailureUrl = req.FailureUrl;
        if (req.PendingUrl != null) config.PendingUrl = req.PendingUrl;

        config.IsTestMode = req.IsTestMode;
        config.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new MercadoPagoConfigDto(
            AccessToken: Mask(config.AccessToken),
            PublicKey: config.PublicKey,
            WebhookSecret: Mask(config.WebhookSecret),
            NotificationUrl: config.NotificationUrl,
            SuccessUrl: config.SuccessUrl,
            FailureUrl: config.FailureUrl,
            PendingUrl: config.PendingUrl,
            IsTestMode: config.IsTestMode,
            UpdatedAt: config.UpdatedAt
        ));
    }

    [HttpPost("mercadopago/test")]
    public async Task<IActionResult> TestConnection()
    {
        var config = await _db.MercadoPagoConfigs.FirstOrDefaultAsync();
        var token = config?.AccessToken ?? _config["MercadoPago:AccessToken"] ?? "";

        if (string.IsNullOrEmpty(token))
            return BadRequest(new TestConnectionResponse(false, "No hay Access Token configurado."));

        try
        {
            var client = _httpClientFactory.CreateClient("MercadoPago");
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mercadopago.com/v1/payment_methods");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return Ok(new TestConnectionResponse(true, null));

            var body = await response.Content.ReadAsStringAsync();
            return BadRequest(new TestConnectionResponse(false, $"MP respondió {(int)response.StatusCode}: {body[..Math.Min(200, body.Length)]}"));
        }
        catch (Exception ex)
        {
            return BadRequest(new TestConnectionResponse(false, ex.Message));
        }
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length <= 6) return "****";
        return $"****{value[^6..]}";
    }
}
