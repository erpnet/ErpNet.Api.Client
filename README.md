# Welcome to ErpNet.Api.Client Project

A Dot Net client for ERP.net APIs

## Introduction
ErpNet.Api.Client allows you to build external applications for ERP.net platform. Currently there are two types of API - Table API and Domain API. Both APIs are build on top of [ODATA](http://odata.org) standard. The project consists of two libraries: ErpNet.Api.Client and ErpNet.Api.Client.DomainApi. 

Nuget Packages:
https://www.nuget.org/packages/ErpNet.Api.Client
https://www.nuget.org/packages/ErpNet.Api.Client.DomainApi

ERP.net Developer Documentation: 
https://docs.erp.net/dev/

## ErpNet.Api.Client
This library provides a generic ODATA service and ODATA command for building API HTTP requests. The JSON result is paresed to IDictionary<string, object> containting the entity properties.

```csharp
 var erpNetDatabaseUri = "https://demodb.my.erp.net";
 // Create ErpNetServiceClient object to obtain a service access token. 
 // In the database there must be trusted application registration with ApplicationUri: "ServiceDemoClient" and ApplcationSecretHash=Sha256("DEMO").
 ErpNetServiceClient identityClient = new ErpNetServiceClient(
                erpNetDatabaseUri,
                "ServiceDemoClient",
                "DEMO");

 // Obtain the web address of the DomainApi for the database.
 var apiRoot = await identityClient.GetDomainApiODataServiceRootAsync();

 // Create the service
 DomainApiService service = new DomainApiService(
                apiRoot,
                identityClient.GetAccessTokenAsync);
                
 // Create query command with $top, $filter, $select and $expand clauses.               
 ODataCommand command = new ODataCommand("General_Products_Products");
 command.Type = ErpCommandType.Query;
 command.FilterClause = "contains(PartNumber,'001')";
 command.SelectClause = "Id,PartNumber,Name,ProductGroup";
 command.ExpandClause = "ProductGroup($select=Id,Code,Name)";
 command.TopClause = 5;   
 
 // Get the ODATA json result as IDictionary<string,object>
 var result = await service.ExecuteDictionaryAsync(command);
```

## ErpNet.Api.Client.DomainApi
This library provides typed entity objects for Domain API and type safe methods for building API requests. 

```csharp

// Use anonymous types for $select and $expand clause
var cmd = service.Command<Product>()
    .Top(5)
    .Filter(p => p.PartNumber.Contains("001"))
    .Select(p => new 
    {
        p.Id, 
        p.PartNumber, 
        p.Name, 
        ProductGroup = new 
        { 
            p.ProductGroup.Id, 
            p.ProductGroup.Code, 
            p.ProductGroup.Name
        }});

var result = await cmd.LoadAsync();

// The HTTP command is 
// GET General_Products_Products?$top=5&$filter=contains(PartNumber,'001')&$select=Id,PartNumber,Name,ProductGroup&$expand=ProductGroup($select=Id,Code,Name)
```

For more samples see https://github.com/ErpNetDocs/dev/tree/master/domain-api/samples/src/dotnet/ErpNet.Api.Client.Samples
