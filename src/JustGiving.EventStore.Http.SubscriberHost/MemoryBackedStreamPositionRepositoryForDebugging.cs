using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class MemoryBackedStreamPositionRepositoryForDebugging : IStreamPositionRepository
    {
        ConcurrentDictionary<string, int> cache = new ConcurrentDictionary<string, int>();

        public async Task<int?> GetPositionForAsync(string stream, string subscriberId)
        {
            await Task.FromResult(true);
            int position;
            if (cache.TryGetValue(string.Concat(stream, EventStreamSubscriber.StreamIdentifierSeparator, subscriberId), out position))
            {
                return position;
            }

            return null;
        }

        public async Task SetPositionForAsync(string stream, string subscriberId, int position)
        {
            cache[string.Concat(stream, EventStreamSubscriber.StreamIdentifierSeparator, subscriberId)] = position;
            await Task.FromResult(true);
        }
    }
}