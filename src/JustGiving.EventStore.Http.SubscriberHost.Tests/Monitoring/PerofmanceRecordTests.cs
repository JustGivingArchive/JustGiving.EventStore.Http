using System;
using System.Collections.Generic;
using FluentAssertions;
using JustGiving.EventStore.Http.SubscriberHost.Monitoring;
using Moq;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests.Monitoring
{
    [TestFixture]
    public class PerformanceRecordTests
    {
        [Test]
        public void QueueStats_ForEmptyPerformanceRecord_ShouldYieldAnEmptyDictionaryOfCounters()
        {
            var sut = new PerformanceRecord(It.IsAny<TimeSpan>());
            sut.QueueStats().Should().NotBeNull();
            sut.QueueStats().Should().BeEmpty();
        }

        [Test]
        public void QueueStats_AfterThreeItemsAdded_ShouldYieldCorectCountsThree()
        {
            var sut = new PerformanceRecord(It.IsAny<TimeSpan>());
            sut.EventProcessed("A");
            sut.EventProcessed("B");
            sut.EventProcessed("A");
            sut.QueueStats().Should().BeEquivalentTo(new Dictionary<string, long>{{"A",2},{"B",1}});
        }

        [Test]
        public void TotalProcessedEventCount_ForEmptyPerformanceRecord_ShouldYieldZero()
        {
            var sut = new PerformanceRecord(It.IsAny<TimeSpan>());
            sut.TotalProcessedEventCount.Should().Be(0);
        }

        [Test]
        public void TotalProcessedEventCount_AfterItemsAdded_ShouldYieldCorrectCount()
        {
            var sut = new PerformanceRecord(It.IsAny<TimeSpan>());
            sut.EventProcessed("A");
            sut.EventProcessed("B");
            sut.EventProcessed("A");
            sut.TotalProcessedEventCount.Should().Be(3);
        }

        [Test]
        public void GetProcessedEventsFor_ForEmptyPerformanceRecord_ShouldYieldZeroForCountForAnyString()
        {
            var sut = new PerformanceRecord(It.IsAny<TimeSpan>());
            sut.GetProcessedEventsFor("something arbirtary").Should().Be(0);
        }

        [Test]
        public void GetProcessedEventsFor_AfterItemsAdded_ShouldYieldCorrectCounts()
        {
            var sut = new PerformanceRecord(It.IsAny<TimeSpan>());
            sut.EventProcessed("A");
            sut.EventProcessed("B");
            sut.EventProcessed("A");
            sut.GetProcessedEventsFor("A").Should().Be(2);
            sut.GetProcessedEventsFor("B").Should().Be(1);
        }

        [Test]
        public void StartTime_ShouldBeCalculatedAccordingToCurrentTimeWhenNotSpecified()
        {
            var sut = new PerformanceRecord(It.IsAny<TimeSpan>());
            sut.StartTime.Should().BeWithin(TimeSpan.FromMilliseconds(50)).Before(DateTime.Now);
        }

        [Test]
        public void StartTime_ShouldBeSetAccordinglyWhenSpecified()
        {
            var expected = new DateTime(1234, 5, 6);
            var sut = new PerformanceRecord(It.IsAny<TimeSpan>(), expected);
            sut.StartTime.Should().Be(expected);
        }

        [Test]
        public void EndTime_ShouldBeCalculatedCorrectlyAccordingToRecordPeriod()
        {
            var recordPeriod = TimeSpan.FromMinutes(5);
            var sut = new PerformanceRecord(recordPeriod);
            sut.EndTime.Should().BeExactly(recordPeriod).After(sut.StartTime);
        }
    }
}