using HomegearLib.RPC;
using System;

namespace HomegearLib
{
    public class ServiceMessage
    {
        public enum ServiceMessageType
        {
            global = 0,
            family = 1,
            device = 2
        }

        public ServiceMessageType Type { get; internal set; }
        public DateTime Timestamp { get; internal set; } = DateTime.MinValue;
        public Int32 FamilyID { get; internal set; } = -1;
        public Int64 PeerID { get; internal set; } = 0;
        public Int32 Channel { get; internal set; } = -1;
        public Int32 MessageID { get; internal set; } = -1;
        public String Message { get; internal set; }
        public RPCVariable Data { get; internal set; } = new RPCVariable();
        public Int32 Value { get; internal set; } = 0;

        public ServiceMessage(DateTime timestamp, Int32 messageID, String message, RPCVariable data, Int32 value)
        {
            Type = ServiceMessageType.global;
            Timestamp = timestamp;
            MessageID = messageID;
            Message = message;
            Data = data;
            Value = value;
        }

        public ServiceMessage(DateTime timestamp, Int32 familyId, Int32 messageID, String message, RPCVariable data, Int32 value)
        {
            Type = ServiceMessageType.family;
            Timestamp = timestamp;
            FamilyID = familyId;
            MessageID = messageID;
            Message = message;
            Data = data;
            Value = value;
        }

        public ServiceMessage(DateTime timestamp, Int64 peerId, Int32 channel, String message, Int32 value)
        {
            Type = ServiceMessageType.device;
            Timestamp = timestamp;
            PeerID = peerId;
            Channel = channel;
            Message = message;
            Value = value;
        }
    }
}
