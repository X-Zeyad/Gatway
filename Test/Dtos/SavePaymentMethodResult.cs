using Test.Entites;

namespace Test.Dtos
{
    public class SavePaymentMethodResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }
}
