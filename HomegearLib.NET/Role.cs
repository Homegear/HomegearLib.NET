using HomegearLib.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomegearLib
{
    public class Role : IDisposable
    {
        internal RPCController _rpc = null;

        private ulong _id = 0;

        public ulong ID { get { return _id; } set { _id = value; } }

        private Dictionary<string, string> _translations = null;
        public Dictionary<string, string> Translations { 
            get { 
                return _translations; 
            } 
            set { 
                _translations = value;
                _rpc?.UpdateRole(this);
            } 
        }

        public int Level
        {
            get
            {
                if ((_id / 10000) * 10000 == _id) return 0;
                else if ((_id / 100) * 100 == _id) return 1;
                else return 2;
            }
        }

        public Role(RPCController rpc, Dictionary<string, string> translations)
        {
            _rpc = rpc;
            _translations = translations;
        }

        public Role(RPCController rpc, ulong id, Dictionary<string, string> translations)
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

        public Dictionary<string, RPCVariable> GetVariablesInRole()
        {
            return _rpc?.GetVariablesInRole(this);
        }

        public bool AddRoleToVariable(Variable variable, Variable.RoleElementDirection direction, bool invert)
        {
            if (_rpc == null) return false;

            Variable.RoleElement roleElement = new Variable.RoleElement
            {
                ID = ID,
                Direction = direction,
                Invert = invert
            };

            return _rpc.AddRoleToVariable(variable, roleElement);
        }

        public bool RemoveRoleFromVariable(Variable variable)
        {
            if (_rpc == null) return false;

            return _rpc.RemoveRoleFromVariable(variable, this);
        }
    }
}
