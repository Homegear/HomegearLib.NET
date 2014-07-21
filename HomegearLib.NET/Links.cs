using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class Links : ReadOnlyDictionary<Int32, ReadOnlyDictionary<Int32, Link>>, IDisposable
    {
        public Links(Dictionary<Int32, ReadOnlyDictionary<Int32, Link>> links) : base(links)
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}
