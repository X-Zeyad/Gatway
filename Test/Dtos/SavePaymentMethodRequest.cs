using System.ComponentModel.DataAnnotations;

namespace Test.Dtos
{
    public class SavePaymentMethodRequest
    {
        [Required]
        public string Gateway { get; set; }

        [Required]
        public string CardToken { get; set; }

        public bool SetAsDefault { get; set; }
    }
}
