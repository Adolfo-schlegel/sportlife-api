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
        var dbConfig = await _db.MercadoPagoConfigs.FirstOrDefaultAsync();
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

        var isSandbox = dbConfig?.IsTestMode ?? _config.GetValue<bool>("MercadoPago:Sandbox");
        var initPoint = isSandbox
            ? root.GetProperty("sandbox_init_point").GetString() ?? ""
            : root.GetProperty("init_point").GetString() ?? "";

        return new PreferenceResponse(preferenceId, initPoint);
    }

    public async Task<ProcessPaymentResponse> ProcessPayment(ProcessPaymentRequest req)
    {
        var accessToken = await GetAccessTokenAsync();

        var payload = new
        {
            token = req.Token,
            payment_method_id = req.PaymentMethodId,
            installments = req.Installments,
            issuer_id = req.IssuerId,
            transaction_amount = req.Amount,
            description = req.Description,
            external_reference = req.ExternalReference,
            payer = new
            {
                email = req.Email,
                identification = new
                {
                    type = req.IdentificationType,
                    number = req.IdentificationNumber
                }
            }
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload, options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpClient = _httpClientFactory.CreateClient("MercadoPago");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/v1/payments")
        {
            Content = content
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MP ProcessPayment error: {StatusCode} - {Body}", response.StatusCode, body);
            throw new Exception($"Error procesando pago: {response.StatusCode} - {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var status = root.GetProperty("status").GetString() ?? "unknown";
        var mpPaymentId = root.GetProperty("id").GetInt64().ToString();

        _logger.LogInformation("MP payment processed: id={Id} status={Status}", mpPaymentId, status);
        return new ProcessPaymentResponse(mpPaymentId, status);
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
