using Microsoft.AspNetCore.Mvc.RazorPages;
using PayPal.Api;
using Stripe.V2;
using Test.DbContext;
using Test.Dtos;
using Test.Entites;

namespace Test.Services.PaymentServices
{
    public interface IPayPalService : IPaymentGatewayService
    {
    }
    public class PayPalService : IPayPalService
    {
        private readonly PaymentDbContext _context;
        private readonly IConfiguration _config;

        public PayPalService(PaymentDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        public async Task<PaymentResult> ProcessPaymentAsync(CreatePaymentRequest request, int userId)
        {
            try
            {
                var config = GetPayPalConfig();
                var payment = new Payment
                {
                    intent = "sale",
                    payer = new Payer { payment_method = "credit_card" },
                    transactions = new List<Transaction>
                    {
                        new Transaction
                        {
                            amount = new PayPal.Api.Amount
                            {
                                currency = request.Currency,
                                total = request.Amount.ToString("F2")
                            },
                            description = request.Description
                        }
                    }
                };

                var createdPayment = payment.Create(config);

                var transaction = new PaymentTransaction
                {
                    UserId = userId,
                    Gateway = "PayPal",
                    ExternalTransactionId = createdPayment.id,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = MapPayPalStatus(createdPayment.state),
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                return new PaymentResult
                {
                    IsSuccess = createdPayment.state == "approved",
                    TransactionId = createdPayment.id,
                    Status = createdPayment.state,
                    Transaction = transaction
                };
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<PaymentResult> ProcessSavedPaymentAsync(int savedPaymentMethodId, decimal amount, string currency, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RefundPaymentAsync(string transactionId, decimal? amount = null)
        {
            throw new NotImplementedException();
        }

        public Task<SavePaymentMethodResult> SavePaymentMethodAsync(SavePaymentMethodRequest request, int userId)
        {
            throw new NotImplementedException();
        }
        private APIContext GetPayPalConfig()
        {
            var config = new Dictionary<string, string>
            {
                { "mode", _config["PayPal:Mode"] },
                { "clientId", _config["PayPal:ClientId"] },
                { "clientSecret", _config["PayPal:ClientSecret"] }
            };

            var accessToken = new OAuthTokenCredential(
                _config["PayPal:ClientId"],
                _config["PayPal:ClientSecret"],
                config
            ).GetAccessToken();

            return new APIContext(accessToken) { Config = config };
        }
        private TransactionStatus MapPayPalStatus(string paypalStatus) => paypalStatus switch
        {
            "approved" => TransactionStatus.Success,
            "created" => TransactionStatus.Pending,
            "failed" => TransactionStatus.Failed,
            _ => TransactionStatus.Pending
        };
    }
}
