using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class MemoryBackedStreamPositionRepositoryForDebugging : IStreamPositionRepository
    {
        ConcurrentDictionary<string, int> cache = new ConcurrentDictionary<string, int>();

        public async Task<int?> GetPositionForAsync(string stream)
        {
            await Task.FromResult(true);
            int position;
            if (cache.TryGetValue(stream, out position))
            {
                return position;
            }

            return null;
        }

        public async Task SetPositionForAsync(string stream, int position)
        {
            cache[stream] = position;
            await Task.FromResult(true);
        }
    }
}