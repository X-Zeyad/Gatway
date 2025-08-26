using Stripe;
using Test.Services.PaymentServices;

namespace Test.FactoryPattern
{
    public class PaymentGatewayFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PaymentGatewayFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IPaymentGatewayService GetGateway(string gatewayName)
        {
            if (string.IsNullOrWhiteSpace(gatewayName))
                throw new ArgumentNullException(nameof(gatewayName));

            IPaymentGatewayService gateway = null;
            switch (gatewayName.ToLower())
            {
                case "stripe":
                    gateway = _serviceProvider.GetService<IStripeService>();
                    break;
                case "paypal":
                    gateway = _serviceProvider.GetService<IPayPalService>();
                    break;
                //case "tap":
                //    gateway = _serviceProvider.GetService<ITapService>();
                //    break;
                default:
                    throw new NotSupportedException($"Gateway {gatewayName} is not supported");
            }

            return gateway ?? throw new InvalidOperationException($"Service for gateway '{gatewayName}' is not registered.");
        }
    }
}
