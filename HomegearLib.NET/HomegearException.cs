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

    public class HomegearVariableValueOutOfBoundsException : HomegearVariableException
    {
        public HomegearVariableValueOutOfBoundsException() : base()
        {

        }

        public HomegearVariableValueOutOfBoundsException(String message) : base(message)
        {

        }
    }

    public class HomegearRPCServerException : HomegearException
    {
        public HomegearRPCServerException() : base()
        {

        }

        public HomegearRPCServerException(String message) : base(message)
        {

        }
    }

    public class HomegearRPCServerSSLException : HomegearRPCServerException
    {
        public HomegearRPCServerSSLException() : base()
        {

        }

        public HomegearRPCServerSSLException(String message)
            : base(message)
        {

        }
    }

    public class HomegearRPCClientException : HomegearException
    {
        public HomegearRPCClientException() : base()
        {

        }

        public HomegearRPCClientException(String message)
            : base(message)
        {

        }
    }

    public class HomegearRPCClientSSLException : HomegearRPCClientException
    {
        public HomegearRPCClientSSLException() : base()
        {

        }

        public HomegearRPCClientSSLException(String message) : base(message)
        {

        }
    }
}
