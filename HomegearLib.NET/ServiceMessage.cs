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
        public long FamilyID { get; internal set; } = -1;
        public long PeerID { get; internal set; } = 0;
        public long Channel { get; internal set; } = -1;
        public long MessageID { get; internal set; } = -1;
        public string Message { get; internal set; }
        public RPCVariable Data { get; internal set; } = new RPCVariable();
        public long Value { get; internal set; } = 0;

        public ServiceMessage(DateTime timestamp, long messageID, string message, RPCVariable data, long value)
        {
            Type = ServiceMessageType.global;
            Timestamp = timestamp;
            MessageID = messageID;
            Message = message;
            Data = data;
            Value = value;
        }

        public ServiceMessage(DateTime timestamp, long familyId, long messageID, string message, RPCVariable data, long value)
        {
            Type = ServiceMessageType.family;
            Timestamp = timestamp;
            FamilyID = familyId;
            MessageID = messageID;
            Message = message;
            Data = data;
            Value = value;
        }

        public ServiceMessage(DateTime timestamp, long peerId, long channel, string message, long value)
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
