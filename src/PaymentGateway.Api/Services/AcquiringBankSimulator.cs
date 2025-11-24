
using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Services
{
    public class AcquiringBankSimulator : IAcquiringBankClient
    {
        public Task<BankAuthorizationResponse> PaymentAsync(Payment payment)
        {
            var cardNumber = payment.CardNumber ?? string.Empty;
            var lastChar = cardNumber[^1];

            if (!char.IsDigit(lastChar))
                throw new InvalidOperationException("Invalid card number.");

            var lastDigit = lastChar - '0';

            if (lastDigit == 0)
            {
                throw new BankUnavailableException("Acquiring bank unavailable for this card.");
            }

            var authorized = lastDigit % 2 == 1; 

            return Task.FromResult(new BankAuthorizationResponse
            {
                Authorized = authorized,
                AuthorizationCode = Guid.NewGuid().ToString("N")
            });
        }
    }

    public class BankUnavailableException : Exception
    {
        public BankUnavailableException(string message) : base(message)
        {
        }
    }
}
