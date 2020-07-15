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

        public void Reload()
        {
            _rpc.Rooms = null;
            _dictionary = _rpc.Rooms;
        }

        public void Create(Room room)
        {
            room.ID = _rpc.CreateRoom(room);
            if (room._rpc == null) room._rpc = _rpc;
            _dictionary.Add(room.ID, room);
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
