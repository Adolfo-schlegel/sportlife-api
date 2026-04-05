using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SportLife.DTOs;

namespace SportLife.Services;

public class PaymentService : IPaymentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<PaymentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<PreferenceResponse> CreatePreference(CreatePreferenceRequest req)
    {
        var accessToken = _config["MercadoPago:AccessToken"];
        var notificationUrl = _config["MercadoPago:NotificationUrl"];
        var successUrl = _config["MercadoPago:SuccessUrl"];
        var failureUrl = _config["MercadoPago:FailureUrl"];
        var pendingUrl = _config["MercadoPago:PendingUrl"];

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

        var response = await client.PostAsync("https://api.mercadopago.com/checkout/preferences", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MercadoPago error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new Exception($"MercadoPago error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;
        var preferenceId = root.GetProperty("id").GetString() ?? "";
        var initPoint = root.GetProperty("init_point").GetString() ?? "";

        return new PreferenceResponse(preferenceId, initPoint);
    }

    public async Task<dynamic?> GetPayment(string paymentId)
    {
        var client = _httpClientFactory.CreateClient("MercadoPago");
        var response = await client.GetAsync($"https://api.mercadopago.com/v1/payments/{paymentId}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MercadoPago GetPayment error: {StatusCode} - {Body}", response.StatusCode, body);
            return null;
        }

        return JsonSerializer.Deserialize<dynamic>(body);
    }
}
