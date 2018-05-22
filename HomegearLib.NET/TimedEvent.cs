using System;
using System.Collections.Generic;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class TimedEvent : Event
    {
        protected DateTime _eventTime = DateTime.MinValue;
        public DateTime EventTime { get { return _eventTime; } internal set { _eventTime = value; } }

        protected Int32 _recurEvery = 0;
        public Int32 RecurEvery { get { return _recurEvery; } internal set { _recurEvery = value; } }

        protected DateTime _endTime = DateTime.MinValue;
        public DateTime EndTime { get { return _endTime; } internal set { _endTime = value; } }

        internal TimedEvent(RPCController rpc, String id) : base(rpc, id)
        {

        }

        public TimedEvent(String id, Boolean enabled, String eventMethod, List<RPCVariable> eventMethodParams, DateTime eventTime, Int32 recurEvery = 0) : base(id, enabled, eventMethod, eventMethodParams)
        {
            _eventTime = eventTime;
            _recurEvery = recurEvery;
        }

        public TimedEvent(String id, Boolean enabled, String eventMethod, List<RPCVariable> eventMethodParams, DateTime eventTime, Int32 recurEvery, DateTime endTime) : base(id, enabled, eventMethod, eventMethodParams)
        {
            _eventTime = eventTime;
            _recurEvery = recurEvery;
            _endTime = endTime;
        }

        public override bool Update(Event value)
        {
            if (!(value is TimedEvent)) return true;
            bool changed = false;
            TimedEvent e = (TimedEvent)value;
            base.Update(value);
            if (_eventTime != e.EventTime)
            {
                changed = true;
                _eventTime = e.EventTime;
            }
            if (_recurEvery != e.RecurEvery)
            {
                changed = true;
                _recurEvery = e.RecurEvery;
            }
            if (_endTime != e.EndTime)
            {
                changed = true;
                _endTime = e.EndTime;
            }
            return changed;
        }
    }
}
