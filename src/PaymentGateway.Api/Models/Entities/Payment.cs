namespace PaymentGateway.Api.Models.Entities
{
    public class Payment
    {
        public string CardNumber { get; set; } = string.Empty;
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public string AuthorizationCode { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
    }
}