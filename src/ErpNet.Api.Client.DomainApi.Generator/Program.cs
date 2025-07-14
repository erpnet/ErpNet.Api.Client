using System;
using System.Threading.Tasks;

namespace ErpNet.Api.Client.DomainApi.Generator
{
    class Program
    {
        //const string ErpNetDatabaseUri = "https://demodb.my.erp.net";
        const string ErpNetDatabaseUri = "https://testdb.my.erp.net";
        //const string ErpNetDatabaseUri = "https://internal-nstable.my.erp.net";
        //const string ErpNetDatabaseUri = "https://e1-nstable.local";
        //const string ErpNetDatabaseUri = "https://e1-nbeta.local";

        static void Main(string[] args)
        {
            GenerateEntities().Wait();
            Console.WriteLine("Model generated successfully. Press any key to exit.");
            Console.ReadKey();
        }

        static async Task<DomainApiService> CreateServiceAsync(string erpNetDatabaseUri)
        {
            var identityClient = new ErpNetServiceClient(
                erpNetDatabaseUri,
                "PK",
                "pk");

            var apiRoot = await identityClient.GetDomainApiODataServiceRootAsync();

            var service = new DomainApiService(
                 apiRoot,
                 identityClient.GetAccessTokenAsync);
            return service;
        }


        static async Task GenerateEntities()
        {
            var service = await CreateServiceAsync(ErpNetDatabaseUri);
            using var metadataSteam = await service.GetMetadataStreamAsync();

            ClassGeneration.GenerateFile("GeneratedModel.cs", metadataSteam);
        }

    }
}
