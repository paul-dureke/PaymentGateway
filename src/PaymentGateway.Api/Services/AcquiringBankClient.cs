
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services
{
    public class AcquiringBankClient : IAcquiringBankClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public AcquiringBankClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
        public async Task<BankAuthorizationResponse> PaymentAsync(Payment payment)
        {
            var bankUrl = _configuration["AcquiringBank:Url"];

            var requestBody = new BankAuthorizationRequest
            {
                CardNumber = payment.CardNumber,
                ExpiryDate = $"{payment.ExpiryMonth:D2}/{payment.ExpiryYear}",
                Cvv = payment.Cvv,
                Amount = payment.Amount,
                Currency = payment.Currency
            };

            var response = await _httpClient.PostAsJsonAsync($"{bankUrl}/payments", requestBody);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                throw new BankUnavailableException("Bank is unavailable.");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<BankAuthorizationResponse>()
                   ?? throw new InvalidOperationException("Invalid response from acquiring bank");
        }
    }
}
