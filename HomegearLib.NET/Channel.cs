using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class Channel : IDisposable
    {
        private Int32 _index;
        public Int32 Index { get { return _index; } }

        private Variables _variables;
        public Variables Variables { get { return _variables; } internal set { _variables = value; } }

        public Channel(Int32 index)
        {
            _index = index;
        }

        public void Dispose()
        {
            _variables.Dispose();
        }
    }
}
