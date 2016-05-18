using System;
using System.Collections.Generic;
using System.Linq;
using Shuttle.Core.Infrastructure;

namespace Shuttle.Recall
{
	public class EventStream
	{
	    private ICanSnapshot _canSnapshot = null;
		private readonly List<Event> _events = new List<Event>();
		private int _initialVersion;

		public EventStream(Guid id)
		{
			Id = id;
			Version = 0;
			_initialVersion = 0;
		}

		public EventStream(Guid id, int version, IEnumerable<Event> events, Event snapshot)
		{
			Id = id;
			Version = version;
			_initialVersion = version;
			Snapshot = snapshot;

			if (events != null)
			{
				_events.AddRange(events);
			}
		}

		public Guid Id { get; private set; }
		public int Version { get; private set; }
		public Event Snapshot { get; private set; }
	    public bool Removed { get; private set; }

	    public EventStream Remove()
	    {
	        Removed = true;

	        return this;
	    }

	    public bool IsEmpty
	    {
	        get { return _events.Count == 0; }
	    }

		public EventStream CommitVersion()
		{
			_initialVersion = Version;

            return this;
        }

		public EventStream AddEvent(object data)
		{
			Guard.AgainstNull(data, "data");

			Version = Version + 1;

			_events.Add(new Event(Version, data.GetType().AssemblyQualifiedName, data));

            return this;
        }

		public EventStream AddSnapshot(object data)
		{
			Guard.AgainstNull(data, "data");

			Snapshot = new Event(Version, data.GetType().AssemblyQualifiedName, data);

            return this;
        }

		public bool ShouldSnapshot(int snapshotEventCount)
		{
			return _events.Count >= snapshotEventCount;
		}

	    public bool AttemptSnapshot(int snapshotEventCount)
	    {
	        if (!CanSnapshot || !ShouldSnapshot(snapshotEventCount))
	        {
	            return false;
	        }

            AddSnapshot(_canSnapshot.GetSnapshotEvent());

	        return true;
	    }

	    public bool CanSnapshot
	    {
	        get { return _canSnapshot != null; }
	    }

	    public IEnumerable<Event> EventsAfter(Event @event)
		{
			return _events.Where(e => e.Version > @event.Version);
		}

		public IEnumerable<Event> EventsAfter(int version)
		{
			return _events.Where(e => e.Version > version);
		}

		public IEnumerable<Event> NewEvents()
		{
			return _events.Where(e => e.Version > _initialVersion);
		}

		public IEnumerable<Event> PastEvents()
		{
			return _events.Where(e => e.Version <= _initialVersion);
		}

		public void Apply(object instance)
		{
			Apply(instance, "On");
		}

		public void Apply(object instance, string eventHandlingMethodName)
		{
			Guard.AgainstNull(instance, "instance");

            _canSnapshot = instance as ICanSnapshot;
            
			var events = new List<Event>(PastEvents());

			if (HasSnapshot)
			{
				events.Insert(0, Snapshot);
			}

            var instanceType = instance.GetType();
            
            foreach (var @event in events)
			{
			    var method = instanceType.GetMethod(eventHandlingMethodName, new[] {@event.Data.GetType()});

				if (method == null)
				{
					throw new UnhandledEventException(string.Format(RecallResources.UnhandledEventException,
						instanceType.AssemblyQualifiedName, eventHandlingMethodName, @event.Data.GetType().AssemblyQualifiedName));
				}

				method.Invoke(instance, new[] {@event.Data});
			}
		}

		public bool HasSnapshot
		{
			get { return Snapshot != null; }
		}

		public void ConcurrencyInvariant(int expectedVersion)
		{
			if (expectedVersion != _initialVersion)
			{
				throw new EventStreamConcurrencyException(string.Format(RecallResources.EventStreamConcurrencyException, Id, _initialVersion, expectedVersion));
			}
		}
	}
}