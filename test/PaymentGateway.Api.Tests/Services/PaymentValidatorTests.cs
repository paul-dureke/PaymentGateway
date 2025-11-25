using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services.Implementations;

namespace PaymentGateway.Api.Tests.Services
{
    public class PaymentValidatorTests
    {
        private readonly PaymentRequestValidator _validator = new();
        [Fact]
        public void Validate_WithValidRequest_ReturnsValid()
        {
            // Arrange
            var validRequest = CreateValidRequest();

            // Act
            var result = _validator.Validate(validRequest);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1234567890123")]           //< 14
        [InlineData("12345678901234567890")]    //> 19
        [InlineData("1234567890123456-7890")]
        public void Validate_WithInvalidCardNumber_ReturnsInvalid(string cardNumber)
        {
            // Arrange
            var invalidRequest = CreateValidRequest();
            invalidRequest.CardNumber = cardNumber;

            // Act
            var result = _validator.Validate(invalidRequest);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void Validate_WithInvalidExpiryMonth_ReturnsInvalid(int expiryMonth)
        {
            // Arrange
            var invalidRequest = CreateValidRequest();
            invalidRequest.ExpiryMonth = expiryMonth;
            // Act
            var result = _validator.Validate(invalidRequest);
            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void Validate_WithInvalidExpiryYear_ReturnsInvalid()
        {
            // Arrange
            var invalidRequest = CreateValidRequest();
            invalidRequest.ExpiryMonth = 10;
            invalidRequest.ExpiryYear = DateTime.UtcNow.Year - 1;
            // Act
            var result = _validator.Validate(invalidRequest);
            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("GB")]
        [InlineData("Pounds")]
        [InlineData("NGN")]
        public void Validate_WithInvalidCurrency_ReturnsInvalid(string currency)
        {
            // Arrange
            var invalidRequest = CreateValidRequest();
            invalidRequest.Currency = currency;
            // Act
            var result = _validator.Validate(invalidRequest);
            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Validate_WithInvalidAmount_ReturnsInvalid(int amount)
        {
            // Arrange
            var invalidRequest = CreateValidRequest();
            invalidRequest.Amount = amount;
            // Act
            var result = _validator.Validate(invalidRequest);
            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("12")]
        [InlineData("12345")]
        [InlineData("12x")]
        public void Validate_WithInvalidCvv_ReturnsInvalid(string cvv)
        {
            // Arrange
            var invalidRequest = CreateValidRequest();
            invalidRequest.Cvv = cvv;
            // Act
            var result = _validator.Validate(invalidRequest);
            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        private static PostPaymentRequest CreateValidRequest()
        {
            return new ()
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };
        }
    }
}
