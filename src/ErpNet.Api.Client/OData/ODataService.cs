using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ErpNet.Api.Client.OData
{
    /// <summary>
    /// Represents a connection to ERP.net ODATA API such as DOMAIN API or TABLE API.
    /// </summary>
    public class ODataService
    {
        /// <summary>
        /// A function used to get the authentication acesss token.
        /// </summary>
        protected Func<Task<string>> getAccessTokenAsync;
        HttpClient client;
        LoggingHttpHandler logger;

        static ODataService()
        {
            // Allow self-signed ssl certificates.
            //ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, ssl) => true;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        /// <summary>
        /// Creates an instance of <see cref="ODataService"/>
        /// </summary>
        /// <param name="odataServiceRootUri">The root address of the ODATA service</param>
        /// <param name="getAccessTokenAsync">User function that provides the access token used to access the API.</param>
        public ODataService(string odataServiceRootUri, Func<Task<string>> getAccessTokenAsync)
        {
            this.getAccessTokenAsync = getAccessTokenAsync;
            var messageHandler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = delegate { return true; },
            };
            messageHandler.Proxy = null;
            messageHandler.UseProxy = false;
            messageHandler.MaxConnectionsPerServer = 10;

            logger = new LoggingHttpHandler(messageHandler);
            client = new HttpClient(messageHandler, true);
            client.BaseAddress = new Uri(odataServiceRootUri);
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.Timeout = TimeSpan.FromMinutes(60);
            ServicePointManager.FindServicePoint(client.BaseAddress).ConnectionLeaseTimeout = 60 * 1000;

        }

        /// <summary>
        /// The odata service root uri.
        /// </summary>
        public string ODataServiceRootUri => client.BaseAddress.ToString();



        /// <summary>
        /// Gets the metadata XML stream.
        /// </summary>
        /// <returns></returns>
        public async Task<Stream> GetMetadataStreamAsync()
        {
            var httpResponse = await GetAsync("$metadata");
            await HandleErrorAsync(httpResponse);

            return await httpResponse.Content.ReadAsStreamAsync();
        }

        private async Task<HttpResponseMessage> ExecuteAsync(ODataCommand command)
        {
            HttpRequestMessage requestMsg = await CreateRequestMessage(command.GetUriString());
            var accessToken = await getAccessTokenAsync();
            requestMsg.Headers.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse($"Bearer {accessToken}");

            switch (command.Type)
            {
                case ErpCommandType.SingleEntity:
                case ErpCommandType.Query:
                case ErpCommandType.Function:
                case ErpCommandType.Count:
                    requestMsg.Method = HttpMethod.Get;

                    break;
                case ErpCommandType.Insert:
                case ErpCommandType.Action:
                    requestMsg.Method = HttpMethod.Post;
                    break;
                case ErpCommandType.Update:
                    requestMsg.Method = new HttpMethod("PATCH");
                    break;
                case ErpCommandType.Delete:
                    requestMsg.Method = HttpMethod.Delete;
                    break;
            }

            if (command.Payload != null)
                requestMsg.Content = new StringContent(command.Payload, Encoding.UTF8, "application/json");

            Debug.WriteLine(requestMsg);

            var responseMsg = await client.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead);

            await HandleErrorAsync(responseMsg);
            return responseMsg;
        }

        /// <summary>
        /// Executes the command and returns the response stream.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<Stream?> ExecuteStreamAsync(ODataCommand command)
        {

            var responseMsg = await ExecuteAsync(command);

            if (responseMsg.StatusCode == HttpStatusCode.NoContent)
                return null;

            var stream = await responseMsg.Content.ReadAsStreamAsync();
            return stream;
        }

        /// <summary>
        /// Executes the command and returns the response as plain string.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<string?> ExecuteStringAsync(ODataCommand command)
        {
            var responseMsg = await ExecuteAsync(command);

            if (responseMsg.StatusCode == HttpStatusCode.NoContent)
                return null;

            var str = await responseMsg.Content.ReadAsStringAsync();
            return str;
        }

        /// <summary>
        /// Executes the command and returns an object result or null.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public async Task<object?> ExecuteObjectAsync(ODataCommand command)
        {
            using var stream = await ExecuteStreamAsync(command);
            return stream?.ReadJsonObject();
        }

        /// <summary>
        /// Executes the command and returns a IDictionary{string,object} result or throws exception.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public async Task<IDictionary<string, object?>> ExecuteDictionaryAsync(ODataCommand command)
        {
            using var stream = await ExecuteStreamAsync(command);
            if (stream != null && stream.ReadJsonObject() is IDictionary<string, object?> dict)
                return dict;
            throw new InvalidOperationException("The odata result is not a valid JSON object.");
        }


        /// <summary>
        /// Handles a http error response.
        /// </summary>
        /// <param name="responseMsg"></param>
        /// <returns></returns>
        protected async Task HandleErrorAsync(HttpResponseMessage responseMsg)
        {
            if (!responseMsg.IsSuccessStatusCode)
            {
                // Show error.
                var stream = await responseMsg.Content.ReadAsStreamAsync();
                if (!stream.TryReadJsonObject(out var obj))
                    responseMsg.EnsureSuccessStatusCode();

                ODataException? ex = null;
                if (obj is IDictionary<string, object?> dict && dict != null)
                {
                    int code = (int)dict.Member<double>("error.code");
                    string message = dict.Member<string>("error.message") ?? responseMsg.ReasonPhrase;
                    string info = dict.Member<string>("error.info") ?? "";
                    string type = dict.Member<string>("error.type") ?? "";
                    ex = new ODataException(type, code, message.Replace("\\r\\n", "\r\n"), info.Replace("\\r\\n", "\r\n"));
                }
                if (ex == null)
                {
                    ex = new ODataException("Unknown", 0, $"Error {responseMsg.StatusCode} - {responseMsg.ReasonPhrase}.", obj?.ToString() ?? "");
                }
                ex.Data["JSON"] = obj;
                ex.Data["Request"] = logger.LastRequest?.ToString();
                throw ex;
            }
        }

       
        /// <summary>
        /// Creates http request message.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected virtual async Task<HttpRequestMessage> CreateRequestMessage(string uri)
        {
            HttpRequestMessage requestMsg = new HttpRequestMessage();
            requestMsg.RequestUri = new Uri(uri, UriKind.Relative);
            var accessToken = await getAccessTokenAsync();
            requestMsg.Headers.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse($"Bearer {accessToken}");
            return requestMsg;
        }
        /// <summary>
        /// Executes HTTP GET.
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        protected async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            var msg = await CreateRequestMessage(requestUri);
            return await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
        }
        /// <summary>
        /// Executes HTTP POST.
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        protected async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            var msg = await CreateRequestMessage(requestUri);
            msg.Method = HttpMethod.Post;
            msg.Content = content;
            return await client.SendAsync(msg);
        }
    }
}
