using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Common;
using JustGiving.EventStore.Http.SubscriberHost.Monitoring;
using Moq;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests.Monitoring
{
    [TestFixture]
    public class PerformanceStatsTests
    {
        [Test]
        public void Records_ForEmptyCounter_YieldsEmptySingleResultSet()
        {
            var sut = new PerformanceStats(It.IsAny<TimeSpan>(), It.IsAny<int>());
            sut.Records.Should().HaveCount(1);
        }

        [Test]
        public void Records_ForEmptyCounter_YieldsEmptySingleResultSetForCurrentPeriod()
        {
            var sut = new PerformanceStats(TimeSpan.FromMinutes(5), It.IsAny<int>());
            var item = sut.Records.First();
            item.StartTime.Should().BeWithin(TimeSpan.FromMilliseconds(50)).Before(DateTime.Now);
            item.EndTime.Should().BeWithin(TimeSpan.FromMilliseconds(50)).Before(DateTime.Now.AddMinutes(5));
        }

        [Test]
        public async Task MessageProcessed_ShouldNotGrowQueueIfNewEventIsWithinTheLatestTimePeriod()
        {
            var sut = new PerformanceStats(TimeSpan.FromHours(1), 3);
            await Task.Delay(5);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(5);
            sut.Records.Should().HaveCount(1);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(5);
            sut.Records.Should().HaveCount(1);
        }

        [Test]
        public async Task MessageProcessed_ShouldGrowQueueIfNewEventIsLaterThanLatestTimePeriod()
        {
            var sut = new PerformanceStats(TimeSpan.FromMilliseconds(50), 3);
            await Task.Delay(60);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(60);
            sut.Records.Should().HaveCount(2);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(60);
            sut.Records.Should().HaveCount(3);
        }

        [Test]
        public async Task TidyRecords_WhenAddingRecords_ShouldGrowUpToMaximumSize()
        {
            var sut = new PerformanceStats(TimeSpan.FromMilliseconds(50), 3);
            await Task.Delay(60);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(60);
            sut.Records.Should().HaveCount(2);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(60);
            sut.Records.Should().HaveCount(3);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(60);
            sut.Records.Should().HaveCount(3);
        }

        [Test]
        public async Task TidyRecords_WhenAddingRecords_ShouldNotOverflowMaxRecordCount()
        {
            var sut = new PerformanceStats(TimeSpan.FromMilliseconds(30), 3);

            await Task.Delay(150);

            var records = (Queue<PerformanceRecord>)sut.Records;

            sut.TidyRecords();

            records.Count.Should().Be(3);
        }

        [Test]
        public async Task TidyRecords_WhenAddingRecords_ShouldFillInGapsInBuketsIfNecessary()
        {
            var sut = new PerformanceStats(TimeSpan.FromMilliseconds(30), 3);

            await Task.Delay(150);

            var records = (Queue<PerformanceRecord>)sut.Records;

            sut.TidyRecords();

            var now = DateTime.Now;

            var last = records.Skip(2).First();
            var middle = records.Skip(1).First();
            var first = records.Skip(0).First();
            last.EndTime.Should().BeWithin(TimeSpan.FromMilliseconds(50)).After(now);
            last.EndTime.Should().BeWithin(TimeSpan.FromMilliseconds(50)).Before(now);

            var oneMS = TimeSpan.FromMilliseconds(1);
            last.StartTime.Should().BeWithin(oneMS).After(middle.EndTime);
            middle.StartTime.Should().BeWithin(oneMS).After(first.EndTime);
        }

        [Test]
        public async Task RecordsAfter_ShouldReturnAllRecordsStartingAfterTheSuppliedDate()
        {
            var sut = new PerformanceStats(TimeSpan.FromMilliseconds(30), 3);

            var records = (Queue<PerformanceRecord>)sut.Records;

            records.Clear();

            var first = new PerformanceRecord(It.IsAny<TimeSpan>(), new DateTime(1, 2, 3));
            var second = new PerformanceRecord(It.IsAny<TimeSpan>(), first.StartTime.AddTicks(1));
            
            records.Enqueue(first);
            records.Enqueue(second);

            var foundRecords = sut.RecordsAfter(first.StartTime);

            foundRecords.Should().BeEquivalentTo(new[] {second});
        }
    }
}