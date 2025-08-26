namespace Test.Entites
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
        public List<PaymentTransaction> Transactions { get; set; } = new();
    }
}
