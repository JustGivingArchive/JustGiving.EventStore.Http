using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    /// <summary>
    /// A persistent (to the extent your system requires) storage mechanism the subscriber uses to remember its last-read position for any given stream
    /// </summary>
    /// <remarks>This should be implemented by the host-system, although the non-persistent <see cref="MemoryBackedStreamPositionRepositoryForDebugging"/> may be used for development purposes</remarks>
    public interface IStreamPositionRepository
    {
        /// <summary>
        /// Returns the index of most-recently processed event for a given stream
        /// </summary>
        /// <param name="stream">The name of the stream to be queried</param>
        /// <param name="subscriberId">The arbetrary name of a specific scubscriber to a stream, to support multiple subscribers in the same app, which may be at different positions</param>
        /// <returns>The last-processed position, or null if the stream has not been previously read</returns>
        Task<int?> GetPositionForAsync(string stream, string subscriberId);

        /// <summary>
        /// Records the index of most-recently processed event for a given stream
        /// </summary>
        /// <param name="stream">The name of the stream to be queried</param>
        /// <param name="subscriberId">The arbetrary name of a specific scubscriber to a stream, to support multiple subscribers in the same app, which may be at different positions</param>
        /// <param name="position">The most index of the most-recently processed event</param>
        Task SetPositionForAsync(string stream, string subscriberId, int position);
    }
}