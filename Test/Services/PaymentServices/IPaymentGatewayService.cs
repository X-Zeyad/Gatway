using Test.Dtos;

namespace Test.Services.PaymentServices
{
    public interface IPaymentGatewayService
    {
        Task<PaymentResult> ProcessPaymentAsync(CreatePaymentRequest request, int userId);
        Task<PaymentResult> ProcessSavedPaymentAsync(int savedPaymentMethodId, decimal amount, string currency, int userId);
        Task<SavePaymentMethodResult> SavePaymentMethodAsync(SavePaymentMethodRequest request, int userId);
        Task<bool> RefundPaymentAsync(string transactionId, decimal? amount = null);
    }
}
