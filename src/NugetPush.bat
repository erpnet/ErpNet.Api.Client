
dotnet nuget push ErpNet.Api.Client\bin\Debug\ErpNet.Api.Client.23.1.0-beta.nupkg --api-key %1 --source https://api.nuget.org/v3/index.json

dotnet nuget push ErpNet.Api.Client.DomainApi\bin\Debug\ErpNet.Api.Client.DomainApi.23.1.0-beta.nupkg --api-key %1 --source https://api.nuget.org/v3/index.json
