using Test.Entites;

namespace Test.Dtos
{
    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public PaymentTransaction Transaction { get; set; }
    }
}
