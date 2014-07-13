using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace HomegearLib
{
    internal class HomegearRPCServerException : HomegearException
    {
        public HomegearRPCServerException() : base()
        {

        }

        public HomegearRPCServerException(String message) : base(message)
        {

        }
    }

    internal class HomegearRPCServerSSLException : HomegearRPCServerException
    {
        public HomegearRPCServerSSLException() : base()
        {

        }

        public HomegearRPCServerSSLException(String message) : base(message)
        {

        }
    }

    internal class RPCServer
    {
        X509Certificate _serverCertificate = null;

        RPCServer(string certificatePath = "")
        {
            if (File.Exists(certificatePath)) _serverCertificate = X509Certificate.CreateFromCertFile(certificatePath);
        }

        void Listen()
        {

        }
    }
}
