using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ErpNet.Api.Client
{
    class LoggingHttpHandler : DelegatingHandler
    {
        HttpRequestMessage? lastRequest;
        public LoggingHttpHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        public bool Enabled { get; set; }

        public HttpRequestMessage? LastRequest => lastRequest;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref lastRequest, request);
            if (!Enabled)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            Console.WriteLine("Request:");
            Console.WriteLine(request.ToString());
            if (request.Content != null)
            {
                Console.WriteLine(await request.Content.ReadAsStringAsync());
            }
            Console.WriteLine();

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            Console.WriteLine("Response:");
            Console.WriteLine(response.ToString());
            if (response.Content != null)
            {
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
            Console.WriteLine();

            return response;
        }
    }
}
