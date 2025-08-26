namespace Test.Entites
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Gateway { get; set; } // Stripe, PayPal, etc.
        public string ExternalId { get; set; } // Customer ID من البوابة
        public string TokenId { get; set; } // Payment Method Token
        public string LastFourDigits { get; set; }
        public string CardBrand { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
    }
}
