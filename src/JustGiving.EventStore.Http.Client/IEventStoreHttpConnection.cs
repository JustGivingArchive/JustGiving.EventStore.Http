using System;
using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.Client
{
    public interface IEventStoreHttpConnection
    {
        /// <summary>
        /// Gets the name of this connection. A connection name can be used for disambiguation
        /// in log files.
        /// </summary>
        string ConnectionName { get; }

        /// <summary>
        /// Deletes a stream from the Event Store asynchronously
        /// </summary>
        /// <param name="stream">The name of the stream to delete.</param>
        /// <param name="expectedVersion">The expected version that the streams should have when being deleted. <see cref="ExpectedVersion"/></param>
        /// <returns>A <see cref="Task"/> that can be awaited upon by the caller.</returns>
        Task DeleteStreamAsync(string stream, int expectedVersion);

        /// <summary>
        /// Deletes a stream from the Event Store asynchronously
        /// </summary>
        /// <param name="stream">The name of the stream to delete.</param>
        /// <param name="expectedVersion">The expected version that the streams should have when being deleted. <see cref="ExpectedVersion"/></param>
        /// <param name="hardDelete">Indicator for tombstoning vs soft-deleting the stream. Tombstoned streams can never be recreated. Soft-deleted streams
        /// can be written to again, but the EventNumber sequence will not start from 0.</param>
        /// <returns>A <see cref="Task"/> that can be awaited upon by the caller.</returns>
        Task DeleteStreamAsync(string stream, int expectedVersion, bool hardDelete);

        /// <summary>
        /// Appends Events asynchronously to a stream.
        /// </summary>
        /// <remarks>
        /// When appending events to a stream the <see cref="ExpectedVersion"/> choice can
        /// make a very large difference in the observed behavior. For example, if no stream exists
        /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
        /// 
        /// There are also differences in idempotency between different types of calls.
        /// If you specify an ExpectedVersion aside from ExpectedVersion.Any the Event Store
        /// will give you an idempotency guarantee. If using ExpectedVersion.Any the Event Store
        /// will do its best to provide idempotency but does not guarantee idempotency
        /// </remarks>
        /// <param name="stream">The name of the stream to append events to</param>
        /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to</param>
        /// <param name="events">The events to append to the stream</param>
        Task AppendToStreamAsync(string stream, int expectedVersion, params NewEventData[] events);

        /// <summary>
        /// Asynchronously reads a single event from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
        /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
        Task<EventReadResult> ReadEventAsync(string stream, int eventNumber);

        /// <summary>
        /// Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start 
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="start">The starting point to read from</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="longPollingTimeout">The amount of time to wait during a stream read if no events can be found.  If null, then do not wait</param>
        /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation</returns>
        Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, int start, int count, TimeSpan? longPollingTimeout);
    }
}