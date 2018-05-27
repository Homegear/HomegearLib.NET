using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HomegearLib
{
    public enum EventType
    {
        Triggered = 0,
        Timed = 1
    }

    public class Event : IDisposable
    {
        RPCController _rpc = null;

        protected string _id;
        public string ID { get { return _id; } }

        protected bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _rpc.EnableEvent(_id, value);
            }
        }
        internal void SetEnabledNoRPC(bool enabled) { _enabled = enabled; }

        protected string _eventMethod = "";
        public string EventMethod { get { return _eventMethod; } internal set { _eventMethod = value; } }

        protected List<RPCVariable> _eventMethodParams = new List<RPCVariable>();
        public IReadOnlyList<RPCVariable> EventMethodParams { get { return _eventMethodParams.AsReadOnly(); } }
        internal void SetEventMethodParams(List<RPCVariable> value) { _eventMethodParams = value; }

        internal Event(RPCController rpc, string id)
        {
            _rpc = rpc;
            _id = id;
        }

        public Event(string id, bool enabled, string eventMethod, List<RPCVariable> eventMethodParams)
        {
            _id = id;
            _enabled = enabled;
            _eventMethod = eventMethod;
            _eventMethodParams = eventMethodParams;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public void Remove()
        {
            _rpc.RemoveEvent(_id);
        }

        public virtual bool Update(Event value)
        {
            bool changed = false;
            if (_enabled != value.Enabled)
            {
                changed = true;
                _enabled = value.Enabled;
            }
            if (_eventMethod != value.EventMethod)
            {
                changed = true;
                _eventMethod = value.EventMethod;
            }
            if (_eventMethodParams.Count != value.EventMethodParams.Count)
            {
                changed = true;
                _eventMethodParams = new List<RPCVariable>();
                foreach (RPCVariable element in value.EventMethodParams)
                {
                    _eventMethodParams.Add(element);
                }
            }
            else
            {
                var pair = _eventMethodParams.Zip(value.EventMethodParams, (l, r) => new { Left = l, Right = r });
                foreach (var element in pair)
                {
                    if (!element.Left.Compare(element.Right))
                    {
                        changed = true;
                        element.Left.SetValue(element.Right);
                    }
                }
            }
            return changed;
        }
    }
}
