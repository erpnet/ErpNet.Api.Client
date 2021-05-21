using ErpNet.Api.Client.OData;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Represents a ERP.net DomainApi service client.
    /// </summary>
    public class DomainApiService : ODataService
    {
        /// <summary>
        /// Creates an instance of <see cref="DomainApiService"/>
        /// </summary>
        /// <param name="odataServiceRootUri"></param>
        /// <param name="getAccessTokenAsync"></param>
        public DomainApiService(string odataServiceRootUri, 
            Func<Task<string>> getAccessTokenAsync) 
            : base(odataServiceRootUri, getAccessTokenAsync)
        {
        }

        /// <summary>
        /// Opens an API transaction on the server side.
        /// </summary>
        /// <returns></returns>
        public async Task<DomainApiTransaction> BeginTransactionAsync(DomainModel model = DomainModel.Common, bool trackChanges = false)
        {
            var payload = new Dictionary<string, object?>
            {
                ["model"] = model.ToString().ToLowerInvariant(),
                ["trackChanges"] = trackChanges
            };
            var httpResponse = await PostAsync(
                "BeginTransaction",
                new StringContent(payload.ToJson(), Encoding.UTF8, "application/json"));

            await HandleErrorAsync(httpResponse);

            var transactionId = await httpResponse.Content.ReadAsStringAsync();
            return new DomainApiTransaction(this.ODataServiceRootUri, getAccessTokenAsync, transactionId, model, trackChanges);
        }

        /// <summary>
        /// Refreshes the Entity Data Model of the Domain Api. If custom property is created this method must be called to appear the custom property in the api. 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ResetModelAsync()
        {
            var httpResponse = await GetAsync("Reset");
            var stream = await httpResponse.Content.ReadAsStreamAsync();

            await HandleErrorAsync(httpResponse);

            var jsonResponse = stream.ReadJsonObject();

            if (jsonResponse is IDictionary<string, object?> dict && dict.TryGetValue("success", out var value))
                return true.Equals(value);

            throw new InvalidOperationException($"Unsuccessful Domain Api reset: " + jsonResponse);
        }
    }
}
