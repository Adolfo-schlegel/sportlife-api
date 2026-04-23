using SportLife.DTOs;

namespace SportLife.Services;

public interface IPaymentService
{
    Task<PreferenceResponse> CreatePreference(CreatePreferenceRequest req);
    Task<dynamic?> GetPayment(string paymentId);
    Task<ProcessPaymentResponse> ProcessPayment(ProcessPaymentRequest req);
}
