using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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
            var sut = new PerformanceStats(TimeSpan.FromMilliseconds(1), 3);
            await Task.Delay(5);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(5);
            sut.Records.Should().HaveCount(2);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(5);
            sut.Records.Should().HaveCount(3);
        }

        [Test]
        public async Task TidyRecords_WhenAddingRecords_ShouldGrowUpToMaximumSize()
        {
            var sut = new PerformanceStats(TimeSpan.FromMilliseconds(1), 3);
            await Task.Delay(5);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(5);
            sut.Records.Should().HaveCount(2);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(5);
            sut.Records.Should().HaveCount(3);

            sut.MessageProcessed("something arbitrary");
            await Task.Delay(5);
            sut.Records.Should().HaveCount(3);
        }
    }
}