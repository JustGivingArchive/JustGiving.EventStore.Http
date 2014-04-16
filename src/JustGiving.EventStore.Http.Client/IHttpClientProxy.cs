using System.Net.Http;
using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.Client
{
    public interface IHttpClientProxy
    {
        Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request);
    }
}