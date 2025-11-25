# Payment Gateway API

This project implements a simple Payment Gateway API in .NET, designed to process and retrieve payment transactions. It includes a mock integration with an acquiring bank simulator for authorization.

## Solution Structure

```
src/
  PaymentGateway.Api         - ASP.NET Core Web API for payment processing
test/
  PaymentGateway.Api.Tests   - xUnit test project for API and service logic
imposters/
  bank_simulator.ejs         - Mountebank configuration for the acquiring bank simulator

docker-compose.yml           - Runs the bank simulator locally
.editorconfig                - Code style and formatting rules
PaymentGateway.sln           - Visual Studio solution file
```

## Features

- **Process Payments:** Accepts card payments, validates input, and interacts with the acquiring bank.
- **Retrieve Payments:** Fetches payment details by ID, masking sensitive card data.
- **Bank Simulator:** Uses Mountebank to simulate bank authorization responses for testing.
- **Validation:** Ensures all payment requests are well-formed and valid.
- **Comprehensive Tests:** Includes unit and integration tests for controllers and services.

## Getting Started

1. **Run the Bank Simulator:**  
   ```sh
   docker-compose up
   ```
2. **Run the API:**  
   Use Visual Studio or `dotnet run` in `src/PaymentGateway.Api`.
3. **API Documentation:**  
   Swagger UI is available at `/swagger` when running in Development mode.

## Testing

Run all tests with:
```sh
dotnet test
```

---

**Note:**  
- The bank simulator must be running for the API to process payments.
- Do not modify `.editorconfig` or `imposters/bank_simulator.ejs`.
