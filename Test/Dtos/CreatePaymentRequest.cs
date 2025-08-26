using System.ComponentModel.DataAnnotations;

namespace Test.Dtos
{
    public class CreatePaymentRequest
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "USD";

        [Required]
        public string Gateway { get; set; } // "stripe", "paypal", "tap"

        public string Description { get; set; }

        public bool SavePaymentMethod { get; set; }

        // للدفع بطريقة محفوظة
        public int? SavedPaymentMethodId { get; set; }

        // لبطاقة جديدة
        public string CardToken { get; set; }
    }
}
