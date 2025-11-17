# ErpNet.Api.Client.DomainApi

**ErpNet.Api.Client.DomainApi** is a .NET library that extends the ErpNet.Api.Client with domain-specific API integrations for ErpNet. It provides convenient, strongly-typed access to domain endpoints, making it easier to work with business objects and processes in ErpNet.

## Features

- Supports .NET Standard 2.1 and .NET 9
- Domain-focused API client for ErpNet (e.g., sales, inventory, finance)
- Strongly-typed models and methods for domain entities
- Asynchronous operations with `async`/`await`
- Designed for use in desktop, web, and service applications

## Installation

Install via [NuGet](https://www.nuget.org/packages/ErpNet.Api.Client.DomainApi):

```bash
dotnet add package ErpNet.Api.Client.DomainApi
```

Or using the NuGet Package Manager:

```bash
Install-Package ErpNet.Api.Client.DomainApi
```

## Usage

### 1. Configure the Domain API Client

```csharp
using ErpNet.Api.Client;
using ErpNet.Api.Client.DomainApi;

var baseClient = new ErpNetApiClient("https://your-erpnet-api-url", "your-api-key");
```

### 2. Call Domain API Methods

```csharp
// Example: Get a list of sales orders
var salesOrders = await client.Sales.Orders.GetAllAsync();
```

### 3. Handle Results

```csharp
// Example: Handle the results of the API call
foreach (var order in salesOrders)
{
    Console.WriteLine($"Order ID: {order.Id}, Total Amount: {order.TotalAmount}");
}
```

## Configuration

- **Base URL**: The root URL of your ErpNet API instance.
- **API Key**: Your authentication token for accessing the API.

## Documentation

- [API Reference](https://docs.erp.net/model)
- [ErpNet Domain API Documentation](https://docs.erp.net/dev/domain-api/index.html)

## Contributing

Contributions are welcome! Please open issues or submit pull requests via [GitHub](https://github.com/erpnet/ErpNet.Api.Client).

## License

This project is licensed under the MIT License. See the [LICENSE](../LICENSE) file for details.

## Support

For questions or support, please open an issue on [GitHub](https://github.com/erpnet/ErpNet.Api.Client/issues).

