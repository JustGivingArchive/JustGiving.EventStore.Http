using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using JustGiving.EventStore.Http.SubscriberHost;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests
{
    [TestFixture]
    public class EventTypeResolverTests
    {
        [Test]
        public void WhenReferencingAType_AndTypeIsNotInAppDomain_ReturnNull()
        {
            new EventTypeResolver().Resolve("something completely arbitrary").Should().BeNull();
        }

        [Test]
        public void WhenReferencingAType_AndTypeIsInAppDomain_ReturnThatType()
        {
            var requiredTypeString = typeof(SomePlainType).FullName;
            var result = new EventTypeResolver().Resolve(requiredTypeString);
            result.Should().Be<SomePlainType>();
        }

        [Test]
        public void WhenReferencingAType_AndTypeIsNotInAppDomainButAMappedTypeExist_ReturnTheMappedType()
        {
            var result = new EventTypeResolver().Resolve("Some arbitrarily bound type");
            result.Should().Be<SomeBoundType>();
        }

        [Test]
        public void WhenReferencingAType_AndMappedTypeExistsWithMultipleMappings_ReturnTheMappedType()
        {
            var result1 = new EventTypeResolver().Resolve("Some multi bound type 1");
            var result2 = new EventTypeResolver().Resolve("Some multi bound type 2");
            var result3 = new EventTypeResolver().Resolve("Some multi bound type 3");

            result1.Should().Be<SomeMultiBoundType>();
            result2.Should().Be<SomeMultiBoundType>();
            result3.Should().Be<SomeMultiBoundType>();
        }

        private class SomePlainType{}

        [BindsTo("Some arbitrarily bound type")]
        private class SomeBoundType { }

        [BindsTo("Some multi bound type 1")]
        [BindsTo("Some multi bound type 2")]
        [BindsTo("Some multi bound type 3")]
        private class SomeMultiBoundType { }
    }

    
}