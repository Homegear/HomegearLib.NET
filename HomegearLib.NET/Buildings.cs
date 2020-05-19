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

        public void Update(out bool buildingsRemoved, out bool buildingsAdded)
        {
            buildingsRemoved = false;
            buildingsAdded = false;
            Dictionary<ulong, Building> buildings = _rpc.GetBuildings();
            foreach (KeyValuePair<ulong, Building> buildingPair in buildings)
            {
                if (!_dictionary.ContainsKey(buildingPair.Key))
                {
                    buildingsAdded = true;
                    continue;
                }
                Building building = _dictionary[buildingPair.Value.ID];
                building.Translations = buildingPair.Value.Translations;
            }
            foreach (KeyValuePair<ulong, Building> buildingPair in _dictionary)
            {
                if (!buildings.ContainsKey(buildingPair.Key))
                {
                    buildingsRemoved = true;
                    break;
                }
            }
        }

        public void Create(Building building)
        {
            _rpc.CreateBuilding(building);
        }

        public Building BuildingByName(string name)
        {
            foreach (var building in _dictionary)
            {
                if (building.Value.HasName(name)) return building.Value;
            }
            return null;
        }
    }
}
