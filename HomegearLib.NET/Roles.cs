using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomegearLib
{
    public class Roles : ReadOnlyDictionary<ulong, Role>, IDisposable
    {
        RPCController _rpc = null;

        public Roles(RPCController rpc, Dictionary<ulong, Role> roles) : base(roles)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
            foreach (KeyValuePair<ulong, Role> role in _dictionary)
            {
                role.Value.Dispose();
            }
        }

        public void Update(out bool rolesRemoved, out bool rolesAdded)
        {
            rolesRemoved = false;
            rolesAdded = false;
            Dictionary<ulong, Role> roles = _rpc.GetRoles();
            foreach (KeyValuePair<ulong, Role> rolePair in roles)
            {
                if (!_dictionary.ContainsKey(rolePair.Key))
                {
                    rolesAdded = true;
                    continue;
                }
                Role role = _dictionary[rolePair.Value.ID];
                role.Translations = rolePair.Value.Translations;
            }
            foreach (KeyValuePair<ulong, Role> rolePair in _dictionary)
            {
                if (!roles.ContainsKey(rolePair.Key))
                {
                    rolesRemoved = true;
                    break;
                }
            }
        }
    }
}
