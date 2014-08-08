using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class Family : IDisposable
    {
        private Int32 _id = -1;
        public Int32 ID { get { return _id; } set { _id = value; } }

        private String _name = "";
        public String Name { get { return _name; } set { _name = value; } }

        private IReadOnlyList<String> _pairingMethods = (new List<String>()).AsReadOnly();
        public IReadOnlyList<String> PairingMethods { get { return _pairingMethods; } }
        internal void SetPairingMethods(IReadOnlyList<String> pairingMethods)
        {
            _pairingMethods = pairingMethods;
        }

        public Family(Int32 id, String name)
        {
            _id = id;
            _name = name;
        }

        public void Dispose()
        {
            
        }
    }
}
