using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomegearLib
{
    public class Story : IDisposable
    {
        RPCController _rpc = null;

        private ulong _id = 0;
        public ulong ID { get { return _id; } internal set { _id = value; } }

        private Dictionary<string, string> _translations = null;
        public Dictionary<string, string> Translations { get { return _translations; } set { _translations = value; } }

        private Rooms _rooms = null;
        public Rooms Rooms { get { return _rooms; } internal set { _rooms = value; } }

        public Story(RPCController rpc, Dictionary<string, string> translations)
        {
            _rpc = rpc;
            _translations = translations;
        }

        public Story(RPCController rpc, ulong id, Dictionary<string, string> translations)
        {
            _rpc = rpc;
            _id = id;
            _translations = translations;
        }

        public void Dispose()
        {

        }

        public void AddRoom(Room room)
        {
            if (room.ID == 0) return;
            _rpc.AddRoomToStory(this, room);
            _rooms._dictionary.Add(room.ID, room);
        }

        public void RemoveRoom(Room room)
        {
            if (room.ID == 0) return;
            _rpc.RemoveRoomFromStory(this, room);
            _rooms._dictionary.Remove(room.ID);
        }

        public bool HasName(string name)
        {
            foreach (var translation in _translations)
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
    }
}
