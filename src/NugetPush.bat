
dotnet nuget push ErpNet.Api.Client\bin\Release\ErpNet.Api.Client.27.1.0-beta.nupkg --api-key %1 --source https://api.nuget.org/v3/index.json

dotnet nuget push ErpNet.Api.Client.DomainApi\bin\Release\ErpNet.Api.Client.DomainApi.27.1.0-beta.nupkg --api-key %1 --source https://api.nuget.org/v3/index.json
