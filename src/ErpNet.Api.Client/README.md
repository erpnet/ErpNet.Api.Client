# ErpNet.Api.Client

**ErpNet.Api.Client** is a .NET library designed to simplify integration with the ErpNet API. It provides a strongly-typed, easy-to-use client for consuming ErpNet services from .NET applications.

## Features

- Supports .NET Standard 2.1 and .NET 9
- Strongly-typed API client for ErpNet endpoints
- Asynchronous operations with `async`/`await`
- Simple configuration and extensibility
- Designed for integration in desktop, web, and service applications

## Installation

Install via [NuGet](https://www.nuget.org/packages/ErpNet.Api.Client):

```bash
dotnet add package ErpNet.Api.Client
```

Or using the NuGet Package Manager:

```bash
Install-Package ErpNet.Api.Client
```

## Usage

### 1. Configure the Client

```csharp
using ErpNet.Api.Client;
var client = new ErpNetApiClient("https://your-erpnet-api-url", "your-api-key");
```

### 2. Make API Calls

```csharp
// Example: Get a list of customers
var customers = await client.Customers.GetAllAsync();
```

### 3. Handle Results

```csharp
foreach (var customer in customers)
{
    Console.WriteLine($"Customer ID: {customer.Id}, Name: {customer.Name}");
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

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Support

For questions or support, please open an issue on [GitHub](https://github.com/erpnet/ErpNet.Api.Client/issues).
