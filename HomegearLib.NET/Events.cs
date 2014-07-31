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

        public Events(RPCController rpc, Dictionary<String, Event> events, EventType type) : base(events)
        {
            _rpc = rpc;
            _type = type;
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
            _dictionary = _rpc.ListEvents(_type);
        }

        public List<Event> Update(out bool eventsDeleted, out bool eventsAdded)
        {
            Dictionary<String, Event> events = _rpc.ListEvents(_type);
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
