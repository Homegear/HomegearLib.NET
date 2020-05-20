using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomegearLib
{
    public class Buildings : ReadOnlyDictionary<ulong, Building>, IDisposable
    {
        RPCController _rpc = null;

        public Buildings(RPCController rpc, Dictionary<ulong, Building> buildings) : base(buildings)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<ulong, Building> building in _dictionary)
            {
                building.Value.Dispose();
            }
        }

        public void Reload()
        {
            _rpc.Buildings = null;
            _dictionary = _rpc.Buildings;
        }

        public void Create(Building building)
        {
            _rpc.CreateBuilding(building);
        }

        public Building ByName(string name)
        {
            foreach (var building in _dictionary)
            {
                if (building.Value.HasName(name)) return building.Value;
            }
            return null;
        }
    }
}
