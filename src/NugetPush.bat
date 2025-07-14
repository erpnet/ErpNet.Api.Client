
dotnet nuget push ErpNet.Api.Client\bin\Release\ErpNet.Api.Client.26.1.0-beta.nupkg --api-key %1 --source https://api.nuget.org/v3/index.json

dotnet nuget push ErpNet.Api.Client.DomainApi\bin\Release\ErpNet.Api.Client.DomainApi.26.1.0-beta.nupkg --api-key %1 --source https://api.nuget.org/v3/index.json
