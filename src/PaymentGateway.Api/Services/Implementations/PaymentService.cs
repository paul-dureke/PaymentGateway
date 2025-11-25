using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.DTOs;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IAcquiringBankClient _acquiringBank;
        private readonly IPaymentRequestValidator _validator;
        private readonly IPaymentsRepository _paymentsRepository;

        public PaymentService(IAcquiringBankClient acquiringBank, IPaymentRequestValidator validator, IPaymentsRepository paymentsRepository)
        {
            _acquiringBank = acquiringBank;
            _validator = validator;
            _paymentsRepository = paymentsRepository;
        }

        public Task<GetPaymentResponse> GetPaymentAsync(Guid id)
        {
            var savedpayment = _paymentsRepository.Get(id);
            if (savedpayment == null)
                return Task.FromResult<GetPaymentResponse>(null!);

            return Task.FromResult(new GetPaymentResponse
            {
                Id = savedpayment.Id,
                Status = savedpayment.Status,
                CardNumberLastFour = savedpayment.CardNumberLastFour!,
                ExpiryMonth = savedpayment.ExpiryMonth,
                ExpiryYear = savedpayment.ExpiryYear,
                Currency = savedpayment.Currency,
                Amount = savedpayment.Amount
            });
        }

        public async Task<PostPaymentResponse> ProcessPaymentAsync(PostPaymentRequest paymentRequest)
        {
            var validationResult = _validator.Validate(paymentRequest);

            if (!validationResult.IsValid)
            {
                return new PostPaymentResponse
                {
                    Id = Guid.Empty,
                    Status = PaymentStatus.Rejected,
                    CardNumberLastFour = ExtractLastFourDigits(paymentRequest.CardNumber),
                    ExpiryMonth = paymentRequest.ExpiryMonth,
                    ExpiryYear = paymentRequest.ExpiryYear,
                    Amount = paymentRequest.Amount,
                    Currency = paymentRequest.Currency
                };
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                CardNumber = paymentRequest.CardNumber,
                ExpiryMonth = paymentRequest.ExpiryMonth,
                ExpiryYear = paymentRequest.ExpiryYear,
                Amount = paymentRequest.Amount,
                Currency = paymentRequest.Currency,
                Cvv = paymentRequest.Cvv
            };
            BankAuthorizationResponse result;
            try
            {
                result = await _acquiringBank.PaymentAsync(payment);
            }
            catch (BankUnavailableException) { throw; }

            payment.Status = result.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
            payment.AuthorizationCode = result.AuthorizationCode;

            var response = new PostPaymentResponse
            {
                Id = payment.Id,
                Status = payment.Status,
                CardNumberLastFour = ExtractLastFourDigits(payment.CardNumber),
                ExpiryMonth = payment.ExpiryMonth,
                ExpiryYear = payment.ExpiryYear,
                Amount = payment.Amount,
                Currency = payment.Currency
            };

            _paymentsRepository.Add(response);
            return response;
        }

        private string? ExtractLastFourDigits(string? cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            {
                return cardNumber;
            }

            return cardNumber[^4..];
        }
    }
}