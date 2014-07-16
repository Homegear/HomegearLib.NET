using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class HomegearException : Exception
    {
        public HomegearException() : base()
        {
            
        }

        public HomegearException(String message) : base(message)
        {

        }
    }

    public class HomegearReadOnlyException : Exception
    {
        public HomegearReadOnlyException() : base()
        {

        }

        public HomegearReadOnlyException(String message) : base(message)
        {

        }
    }
}
