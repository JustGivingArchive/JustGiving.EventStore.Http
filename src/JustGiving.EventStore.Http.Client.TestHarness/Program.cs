using System;
using CommandLine;
using Nito.AsyncEx;

namespace JustGiving.EventStore.Http.Client.TestHarness
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }

        static async void MainAsync(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                Console.Write(options.GetUsage());
                Console.Write("Press any key to continue...");
                Console.Read();
                return;
            }

            await new Runner(options).Do();

            Console.Read();
        }
    }
}
