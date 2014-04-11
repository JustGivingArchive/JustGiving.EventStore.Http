using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.Client.TestHarness
{
    public class Runner
    {
        private IEventStoreHttpConnection connection;
        private Options options;

        public Runner(Options options)
        {
            connection = EventStoreHttpConnection.Create(options.Endpoint);
            this.options = options;
        }

        public async Task Do()
        {
            var action = GetMethodFor(options.Operation);

            var sw = Stopwatch.StartNew();

            for (var i = 0; i < options.IterationCount; i++)
            {
                await action();

                DrawProgress(i, options.IterationCount);
            }

            Console.Write(".");

            sw.Stop();
            var ms = sw.ElapsedMilliseconds;

            Console.WriteLine();
            Console.WriteLine("Run {0} {1} operations in {2}ms ({3}ms/operation)", options.IterationCount, options.Operation, ms, ms/options.IterationCount);
        }

        private void DrawProgress(int i, int iterationCount, int totalColumns = 50)
        {
            var progressColumns =(int) (((float)i/(float)iterationCount)*(float)totalColumns);

            Console.Write('\r');
            Console.Write("[");
            if (progressColumns > 0)
            {
                Console.Write("".PadLeft(progressColumns, '='));
            }

            if (totalColumns - progressColumns >0)
            {
                Console.Write("".PadLeft(totalColumns - progressColumns, ' '));
            }
            Console.Write("]");
            Console.Write("{0} of {1}", i, iterationCount);
        }


        public Func<Task> GetMethodFor(Operation operation)
        {
            switch (operation)
            {
                case Operation.Append:
                    return Append;
                case Operation.ReadHead:
                    return ReadHead;
                case Operation.ReadStream:
                    return ReadSream;
            }

            throw new InvalidOperationException();
        }

        private async Task Append()
        {
            await connection.AppendToStreamAsync(options.StreamName, ExpectedVersion.Any, NewEventData.Create(new StubItem()));
        }

        private async Task ReadHead()
        {
            await connection.ReadEventAsync(options.StreamName, StreamPosition.End);
        }

        private async Task ReadSream()
        {
            await connection.ReadStreamEventsForwardAsync(options.StreamName, StreamPosition.Start, 20);
        }

        public class StubItem
        {
            public StubItem()
            {
                Id = Guid.NewGuid();
                Name = Guid.NewGuid().ToString();
            }

            public Guid Id { get; set; }
            public string Name { get; set; }
        }

    }
}