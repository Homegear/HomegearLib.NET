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

    public class HomegearRpcClientException : HomegearException
    {
        public HomegearRpcClientException() : base()
        {

        }

        public HomegearRpcClientException(string message)
            : base(message)
        {

        }
    }

    public class HomegearRpcClientSSLException : HomegearRpcClientException
    {
        public HomegearRpcClientSSLException() : base()
        {

        }

        public HomegearRpcClientSSLException(string message) : base(message)
        {

        }
    }

    public class HomegearBinaryRpcException : HomegearException
    {
        public HomegearBinaryRpcException() : base()
        {

        }

        public HomegearBinaryRpcException(string message)
            : base(message)
        {

        }
    }
}
