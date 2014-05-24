JG.EventStore.Http
==================

A minimal [EventStore](http://www.geteventstore.com) .Net HTTP client, with some subscribing functionality too.

FAQs
----

**Q**: Are these questions frequently asked?<br />
**A**: No, they have never been asked

**Q**: What platform does it run on?<br />
**A**: This package makes extensive use of the C#5 async pattern, and is built against .Net 4.5.  You may be able to run it against .Net 4 with the BCL compatibility packs, though we haven't tried.

**Q**: Why did you make this?<br />
**A**: We wanted a simple, testable way to talk with our EventStore cluster over HTTP, as well as subscribe to events in a cacheable manner.  We haven't found any other library, so we can give back to the ES community in our own little way

**Q**: Do you advocate HTTP over raw TCP?<br />
**A**: Each to their own; check out [the official line on it](https://github.com/eventstore/eventstore/wiki/Which-API).  We are using varnish which could be handy...


**Q**: Do you have any nuget packages?<br />
**A**: Yes:<br />
    [**JustGiving.EventStore.Http.Client**](http://www.nuget.org/packages/JustGiving.EventStore.Http.Client/) - The main client that actually talks to an EventStore instance<br />
    [**JustGiving.EventStore.Http.SubscriberHost**](http://www.nuget.org/packages/JustGiving.EventStore.Http.SubscriberHost/) - A library that will poll a stream, and invoke message handlers as events are found<br />
    [**JustGiving.EventStore.Http.SubscriberHost.Ninject**](http://www.nuget.org/packages/JustGiving.EventStore.Http.SubscriberHost.Ninject/) - A support library for the SubscriberHost to find MessageHandlers for messages<br />

**Q**: What is the minimum I need to do to get a client running?<br />
**A**: <code>var connection = EventStoreHttpConnection.Create("http://localhost:9113");</code><br />
If you please, you can create a new <code>ConnectionSettingsBuilder()</code> to build a custom ConnectionSettings object.

**Q**: What exactly does the subscriber do?<br />
**A**: At a high level, the subscriber will poll specified queues of an EventStore instance indefinitely, running event handlers the match the event type.  The 'Event Type' is a string, found in the event's Summary field ('MessageType' in the web client).  If you use the EventStore http or official client, this is taken care of automatically.
<br />e.g. given an instance of the following event was stored

```csharp
namespace SomeApp.Events
{
    public class SomethingHappened
    {
        public Guid Id { get; set; }
        public bool ItWasGood { get; set; 
    }
}
```

The following message handler would be fired

```csharp
namespace SomeApp.Events
{
    public class DoSomethingUseful : IHandleEventsOf<SomethingHappened>
    {
        public Task Handle(SomethingHappened @event)
        {
            //Handle the event
        }
        
        public void OnError(Exception ex)
        {
            //Handle the exception
        }
    }
}
```

**Q**: What do I need to do to get the subscriber running?<br />
**A**: This is a little more tricky than the plain client because you will need to implement one or two interfaces:<br /><br />
[<code>IEventHandlerResolver</code>](https://github.com/JustGiving/JustGiving.EventStore.Http/blob/master/src/JustGiving.EventStore.Http.SubscriberHost/IEventHandlerResolver.cs) - Get all EventHandlers for an event type (or use the Ninject one above)<br />
[<code>IStreamPositionRepository</code>](https://github.com/JustGiving/JustGiving.EventStore.Http/blob/master/src/JustGiving.EventStore.Http.SubscriberHost/IStreamPositionRepository.cs) - Save and load the last-read event for a given stream<br />
Apart from that:

```csharp
var subscriber = EventStreamSubscriber.Create(someConnection, someEventHanderResolver, someStreamPositionRepository);
```

Again, you may use a builder to customise the subscriber:

```csharp
var builder = new EventStreamSubscriberSettingsBuilder(someConnection, someEventHanderResolver, someStreamPositionRepository);
var subscriber = EventStreamSubscriber.Create(builder);
```

Finally, subscribe to streams that you are interestesd in

```csharp
subscriber.SubscribeTo("InterestingStreamName");
```

**Q:** Do you collect have any performance metrics?<br />
**A:** By jingo, yes!  Each <code>IEventStreamSubscriber</code> has two properties - <code>AllEventsStats</code> & <code>ProcessedEventsStats</code>.  These collect counts of processed events and all events from your subscribed queues respectively.  Each one yields a <code>PerformanceStats</code> object, which is a time-series enumerable of stream/message-count pairs.<br />
The number and duration of snapshots may be configured per-subscriber when building it:

```csharp

var builder = new EventStreamSubscriberSettingsBuilder(someConnection, someEventHanderResolver, someStreamPositionRepository);
                .WithMessageProcessingStatsWindowPeriodOf(someTimespan)
                .WithMessageProcessingStatsWindowCountOf(someInt);
.
```

By default, a maximum of 120 windows will be kept, each representing 30 seconds of activity.


**Q**: What is the DI/IoC story here?<br />
**A**: Not ideal, as the interfaces were designed in homage to the GetEventStore TCP client. The builders are designed to be injectable directly, but the stream / subscribers will need a custom builder (hey, the stream endpoint is mandatory anyway...)<br />

**Q**: Do you accept pull requests?<br />
**A**: Maybe! This library was created to fulfil a small set of internal needs, and we have no real policy on them; it depends on the usefulness vs how breaky they are.  Feel free to discuss with [@jghackers](http://www.twitter.com/jghackers)

**Q**: If you could drink any cola, what would it be?<br />
**A**: Faygo

Building
--------

This API is built from the ground up on C#5, so you will need VS2013 to build, but no other prerequisites exist outside of some nuget packages
We created a build.ps1 which builds and runs tests etc if you are into that kind of thing.
