using System;

namespace HomegearLib
{
    public class HomegearException : Exception
    {
        public HomegearException() : base()
        {

        }

        public HomegearException(string message) : base(message)
        {

        }
    }

    public class HomegearVariableException : Exception
    {
        public HomegearVariableException() : base()
        {

        }

        public HomegearVariableException(string message) : base(message)
        {

        }
    }

    public class HomegearVariableTypeException : HomegearVariableException
    {
        public HomegearVariableTypeException() : base()
        {

        }

        public HomegearVariableTypeException(string message) : base(message)
        {

        }
    }

    public class HomegearVariableReadOnlyException : HomegearVariableException
    {
        public HomegearVariableReadOnlyException() : base()
        {

        }

        public HomegearVariableReadOnlyException(string message) : base(message)
        {

        }
    }

    public class HomegearVariableValueOutOfBoundsException : HomegearVariableException
    {
        public HomegearVariableValueOutOfBoundsException() : base()
        {

        }

        public HomegearVariableValueOutOfBoundsException(string message) : base(message)
        {

        }
    }

    public class HomegearRPCServerException : HomegearException
    {
        public HomegearRPCServerException() : base()
        {

        }

        public HomegearRPCServerException(string message) : base(message)
        {

        }
    }

    public class HomegearRPCServerSSLException : HomegearRPCServerException
    {
        public HomegearRPCServerSSLException() : base()
        {

        }

        public HomegearRPCServerSSLException(string message)
            : base(message)
        {

        }
    }

    public class HomegearRPCClientException : HomegearException
    {
        public HomegearRPCClientException() : base()
        {

        }

        public HomegearRPCClientException(string message)
            : base(message)
        {

        }
    }

    public class HomegearRPCClientSSLException : HomegearRPCClientException
    {
        public HomegearRPCClientSSLException() : base()
        {

        }

        public HomegearRPCClientSSLException(string message) : base(message)
        {

        }
    }
}
