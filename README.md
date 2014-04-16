JG.EventStore.Http
==================

A minimal [EventStore](http://www.geteventstore.com) .Net HTTP client, with some subscribing functionality too.

FAQs
----

**Q**: Are these questions frequently asked?<br />
**A**: No, they have never been asked

**Q**: What platform does it run on?<br />
**A**: This package makes extensive use of the C#5 async pattern, and is built against .Net 4.5.1.  You may be able to run it against .Net 4 with the BCL compatibility packs, though we haven't tried.

**Q**: Do you have any nuget packages?<br />
**A**: By the end of April, I'd guess

**Q**: Why did you make this?<br />
**A**: We wanted a simple, testable way to talk with our EventStore cluster over HTTP, as well as subscribe to events in a cacheable manner.  We haven't found any other library, so we can give back to the ES community in our own little way

**Q**: Do you advocate HTTP over raw TCP?<br />
**A**: Each to their own; check out [the official line on it](https://github.com/eventstore/eventstore/wiki/Which-API).  We are using varnish which could be handy...

**Q**: Do you accept pull requests?<br />
**A**: This library was created to fulfil a small set of internal needs, and we have no real policy on them.  Feel free to discuss at [@jghackers](http://www.twitter.com/jghackers)

**Q**: If you could drink any cola, what would it be?<br />
**A**: Faygo

Building
--------

This API is built from the ground up on C#5, so you will need VS2013 to build, but no other prerequisites exist