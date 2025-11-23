
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services
{
    public interface IAcquiringBankClient
    {
        Task<BankAuthorizationResponse> PaymentAsync(Payment payment);
    }
}