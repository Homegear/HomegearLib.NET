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

        private Int32 _currentTime;
        public Int32 CurrentTime { get { return _currentTime; } internal set { _currentTime = value; } }

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
        public Int32 PeerID { get { return _peerID; } internal set { _peerID = value; } }

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
        public void SetResetMethodParams(List<RPCVariable> value) { _resetMethodParams = value; }

        protected RPCVariable _lastValue;
        public RPCVariable LastValue { get { return _lastValue; } internal set { _lastValue = value; } }

        protected DateTime _lastRaised;
        public DateTime LastRaised { get { return _lastRaised; } internal set { _lastRaised = value; } }

        protected DateTime _lastReset;
        public DateTime LastReset { get { return _lastReset; } internal set { _lastReset = value; } }

        internal TriggeredEvent(RPCController rpc, String id) : base(rpc, id)
        {

        }

        public TriggeredEvent(String id, Boolean enabled, String eventMethod, List<RPCVariable> eventMethodParams, Int32 peerID, Int32 peerChannel, String variableName, EventTrigger trigger, RPCVariable triggerValue = null, Int32 resetAfter = 0, String resetMethod = "", List<RPCVariable> resetMethodParams = null) : base(id, enabled, eventMethod, eventMethodParams)
        {
            _peerID = peerID;
            _peerChannel = peerChannel;
            _variableName = variableName;
            _trigger = trigger;
            _triggerValue = triggerValue;
            _resetAfterStatic = resetAfter;
            _resetMethod = resetMethod;
            _resetMethodParams = resetMethodParams;
        }

        public TriggeredEvent(String id, Boolean enabled, String eventMethod, List<RPCVariable> eventMethodParams, Int32 peerID, Int32 peerChannel, String variableName, EventTrigger trigger, RPCVariable triggerValue, DynamicResetTime resetAfter, String resetMethod = "", List<RPCVariable> resetMethodParams = null) : base(id, enabled, eventMethod, eventMethodParams)
        {
            _peerID = peerID;
            _peerChannel = peerChannel;
            _variableName = variableName;
            _trigger = trigger;
            _triggerValue = triggerValue;
            _resetAfterDynamic = resetAfter;
            _resetMethod = resetMethod;
            _resetMethodParams = resetMethodParams;
        }

        public override bool Update(Event value)
        {
            if (!(value is TriggeredEvent)) return true;
            bool changed = false;
            TriggeredEvent e = (TriggeredEvent)value;
            base.Update(value);
            if (_peerID != e.PeerID)
            {
                changed = true;
                _peerID = e.PeerID;
            }
            if (_peerChannel != e.PeerChannel)
            {
                changed = true;
                _peerChannel = e.PeerChannel;
            }
            if (_variableName != e.VariableName)
            {
                changed = true;
                _variableName = e.VariableName;
            }
            if (_trigger != e.Trigger)
            {
                changed = true;
                _trigger = e.Trigger;
            }
            if (!_triggerValue.Compare(e.TriggerValue))
            {
                changed = true;
                _triggerValue.SetValue(e.TriggerValue);
            }
            if (_resetAfterStatic != e.ResetAfterStatic)
            {
                changed = true;
                _resetAfterStatic = e.ResetAfterStatic;
            }
            if (_resetAfterDynamic == null && e.ResetAfterDynamic != null)
            {
                changed = true;
                _resetAfterDynamic = e.ResetAfterDynamic;
            }
            else if(_resetAfterDynamic != null && e.ResetAfterDynamic == null)
            {
                changed = true;
                _resetAfterDynamic = null;
            }
            else if(_resetAfterDynamic != null && e.ResetAfterDynamic != null)
            {
                if(_resetAfterDynamic.CurrentTime != e.ResetAfterDynamic.CurrentTime)
                {
                    changed = true;
                    _resetAfterDynamic.CurrentTime = e.ResetAfterDynamic.CurrentTime;
                }
                if(_resetAfterDynamic.Factor != e.ResetAfterDynamic.Factor)
                {
                    changed = true;
                    _resetAfterDynamic.Factor = e.ResetAfterDynamic.Factor;
                }
                if(_resetAfterDynamic.InitialTime != e.ResetAfterDynamic.InitialTime)
                {
                    changed = true;
                    _resetAfterDynamic.InitialTime = e.ResetAfterDynamic.InitialTime;
                }
                if(_resetAfterDynamic.Limit != e.ResetAfterDynamic.Limit)
                {
                    changed = true;
                    _resetAfterDynamic.Limit = e.ResetAfterDynamic.Limit;
                }
                if(_resetAfterDynamic.Operation != e.ResetAfterDynamic.Operation)
                {
                    changed = true;
                    _resetAfterDynamic.Operation = e.ResetAfterDynamic.Operation;
                }
                if(_resetAfterDynamic.ResetAfter != e.ResetAfterDynamic.ResetAfter)
                {
                    changed = true;
                    _resetAfterDynamic.ResetAfter = e.ResetAfterDynamic.ResetAfter;
                }
            }
            if(_resetMethod != e.ResetMethod)
            {
                changed = true;
                _resetMethod = e.ResetMethod;
            }
            if (_resetMethodParams.Count != e.ResetMethodParams.Count)
            {
                changed = true;
                _resetMethodParams = new List<RPCVariable>();
                foreach(RPCVariable element in e.ResetMethodParams)
                {
                    _resetMethodParams.Add(element);
                }
            }
            else
            {
                var pair = _resetMethodParams.Zip(e.ResetMethodParams, (l, r) => new { Left = l, Right = r });
                foreach(var element in pair)
                {
                    if(!element.Left.Compare(element.Right))
                    {
                        changed = true;
                        element.Left.SetValue(element.Right);
                    }
                }
            }
            if(_lastValue != e.LastValue)
            {
                changed = true;
                _lastValue = e.LastValue;
            }
            if(_lastRaised != e.LastRaised)
            {
                changed = true;
                _lastRaised = e.LastRaised;
            }
            if(_lastReset != e.LastReset)
            {
                changed = true;
                _lastReset = e.LastReset;
            }
            return changed;
        }
    }
}
