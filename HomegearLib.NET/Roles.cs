using HomegearLib.RPC;
using System;
using System.Collections.Generic;

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

        public void Reload()
        {
            _rpc.Roles = null;
            _dictionary = _rpc.Roles;
        }

        public void Create(Role role)
        {
            role.ID = _rpc.CreateRole(role);
            _dictionary.Add(role.ID, role);
        }

        public void Delete(Role role)
        {
            _rpc.DeleteRole(role);
            _dictionary.Remove(role.ID);
        }

        public Role ByName(string name)
        {
            foreach (var role in _dictionary)
            {
                if (role.Value.HasName(name)) return role.Value;
            }
            return null;
        }
    }
}
