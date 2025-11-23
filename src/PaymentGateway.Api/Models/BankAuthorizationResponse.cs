namespace PaymentGateway.Api.Models
{
    public class BankAuthorizationResponse
    {
        public bool Authorized { get; set; }
        public Guid AuthorizationCode { get; set; }
    }
}