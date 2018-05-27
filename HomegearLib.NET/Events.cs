using HomegearLib.RPC;
using System;
using System.Collections.Generic;

namespace HomegearLib
{
    public class Events : ReadOnlyDictionary<string, Event>, IDisposable
    {
        RPCController _rpc = null;

        EventType _type;
        public EventType Type { get { return _type; } }

        int _peerId;
        public int PeerID { get { return _peerId; } }

        public Events(RPCController rpc, Dictionary<string, Event> events, EventType type) : base(events)
        {
            _rpc = rpc;
            _type = type;
        }

        public Events(RPCController rpc, Dictionary<string, Event> events, int peerId) : base(events)
        {
            _rpc = rpc;
            _peerId = peerId;
            _type = EventType.Triggered;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<string, Event> element in _dictionary)
            {
                element.Value.Dispose();
            }
        }

        public void Add(Event newEvent)
        {
            _rpc.AddEvent(newEvent);
        }

        public void Reload()
        {
            if (_type == EventType.Timed) _dictionary = _rpc.ListEvents(_type);
            else _dictionary = _rpc.ListEvents(_peerId);
        }

        public List<Event> Update(out bool eventsDeleted, out bool eventsAdded)
        {
            Dictionary<string, Event> events = (_type == EventType.Timed) ? _rpc.ListEvents(_type) : _rpc.ListEvents(_peerId);
            eventsDeleted = false;
            eventsAdded = false;
            List<Event> changedEvents = new List<Event>();
            foreach (KeyValuePair<string, Event> eventPair in events)
            {
                if (!_dictionary.ContainsKey(eventPair.Key))
                {
                    eventsAdded = true;
                    continue;
                }
                Event currentEvent = _dictionary[eventPair.Key];
                if (currentEvent.Update(eventPair.Value)) changedEvents.Add(currentEvent);
            }
            foreach (KeyValuePair<string, Event> eventPair in _dictionary)
            {
                if (!events.ContainsKey(eventPair.Key))
                {
                    eventsDeleted = true;
                    break;
                }
            }
            return changedEvents;
        }
    }
}
