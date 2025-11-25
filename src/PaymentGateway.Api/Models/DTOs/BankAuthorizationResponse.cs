namespace PaymentGateway.Api.Models.DTOs
{
    public class BankAuthorizationResponse
    {
        public bool Authorized { get; set; }
        public string AuthorizationCode { get; set; } = string.Empty;
    }
}