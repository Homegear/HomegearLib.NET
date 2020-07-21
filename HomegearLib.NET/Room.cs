using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomegearLib
{
    public class Room : IDisposable
    {
        internal RPCController _rpc = null;

        private ulong _id = 0;

        private Dictionary<string, RPCVariable> _metadata = null;
        private List<RPCVariable> _uiElements = null;
        private string _language = null;

        public ulong ID { get { return _id; } internal set { _id = value; } }

        private Dictionary<string, string> _translations = null;
        public Dictionary<string, string> Translations { 
            get { 
                return _translations; 
            } 
            set { 
                _translations = value;
                _rpc?.UpdateRoom(this);
            } 
        }

        public Dictionary<string, RPCVariable> Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    _metadata = _rpc?.GetRoomMetadata(this);
                }

                return _metadata;
            }

            set
            {
                _rpc?.SetRoomMetadata(this, value);
                _metadata = value;
            }
        }

        public Room(RPCController rpc, Dictionary<string, string> translations)
        {
            _rpc = rpc;
            _translations = translations;
        }

        public Room(RPCController rpc, ulong id, Dictionary<string, string> translations)
        {
            _rpc = rpc;
            _id = id;
            _translations = translations;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public bool HasName(string name)
        {
            foreach(var translation in _translations)
            {
                if (translation.Value == name) return true;
            }
            return false;
        }

        public string Name(string languageCode)
        {
            if (_translations.Count == 0) return "";

            if (_translations.ContainsKey(languageCode)) return _translations[languageCode];
            else if (_translations.ContainsKey("en-US")) return _translations["en-US"];
            else return _translations.First().Value;
        }

        public List<RPCVariable> UiElements(string languageCode)
        {
            if (_uiElements == null || languageCode != _language)
            {
                if (languageCode == null || languageCode.Length == 0) languageCode = "en-US";
                _uiElements = _rpc?.GetRoomUiElements(this, languageCode);
                _language = languageCode;
            }

            return _uiElements;
        }

        public void AddChannelToRoom(Channel channel)
        {
            _rpc?.AddChannelToRoom(channel, this);
        }

        public void AddDeviceToRoom(Device device)
        {
            _rpc?.AddDeviceToRoom(device, this);
        }

        public void AddVariableToRoom(Variable variable)
        {
            _rpc?.AddVariableToRoom(variable, this);
        }
        public void AddSystemVariableToRoom(string variable)
        {
            _rpc?.AddSystemVariableToRoom(variable, this);
        }

        public bool RemoveChannelFromRoom(Channel channel)
        {
            if (_rpc == null) return false;

            return _rpc.RemoveChannelFromRoom(channel, this);
        }

        public bool RemoveDeviceFromRoom(Device device)
        {
            if (_rpc == null) return false;

            return _rpc.RemoveDeviceFromRoom(device, this);
        }

        public bool RemoveVariableFromRoom(Variable variable)
        {
            if (_rpc == null) return false;

            return _rpc.RemoveVariableFromRoom(variable, this);
        }

        public void RemoveSystemVariableFromRoom(string variable)
        {
            _rpc?.RemoveSystemVariableFromRoom(variable, this);
        }


        // TODO: maybe save them internally and add Reload functions?

        public Dictionary<string, RPCVariable> GetChannelsInRoom()
        {
            return _rpc?.GetChannelsInRoom(this);
        }

        public Dictionary<string, RPCVariable> GetDevicesInRoom()
        {
            return _rpc?.GetDevicesInRoom(this);
        }
        public Dictionary<string, RPCVariable> GetVariablesInRoom()
        {
            return _rpc?.GetDevicesInRoom(this);
        }

        public List<RPCVariable> GetSystemVariablesInRoom()
        {
            List<RPCVariable> result = _rpc?.GetSystemVariablesInRoom(this);

            return result;
        }
    }
}
