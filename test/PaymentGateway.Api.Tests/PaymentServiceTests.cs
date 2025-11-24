using Moq;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests
{
    public class PaymentServiceTests
    {
        [Fact]
        public async Task ProcessPayment_WhenBankAuthorizes_ReturnsAuthorized()
        {
            // Arrange
            var acquiringBankMock = new Mock<IAcquiringBankClient>();
            var validatorMock = new Mock<IPaymentRequestValidator>();
            var repositoryMock = new Mock<IPaymentsRepository>();

            var paymentService = new PaymentService(acquiringBankMock.Object, validatorMock.Object, repositoryMock.Object);

            var paymentRequest = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = 2027,
                Currency = "GBP",
                Amount = 100,
                Cvv = "123"
            };

            acquiringBankMock
                .Setup(x => x.PaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(new BankAuthorizationResponse
                {
                    Authorized = true,
                    AuthorizationCode = Guid.NewGuid().ToString(),
                });
            validatorMock
                .Setup(x => x.Validate(paymentRequest))
                .Returns(new PaymentRequestValidationResult(true, null));

            // Act
            var result = await paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.Equal(PaymentStatus.Authorized, result.Status);
            Assert.Equal("3456", result.CardNumberLastFour);
            Assert.Equal(paymentRequest.Amount, result.Amount);
            Assert.Equal(paymentRequest.Currency, result.Currency);
            Assert.NotEqual(Guid.Empty, result.Id);

            acquiringBankMock.Verify(x => x.PaymentAsync(It.IsAny<Payment>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPayment_WhenBankDeclines_ReturnsDeclined()
        {
            // Arrange
            var acquiringBankMock = new Mock<IAcquiringBankClient>();
            var validatorMock = new Mock<IPaymentRequestValidator>();
            var repositoryMock = new Mock<IPaymentsRepository>();

            var paymentService = new PaymentService(acquiringBankMock.Object, validatorMock.Object, repositoryMock.Object);
            var paymentRequest = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 11,
                ExpiryYear = 2026,
                Currency = "GBP",
                Amount = 200,
                Cvv = "456"
            };
            acquiringBankMock
                .Setup(x => x.PaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(new BankAuthorizationResponse
                {
                    Authorized = false,
                    AuthorizationCode = string.Empty,
                });
            validatorMock
                .Setup(x => x.Validate(paymentRequest))
                .Returns(new PaymentRequestValidationResult(true, null));

            // Act
            var result = await paymentService.ProcessPaymentAsync(paymentRequest);

            // Assert
            Assert.Equal(PaymentStatus.Declined, result.Status);
            Assert.Equal("3456", result.CardNumberLastFour);
            Assert.Equal(paymentRequest.Amount, result.Amount);
            Assert.Equal(paymentRequest.Currency, result.Currency);
            Assert.NotEqual(Guid.Empty, result.Id);
            acquiringBankMock.Verify(x => x.PaymentAsync(It.IsAny<Payment>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPayment_WhenRequestIsInvalid_ReturnsRejected()
        {
            // Arrange
            var acquiringBankMock = new Mock<IAcquiringBankClient>();
            var validatorMock = new Mock<IPaymentRequestValidator>();
            var repositoryMock = new Mock<IPaymentsRepository>();

            var paymentService = new PaymentService(acquiringBankMock.Object, validatorMock.Object, repositoryMock.Object);

            var invalidRequest = new PostPaymentRequest
            {
                Currency = "GBP"
            };

            validatorMock
                .Setup(x => x.Validate(invalidRequest))
                .Returns(new PaymentRequestValidationResult(false, "Invalid payment request"));

            // Act 
            var result = await paymentService.ProcessPaymentAsync(invalidRequest);

            // Assert
            Assert.Equal(PaymentStatus.Rejected, result.Status);

            acquiringBankMock.Verify(x => x.PaymentAsync(It.IsAny<Payment>()), Times.Never);
        }
    }
}
