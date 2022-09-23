using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Unblocker
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(CustomHttpProxy.Register);
        }
    }

    public static class CustomHttpProxy
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "Proxy",
                routeTemplate: "{*path}",
                handler: HttpClientFactory.CreatePipeline(
                    innerHandler: new HttpClientHandler(),
                    handlers: new DelegatingHandler[]
                    {
                    new ProxyHandler()
                    }
                ),
                defaults: new { path = RouteParameter.Optional },
                constraints: null
            );
        }
    }

    public class ProxyHandler : DelegatingHandler
    {
        private static HttpClient client = new HttpClient();
        private static string ForwardHost = null;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var query = request.RequestUri.ParseQueryString();
            var requestedUrl = query["q"];

            if (!string.IsNullOrEmpty(requestedUrl))
            {
                var req = new UriBuilder(requestedUrl);
                ForwardHost = req.Host;
                return new HttpResponseMessage();
            }
            var forwardUri = new UriBuilder(request.RequestUri.AbsoluteUri);
            forwardUri.Host = ForwardHost;
            //forwardUri.Port = 62904;
            request.RequestUri = forwardUri.Uri;

            if (request.Method == HttpMethod.Get)
            {
                request.Content = null;
            }

            request.Headers.Add("X-Forwarded-Host", request.Headers.Host);
            request.Headers.Host = forwardUri.Host;
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            return response;
        }
    }
}
