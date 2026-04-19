using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportLife.Data;
using SportLife.DTOs;

namespace SportLife.Services;

public class PaymentService : IPaymentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;
    private readonly AppDbContext _db;

    public PaymentService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<PaymentService> logger, AppDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
        _db = db;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var dbConfig = await _db.MercadoPagoConfigs.FirstOrDefaultAsync();
        if (dbConfig != null && !string.IsNullOrEmpty(dbConfig.AccessToken))
            return dbConfig.AccessToken;
        return _config["MercadoPago:AccessToken"] ?? "";
    }

    private async Task<(string notification, string success, string failure, string pending)> GetUrlsAsync()
    {
        var dbConfig = await _db.MercadoPagoConfigs.FirstOrDefaultAsync();
        if (dbConfig != null && !string.IsNullOrEmpty(dbConfig.NotificationUrl))
            return (dbConfig.NotificationUrl, dbConfig.SuccessUrl, dbConfig.FailureUrl, dbConfig.PendingUrl);

        return (
            _config["MercadoPago:NotificationUrl"] ?? "",
            _config["MercadoPago:SuccessUrl"] ?? "",
            _config["MercadoPago:FailureUrl"] ?? "",
            _config["MercadoPago:PendingUrl"] ?? ""
        );
    }

    public async Task<PreferenceResponse> CreatePreference(CreatePreferenceRequest req)
    {
        var accessToken = await GetAccessTokenAsync();
        var (notificationUrl, successUrl, failureUrl, pendingUrl) = await GetUrlsAsync();

        var payload = new
        {
            items = new[]
            {
                new
                {
                    title = req.Title,
                    quantity = 1,
                    unit_price = req.Amount,
                    currency_id = "ARS"
                }
            },
            payer = new
            {
                email = req.UserEmail
            },
            external_reference = $"{req.UserId}|{req.PlanId}",
            notification_url = notificationUrl,
            back_urls = new
            {
                success = successUrl,
                failure = failureUrl,
                pending = pendingUrl
            },
            auto_return = "approved"
        };

        var client = _httpClientFactory.CreateClient("MercadoPago");
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/checkout/preferences")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MercadoPago error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new Exception($"MercadoPago error: {response.StatusCode} - {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;
        var preferenceId = root.GetProperty("id").GetString() ?? "";
        var initPoint = root.GetProperty("init_point").GetString() ?? "";

        return new PreferenceResponse(preferenceId, initPoint);
    }

    public async Task<dynamic?> GetPayment(string paymentId)
    {
        var accessToken = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient("MercadoPago");

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{paymentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MercadoPago GetPayment error: {StatusCode} - {Body}", response.StatusCode, body);
            return null;
        }

        return JsonSerializer.Deserialize<dynamic>(body);
    }
}
