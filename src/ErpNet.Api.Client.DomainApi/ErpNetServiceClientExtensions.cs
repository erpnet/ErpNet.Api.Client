using System;
using System.Linq;
using System.Threading.Tasks;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Defines extension methods for <see cref="ErpNetServiceClient" />
    /// </summary>
    public static class ErpNetServiceClientExtensions
    {
        /// <summary>
        /// Returns the first found Domain Api service root url for the database of the <see cref="ErpNetServiceClient"/>
        /// </summary>
        /// <param name="client">The service client</param>
        /// <returns></returns>
        public static async Task<string> GetDomainApiODataServiceRootAsync(this ErpNetServiceClient client)
        {
            var disco = await client.GetAutoDiscoveryAsync()
                ?? throw new InvalidOperationException("API site not running!");

            var apiSite = disco.WebSites?.FirstOrDefault(s => s.Type == WebsiteType.DomainAPI && s.Status == WebsiteStatus.Working)
                ?? throw new InvalidOperationException("API site not running!");
            var apiRoot = apiSite.AdditionalProperties?["ODataServiceRoot"] 
                ?? (apiSite.Url?.TrimEnd('/') + "/domain/odata/");

            return apiRoot;
        }
    }
}
