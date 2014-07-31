using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Events : ReadOnlyDictionary<String, Event>, IDisposable
    {
        RPCController _rpc = null;
        
        EventType _type;
        public EventType Type { get { return _type; } }

        Int32 _peerID;
        public Int32 PeerID { get { return _peerID; } }

        public Events(RPCController rpc, Dictionary<String, Event> events, EventType type) : base(events)
        {
            _rpc = rpc;
            _type = type;
        }

        public Events(RPCController rpc, Dictionary<String, Event> events, Int32 peerID) : base(events)
        {
            _rpc = rpc;
            _peerID = peerID;
            _type = EventType.Triggered;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<String, Event> element in _dictionary)
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
            else _dictionary = _rpc.ListEvents(_peerID);
        }

        public List<Event> Update(out bool eventsDeleted, out bool eventsAdded)
        {
            Dictionary<String, Event> events = (_type == EventType.Timed) ? _rpc.ListEvents(_type) : _rpc.ListEvents(_peerID);
            eventsDeleted = false;
            eventsAdded = false;
            List<Event> changedEvents = new List<Event>();
            foreach (KeyValuePair<String, Event> eventPair in events)
            {
                if (!_dictionary.ContainsKey(eventPair.Key))
                {
                    eventsAdded = true;
                    continue;
                }
                Event currentEvent = _dictionary[eventPair.Key];
                if (currentEvent.Update(eventPair.Value)) changedEvents.Add(currentEvent);
            }
            foreach (KeyValuePair<String, Event> eventPair in _dictionary)
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
