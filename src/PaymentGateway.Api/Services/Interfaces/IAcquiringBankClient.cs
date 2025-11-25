using PaymentGateway.Api.Models.DTOs;
using PaymentGateway.Api.Models.Entities;

namespace PaymentGateway.Api.Services.Interfaces
{
    public interface IAcquiringBankClient
    {
        Task<BankAuthorizationResponse> PaymentAsync(Payment payment);
    }
}