using System;
using CommandLine;
using CommandLine.Text;

namespace JustGiving.EventStore.Http.Client.TestHarness
{
    public class Options
    {
        [Option('e', "endpoint", Required = true, HelpText = "The eventStore to be tested.")]
        public string Endpoint { get; set; }

        [Option('s', "stream", Required = true, HelpText = "The stream to be tested.")]
        public string StreamName { get; set; }

        [Option('o', "operation", Required = true , HelpText = "The Operation to perform: [Append | ReadHead | ReadStream].")]
        public string OperationString { get; set; }

        [Option('n', DefaultValue = 100, HelpText = "The number of iterations to perform.")]
        public int IterationCount { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        public Operation Operation
        {
            get { return (Operation) Enum.Parse(typeof (Operation), OperationString); }
        }
    }
}