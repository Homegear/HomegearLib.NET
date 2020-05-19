using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomegearLib
{
    public class Rooms : ReadOnlyDictionary<ulong, Room>, IDisposable
    {
        RPCController _rpc = null;

        public Rooms(RPCController rpc, Dictionary<ulong, Room> rooms) : base(rooms)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<ulong, Room> room in _dictionary)
            {
                room.Value.Dispose();
            }
        }

        public void Update(out bool roomsRemoved, out bool roomsAdded)
        {
            roomsRemoved = false;
            roomsAdded = false;
            Dictionary<ulong, Room> rooms = _rpc.GetRooms();
            foreach (KeyValuePair<ulong, Room> roomPair in rooms)
            {
                if (!_dictionary.ContainsKey(roomPair.Key))
                {
                    roomsAdded = true;
                    continue;
                }
                Room room = _dictionary[roomPair.Value.ID];
                room.Translations = roomPair.Value.Translations;
            }
            foreach (KeyValuePair<ulong, Room> roomPair in _dictionary)
            {
                if (!rooms.ContainsKey(roomPair.Key))
                {
                    roomsRemoved = true;
                    break;
                }
            }
        }

        public void Create(Room room)
        {
            _rpc.CreateRoom(room);
        }

        public Room ByName(string name)
        {
            foreach(var room in _dictionary)
            {
                if (room.Value.HasName(name)) return room.Value;
            }
            return null;
        }
    }
}
