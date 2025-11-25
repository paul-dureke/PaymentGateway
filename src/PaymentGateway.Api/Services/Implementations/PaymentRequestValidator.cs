using System.Text.RegularExpressions;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Services.Implementations
{
    public class PaymentRequestValidator : IPaymentRequestValidator
    {
        private static readonly HashSet<string> SupportedCurrencies = new()
        {
            "USD",
            "EUR",
            "GBP"
        };

        public PaymentRequestValidationResult Validate(PostPaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CardNumber) ||
                request.CardNumber.Length is < 14 or > 19 ||
                !request.CardNumber.All(char.IsDigit))
            {
                return new PaymentRequestValidationResult(false, "Invalid card number.");
            }
            if (request.ExpiryMonth is < 1 or > 12)
            {
                return new PaymentRequestValidationResult(false, "Invalid expiry month.");
            }
            var today = DateTime.UtcNow;
            var expiryDate = new DateTime(request.ExpiryYear, request.ExpiryMonth, 1).AddMonths(1).AddDays(-1);
            if (expiryDate <= today)
            {
                return new PaymentRequestValidationResult(false, "Card expired.");
            }
            if (string.IsNullOrWhiteSpace(request.Currency) ||
                !SupportedCurrencies.Contains(request.Currency.ToUpper()))
            {
                return new PaymentRequestValidationResult(false, "Unsupported currency.");
            }
            if (request.Amount <= 0)
            {
                return new PaymentRequestValidationResult(false, "Amount must be greater than zero.");
            }
            if (string.IsNullOrWhiteSpace(request.Cvv) || request.Cvv.Length is < 3 or > 4 || 
                !request.Cvv.All(char.IsDigit))
            {
                return new PaymentRequestValidationResult(false, "Invalid CVV.");
            }

            return new PaymentRequestValidationResult(true, null);
        }

    }
}