
dotnet nuget push ErpNet.Api.Client\bin\Debug\ErpNet.Api.Client.1.0.1.nupkg --api-key %1 --source https://api.nuget.org/v3/index.json

dotnet nuget push ErpNet.Api.Client.DomainApi\bin\Debug\ErpNet.Api.Client.DomainApi.1.0.1.nupkg --api-key %1 --source https://api.nuget.org/v3/index.json
