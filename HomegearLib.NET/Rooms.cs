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

        // This will need reloading things that contain rooms, directly or indirectly
        // the room won't be removed from all collections just by calling this
        // use it carefully
        public void Delete(Room room)
        {
            if (_dictionary.ContainsKey(room.ID))
            {
                _rpc.DeleteRoom(room);
                _dictionary.Remove(room.ID);
            }
        }
    }
}
