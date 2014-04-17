using System.Collections.Concurrent;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class MemoryBackedStreamPositionRepositoryForDebugging : IStreamPositionRepository
    {
        ConcurrentDictionary<string, int> cache = new ConcurrentDictionary<string, int>();

        public int? GetPositionFor(string stream)
        {
            int position;
            if (cache.TryGetValue(stream, out position))
            {
                return position;
            }

            return null;
        }

        public void SetPositionFor(string stream, int position)
        {
            cache[stream] = position;
        }
    }
}