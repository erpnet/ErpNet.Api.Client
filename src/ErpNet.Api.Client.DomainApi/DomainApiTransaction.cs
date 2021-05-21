using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Represents a Domain API transaction.
    /// </summary>
    public class DomainApiTransaction: DomainApiService
    {
        /// <summary>
        /// Creates an instance of <see cref="DomainApiTransaction"/>
        /// </summary>
        /// <param name="odataServiceRootUri">the odata service root</param>
        /// <param name="getAccessTokenAsync">a function that returns the authentication access token</param>
        /// <param name="transactionId">the transaction identifier</param>
        /// <param name="model">domain model - 'common' or 'front-end'</param>
        /// <param name="trackChanges">specifies whether to enable track chanegs or not</param>
        public DomainApiTransaction(
            string odataServiceRootUri,
            Func<Task<string>> getAccessTokenAsync,
            string transactionId,
            DomainModel model,
            bool trackChanges)
            : base(odataServiceRootUri, getAccessTokenAsync)
        {
            TransactionId = transactionId;
            Model = model;
            TrackChanges = trackChanges;
        }

        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        public string TransactionId { get; }

        /// <summary>
        /// Indicates the domain model used for this transaction - common or front-end
        /// </summary>
        public DomainModel Model { get; }

        /// <summary>
        /// Indicates whether track changes is enabled for this transaction.
        /// </summary>
        public bool TrackChanges { get; }

        ///<inheritdoc/>
        protected override async Task<HttpRequestMessage> CreateRequestMessage(string uri)
        {
            var msg = await base.CreateRequestMessage(uri);
            msg.Headers.Add("TransactionId", TransactionId);
            return msg;
        }

        /// <summary>
        /// Ends the API transaction.
        /// </summary>
        /// <param name="commit">if set to <c>true</c> [commit].</param>
        /// <returns></returns>
        public async Task EndTransactionAsync(bool commit = true)
        {
            var payload = new Dictionary<string, object?>
            {
                ["commit"] = commit
            };
            var responseMsg = await PostAsync(
               $"EndTransaction?tr={TransactionId}",
               new StringContent(payload.ToJson(), Encoding.UTF8, "application/json"));


            await HandleErrorAsync(responseMsg);
        }

        /// <summary>
        /// Executes the /GetChanges action which returns the changes sinse the last call to /GetChanges.
        /// </summary>
        public async Task<GetChangesResult> GetChangesAsync()
        {
            var httpResponse = await GetAsync($"GetChanges?tr={TransactionId}");
            var stream = await httpResponse.Content.ReadAsStreamAsync();

            await HandleErrorAsync(httpResponse);

            var jsonResponse = stream.ReadJsonObject();

            if (jsonResponse is IDictionary<string, object?> dict && dict != null)
                return new GetChangesResult(dict);

            throw new InvalidOperationException($"Unexpected GetChanges result: " + jsonResponse);
        }
    }

    /// <summary>
    /// Represents a domain model. Different domain models use different sets of business rules.
    /// </summary>
    public enum DomainModel
    {
        /// <summary>
        /// The common model. Front-end business rules are not executed.
        /// </summary>
        Common,
        /// <summary>
        /// The front-end model. Front-end business rules are executed on data attribute change.
        /// </summary>
        FrontEnd
    }
}
