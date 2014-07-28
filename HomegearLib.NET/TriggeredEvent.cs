using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomegearLib.RPC;

namespace HomegearLib
{
    public enum EventTrigger
    {
        Unchanged = 1,
        Changed = 2,
        Greater = 3,
        Less = 4,
        GreaterOrUnchanged = 5,
        LessOrUnchanged = 6,
        Updated = 7,
        Value = 8,
        NotValue = 9,
        GreaterThanValue = 10,
        LessThanValue = 11,
        GreaterOrEqualValue = 12,
        LessOrEqualValue = 13
    }

    public enum DynamicResetTimeOperation
    {
        Addition = 1,
        Subtraction = 2,
        Multiplication = 3,
        Division = 4
    }

    public class DynamicResetTime
    {
        private Int32 _initialTime = 30;
        public Int32 InitialTime { get { return _initialTime; } internal set { _initialTime = value; } }

        private Int32 _resetAfter = 300;
        public Int32 ResetAfter { get { return _resetAfter; } internal set { _resetAfter = value; } }

        private DynamicResetTimeOperation _operation = DynamicResetTimeOperation.Multiplication;
        public DynamicResetTimeOperation Operation { get { return _operation; } internal set { _operation = value; } }

        private Double _factor = 2;
        public Double Factor { get { return _factor; } internal set { _factor = value; } }

        private Int32 _limit = 300;
        public Int32 Limit { get { return _limit; } internal set { _limit = value; } }

        private DateTime _currentTime;
        public DateTime CurrentTime { get { return _currentTime; } internal set { _currentTime = value; } }

        internal DynamicResetTime() { }

        public DynamicResetTime(Int32 initialTime, DynamicResetTimeOperation operation, Double factor, Int32 limit, Int32 resetAfter)
        {
            _initialTime = initialTime;
            _operation = operation;
            _factor = factor;
            _limit = limit;
            _resetAfter = resetAfter;
        }
    }

    public class TriggeredEvent : Event
    {
        protected Int32 _peerID;
        public Int32 PeerID { get { return _peerID; } }

        protected Int32 _peerChannel;
        public Int32 PeerChannel { get { return _peerChannel; } internal set { _peerChannel = value; } }

        protected String _variableName;
        public String VariableName { get { return _variableName; } internal set { _variableName = value; } }

        protected EventTrigger _trigger;
        public EventTrigger Trigger { get { return _trigger; } internal set { _trigger = value; } }

        protected RPCVariable _triggerValue;
        public RPCVariable TriggerValue { get { return _triggerValue; } internal set { _triggerValue = value; } }

        protected Int32 _resetAfterStatic;
        public Int32 ResetAfterStatic { get { return _resetAfterStatic; } internal set { _resetAfterStatic = value; } }

        protected DynamicResetTime _resetAfterDynamic = null;
        public DynamicResetTime ResetAfterDynamic { get { return _resetAfterDynamic; } internal set { _resetAfterDynamic = value; } }

        protected String _resetMethod = "";
        public String ResetMethod { get { return _resetMethod; } internal set { _resetMethod = value; } }

        protected List<RPCVariable> _resetMethodParams = new List<RPCVariable>();
        public IReadOnlyList<RPCVariable> ResetMethodParams { get { return _resetMethodParams.AsReadOnly(); } }
        internal void SetResetMethodParams(List<RPCVariable> value) { _resetMethodParams = value; }

        protected RPCVariable _lastValue;
        public RPCVariable LastValue { get { return _lastValue; } internal set { _lastValue = value; } }

        protected DateTime _lastRaised;
        public DateTime LastRaised { get { return _lastRaised; } internal set { _lastRaised = value; } }

        protected DateTime _lastReset;
        public DateTime LastReset { get { return _lastReset; } internal set { _lastReset = value; } }

        public TriggeredEvent(RPCController rpc, String id) : base(rpc, id)
        {

        }
    }
}
