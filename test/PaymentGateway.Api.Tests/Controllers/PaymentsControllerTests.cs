using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.DTOs;
using PaymentGateway.Api.Models.Entities;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Services.Implementations;
using PaymentGateway.Api.Services.Interfaces;

namespace PaymentGateway.Api.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Random _random = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private HttpClient CreateClient(IPaymentsRepository? repository = null)
    {
        repository ??= new PaymentsRepository();
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        return webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ConfigureServices(services, repository)))
            .CreateClient();
    }

    private void ConfigureServices(IServiceCollection services, IPaymentsRepository repository)
    {
        services.AddSingleton(repository);
        services.AddSingleton<IPaymentRequestValidator, PaymentRequestValidator>();

        // Remove all existing IAcquiringBankClient registrations (including typed clients)
        var descriptorsToRemove = services.Where(d =>
            d.ServiceType == typeof(IAcquiringBankClient) ||
            d.ServiceType.Name.Contains("HttpClient") ||
            d.ServiceType.Name.Contains("AcquiringBankClient") ||
            d.ImplementationType == typeof(AcquiringBankClient)).ToList();

        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }

        var mockBankClient = new Mock<IAcquiringBankClient>();
        mockBankClient
            .Setup(x => x.PaymentAsync(It.IsAny<Payment>()))
            .Returns<Payment>(payment =>
            {
                var lastDigit = int.Parse(payment.CardNumber[^1].ToString());
                if (lastDigit == 0)
                {
                    return Task.FromException<BankAuthorizationResponse>(
                        new BankUnavailableException("Acquiring bank unavailable for this card."));
                }
                var authorized = lastDigit % 2 == 1;
                return Task.FromResult(new BankAuthorizationResponse
                {
                    Authorized = authorized,
                    AuthorizationCode = Guid.NewGuid().ToString("N")
                });
            });

        services.AddSingleton(mockBankClient.Object);
        services.AddSingleton<IPaymentService, PaymentService>();
    }

    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP",
            Status = PaymentStatus.Authorized
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var client = CreateClient(paymentsRepository);

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_WhenRequiredFieldsAreMissing_Returns400()
    {
        // Arrange
        var client = CreateClient();
        
        var invalidRequest = new
        {
            Amount = 100,
            Currency = "GBP"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", invalidRequest);
        var errorContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(errorContent));
    }

    [Theory]
    [InlineData("12345678901234567")]
    [InlineData("12345678901234569")]
    public async Task CreatePayment_WhenCardEndsWithOddNumber_Returns200Authorized(string cardNumber)
    {
        // Arrange
        var client = CreateClient();
        
        var request = new
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Authorized, paymentResponse!.Status);
        Assert.NotNull(paymentResponse);
        Assert.Equal(cardNumber[^4..], paymentResponse.CardNumberLastFour);
    }

    [Theory]
    [InlineData("12345678901234564")]
    [InlineData("12345678901234568")]
    public async Task CreatePayment_WhenCardEndsWithEvenNumber_Returns200Unauthorized(string cardNumber)
    {
        // Arrange
        var client = CreateClient();

        var request = new
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Declined, paymentResponse!.Status);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task CreatePayment_WhenCardEndsWithZero_Returns500ServiceUnavailable()
    {
        // Arrange
        var client = CreateClient();

        var request = new
        {
            CardNumber = "12345678901234560",
            ExpiryMonth = 12,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var errorContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(errorContent));
    }

    [Fact]
    public async Task CreatePayment_ThenRetrievePayment_EndToEnd()
    {
        // Arrange
        var client = CreateClient();

        var createRequest = new
        {
            CardNumber = "12345678901234567",
            ExpiryMonth = 12,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        // Act - Create Payment
        var createResponse = await client.PostAsJsonAsync("/api/Payments", createRequest);
        var createPaymentResponse = await createResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);

        // Assert - Verify Creation
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.NotNull(createPaymentResponse);
        Assert.NotEqual(Guid.Empty, createPaymentResponse.Id);
        Assert.Equal(PaymentStatus.Authorized, createPaymentResponse.Status);
        Assert.Equal("4567", createPaymentResponse.CardNumberLastFour);
        Assert.Equal("GBP", createPaymentResponse.Currency);
        Assert.Equal(100, createPaymentResponse.Amount);

        var paymentId = createPaymentResponse.Id;

        // Act - Retrieve Payment
        var getResponse = await client.GetAsync($"/api/Payments/{paymentId}");
        var getPaymentResponse = await getResponse.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions);

        // Assert - Verify Retrieval
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(getPaymentResponse);
        Assert.Equal(paymentId, getPaymentResponse.Id);
        Assert.Equal(PaymentStatus.Authorized, getPaymentResponse.Status);
        Assert.Equal("4567", getPaymentResponse.CardNumberLastFour);
        Assert.Equal("GBP", getPaymentResponse.Currency);
        Assert.Equal(100, getPaymentResponse.Amount);
        Assert.Equal(12, getPaymentResponse.ExpiryMonth);
        Assert.Equal(2027, getPaymentResponse.ExpiryYear);
    }
}