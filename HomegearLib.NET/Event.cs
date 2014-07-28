using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public class Event : IDisposable
    {
        RPCController _rpc = null;

        protected String _id;
        public String ID { get { return _id; } }

        protected Boolean _enabled;
        public Boolean Enabled { get { return _enabled; } internal set { _enabled = value; } }

        protected String _eventMethod = "";
        public String EventMethod { get { return _eventMethod; } internal set { _eventMethod = value; } }

        protected List<RPCVariable> _eventMethodParams = new List<RPCVariable>();
        public IReadOnlyList<RPCVariable> EventMethodParams { get { return _eventMethodParams.AsReadOnly(); } }
        internal void SetEventMethodParams(List<RPCVariable> value) { _eventMethodParams = value; }

        public Event(RPCController rpc, String id)
        {
            _rpc = rpc;
            _id = id;
        }

        public void Dispose()
        {
            _rpc = null;
        }
    }
}
