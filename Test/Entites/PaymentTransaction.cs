namespace Test.Entites
{
    public class PaymentTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Gateway { get; set; }
        public string ExternalTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public TransactionStatus Status { get; set; } // Pending, Success, Failed, Refunded
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public User User { get; set; }
    }
    public enum TransactionStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }
}
