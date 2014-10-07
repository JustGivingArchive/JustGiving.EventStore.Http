using System.Linq;
using System.Threading;
using FluentAssertions;
using JustGiving.EventStore.Http.SubscriberHost.Monitoring;
using NUnit.Framework;
using System;

namespace JG.EventStore.Http.SubscriberHost.Tests
{
    [TestFixture]
    public class StreamSubscriberIntervalMonitorTest
    {
        private const string StreamName = "streamS";
        private IStreamSubscriberIntervalMonitor _monitor;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

        [SetUp]
        public void Setup()
        {
            _monitor = new StreamSubscriberIntervalMonitor();
        }

        [Test]
        public void UpdateMonitor_stream_should_not_be_behind_when_just_updated()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, _interval);
            _monitor.IsStreamBehind(StreamName).Should().BeFalse();
        }

        [Test]
        public void UpdateMonitor_any_stream_should_not_be_behind_when_just_updated()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, _interval);
            _monitor.IsAnyStreamBehind().Should().BeFalse();
        }

        [Test]
        public void UpdateMonitor_strem_should_be_behind_when_0_interval()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, TimeSpan.FromMilliseconds(-5));
            _monitor.IsStreamBehind(StreamName).Should().BeTrue();
        }

        [Test]
        public void UpdateMonitor_any_stream_should_be_behind_when_0_interval()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, TimeSpan.FromMilliseconds(0));
            Thread.Sleep(1);
            _monitor.IsAnyStreamBehind().Should().BeTrue();
        }

        [Test]
        public void UpdateMonitor_any_stream_should_be_behind_when_some_stream_has_0_interval()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, TimeSpan.FromMilliseconds(-5));
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName + "1", _interval);
            _monitor.IsAnyStreamBehind().Should().BeTrue();
        }

        [Test]
        public void Monitor_stats_should_return_stats_for_a_stream()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, TimeSpan.FromMilliseconds(-5));
            _monitor.GetStreamIntervalStats(StreamName).Should().NotBeNull();
        }

        [Test]
        public void Monitor_stats_should_return_null_when_stream_not_present()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, TimeSpan.FromMilliseconds(-5));
            _monitor.GetStreamIntervalStats(StreamName + "1").Should().BeNull();
        }

        [Test]
        public void Monitor_stats_should_return_stats_for_every_stream()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, TimeSpan.FromMilliseconds(-5));
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName + "1", _interval);
            _monitor.GetStreamsIntervalStats().Should().HaveCount(2);
        }

        [Test]
        public void Monitor_stats_should_return_stats_for_every_stream_with_correct_values()
        {
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName, TimeSpan.FromMilliseconds(-5));
            _monitor.UpdateEventStreamSubscriberIntervalMonitor(StreamName + "1", _interval);
            var stats = _monitor.GetStreamsIntervalStats();
            stats.Should().HaveCount(2);
            stats.Single(x => x.StreamName == StreamName).IsStreamBehind.Should().BeTrue();
            stats.Single(x => x.StreamName == StreamName + "1").IsStreamBehind.Should().BeFalse();
        }

        [Test]
        public void Monitor_stats_should_return_no_stats()
        {
            _monitor.GetStreamsIntervalStats().Should().BeEmpty();
        }

        [Test]
        public void IsStreamBehind_should_return_null_when_stream_not_in()
        {
            _monitor.IsStreamBehind(StreamName).Should().NotHaveValue();
        }
    }
}