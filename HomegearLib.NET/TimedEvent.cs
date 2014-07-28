using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class TimedEvent : Event
    {
        protected DateTime _eventTime;
        public DateTime EventTime { get { return _eventTime; } internal set { _eventTime = value; } }

        protected Int32 _recurEvery = 0;
        public Int32 RecurEvery { get { return _recurEvery; } internal set { _recurEvery = value; } }

        protected DateTime _endTime;
        public DateTime EndTime { get { return _endTime; } internal set { _endTime = value; } }

        public TimedEvent(RPCController rpc, String id) : base(rpc, id)
        {

        }
    }
}
