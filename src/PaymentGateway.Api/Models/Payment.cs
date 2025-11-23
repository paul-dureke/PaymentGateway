namespace PaymentGateway.Api.Models
{
    public class Payment
    {
        public int CardNumber { get; internal set; }
        public int ExpiryMonth { get; internal set; }
        public int ExpiryYear { get; internal set; }
        public int Amount { get; internal set; }
        public string Currency { get; internal set; } = string.Empty;
        public int Cvv { get; internal set; }
        public Guid Id { get; internal set; }
        public Guid AuthorizationCode { get; internal set; }
        public PaymentStatus Status { get; internal set; }
    }
}