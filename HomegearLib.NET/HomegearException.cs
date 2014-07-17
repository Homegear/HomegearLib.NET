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

    public class HomegearObjectReadOnlyException : Exception
    {
        public HomegearObjectReadOnlyException() : base()
        {

        }

        public HomegearObjectReadOnlyException(String message) : base(message)
        {

        }
    }

    public class HomegearVariableException : Exception
    {
        public HomegearVariableException() : base()
        {

        }

        public HomegearVariableException(String message) : base(message)
        {

        }
    }

    public class HomegearVariableTypeException : HomegearVariableException
    {
        public HomegearVariableTypeException() : base()
        {

        }

        public HomegearVariableTypeException(String message) : base(message)
        {

        }
    }

    public class HomegearVariableReadOnlyException : HomegearVariableException
    {
        public HomegearVariableReadOnlyException() : base()
        {

        }

        public HomegearVariableReadOnlyException(String message) : base(message)
        {

        }
    }

    public class HomegearVariableWriteOnlyException : HomegearVariableException
    {
        public HomegearVariableWriteOnlyException() : base()
        {

        }

        public HomegearVariableWriteOnlyException(String message) : base(message)
        {

        }
    }

    public class HomegearVariableValueOutOfBoundsException : HomegearVariableException
    {
        public HomegearVariableValueOutOfBoundsException() : base()
        {

        }

        public HomegearVariableValueOutOfBoundsException(String message) : base(message)
        {

        }
    }
}
