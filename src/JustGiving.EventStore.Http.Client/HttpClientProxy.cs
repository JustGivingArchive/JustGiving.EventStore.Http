using System.Net.Http;
using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.Client
{
    public class HttpClientProxy : IHttpClientProxy
    {
        public async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request)
        {
            return await client.SendAsync(request).ConfigureAwait(false);
        }
    }
}
