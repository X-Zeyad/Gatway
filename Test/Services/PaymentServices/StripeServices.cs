using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Forwarding;
using Test.DbContext;
using Test.Dtos;
using Test.Entites;

namespace Test.Services.PaymentServices
{
    public interface IStripeService : IPaymentGatewayService
    {
    }
    public class StripeServices : IStripeService
    {
        private readonly PaymentDbContext _context;
        private readonly IConfiguration _config;

        public StripeServices(PaymentDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }
        public async Task<PaymentResult> ProcessPaymentAsync(CreatePaymentRequest request, int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                // إنشاء أو استرداد العميل في Stripe
                var customerService = new CustomerService();
                var customer = await GetOrCreateStripeCustomer(user, customerService);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100), // Stripe يستخدم cents
                    Currency = request.Currency.ToLower(),
                    Customer = customer.Id,
                    Description = request.Description,
                    PaymentMethod = request.CardToken,
                    ConfirmationMethod = "manual",
                    Confirm = true,
                    ReturnUrl = _config["App:BaseUrl"] + "/payment/return"
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                // حفظ المعاملة في قاعدة البيانات
                var transaction = new PaymentTransaction
                {
                    UserId = userId,
                    Gateway = "Stripe",
                    ExternalTransactionId = paymentIntent.Id,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = MapStripeStatus(paymentIntent.Status),
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(transaction);

                // حفظ طريقة الدفع إذا طُلب ذلك
                if (request.SavePaymentMethod && paymentIntent.PaymentMethod != null)
                {
                    await SaveStripePaymentMethod(paymentIntent.PaymentMethod.Id, userId, customer.Id);
                }

                await _context.SaveChangesAsync();

                return new PaymentResult
                {
                    IsSuccess = paymentIntent.Status == "succeeded",
                    TransactionId = paymentIntent.Id,
                    Status = paymentIntent.Status,
                    Transaction = transaction
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }


        }

        public async Task<PaymentResult> ProcessSavedPaymentAsync(int savedPaymentMethodId, decimal amount, string currency, int userId)
        {
            var savedMethod = await _context.PaymentMethods
             .FirstOrDefaultAsync(pm => pm.Id == savedPaymentMethodId && pm.UserId == userId);

            if (savedMethod == null)
                return new PaymentResult { IsSuccess = false, ErrorMessage = "Payment method not found" };

            var request = new CreatePaymentRequest
            {
                Amount = amount,
                Currency = currency,
                Gateway = "stripe",
                CardToken = savedMethod.TokenId,
                SavePaymentMethod = false
            };

            return await ProcessPaymentAsync(request, userId);
        }

        public async Task<bool> RefundPaymentAsync(string transactionId, decimal? amount = null)
        {
            try
            {
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = transactionId
                };

                if (amount.HasValue)
                    refundOptions.Amount = (long)(amount.Value * 100);

                var refundService = new RefundService();
                var refund = await refundService.CreateAsync(refundOptions);

                return refund.Status == "succeeded";
            }
            catch
            {
                return false;
            }
        }

        public async Task<SavePaymentMethodResult> SavePaymentMethodAsync(SavePaymentMethodRequest request, int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                var customerService = new CustomerService();
                var customer = await GetOrCreateStripeCustomer(user, customerService);

                // ربط طريقة الدفع بالعميل
                var attachOptions = new PaymentMethodAttachOptions
                {
                    Customer = customer.Id,
                };

                var paymentMethodService = new PaymentMethodService();
                var paymentMethod = await paymentMethodService.AttachAsync(request.CardToken, attachOptions);

                // حفظ في قاعدة البيانات
                var savedPaymentMethod = await SaveStripePaymentMethod(paymentMethod.Id, userId, customer.Id);

                if (request.SetAsDefault)
                {
                    await SetDefaultPaymentMethod(userId, savedPaymentMethod.Id);
                }

                return new SavePaymentMethodResult
                {
                    IsSuccess = true,
                    PaymentMethod = savedPaymentMethod
                };
            }
            catch (StripeException ex)
            {
                return new SavePaymentMethodResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        private async Task<Customer> GetOrCreateStripeCustomer(User user, CustomerService customerService)
        {
            var existingPaymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.UserId == user.Id && pm.Gateway == "Stripe");

            if (existingPaymentMethod != null)
            {
                return await customerService.GetAsync(existingPaymentMethod.ExternalId);
            }

            var customerOptions = new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.Name,
            };

            return await customerService.CreateAsync(customerOptions);
        }

        private async Task<Entites.PaymentMethod> SaveStripePaymentMethod(string paymentMethodId, int userId, string customerId)
        {
            var paymentMethodService = new PaymentMethodService();
            var stripePaymentMethod = await paymentMethodService.GetAsync(paymentMethodId);

            var savedMethod = new Entites.PaymentMethod
            {
                UserId = userId,
                Gateway = "Stripe",
                ExternalId = customerId,
                TokenId = paymentMethodId,
                LastFourDigits = stripePaymentMethod.Card.Last4,
                CardBrand = stripePaymentMethod.Card.Brand,
                ExpiryMonth = (int)stripePaymentMethod.Card.ExpMonth,
                ExpiryYear = (int)stripePaymentMethod.Card.ExpYear,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(savedMethod);
            await _context.SaveChangesAsync();

            return savedMethod;
        }

        private async Task SetDefaultPaymentMethod(int userId, int paymentMethodId)
        {
            var userMethods = await _context.PaymentMethods
                .Where(pm => pm.UserId == userId)
                .ToListAsync();

            foreach (var method in userMethods)
            {
                method.IsDefault = method.Id == paymentMethodId;
            }

            await _context.SaveChangesAsync();
        }

        private TransactionStatus MapStripeStatus(string stripeStatus) => stripeStatus switch
        {
            "succeeded" => TransactionStatus.Success,
            "processing" => TransactionStatus.Pending,
            "requires_action" => TransactionStatus.Pending,
            "canceled" => TransactionStatus.Failed,
            _ => TransactionStatus.Pending
        };
    }
}
