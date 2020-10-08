using HomegearLib.RPC;
using System;

namespace HomegearLib
{
    public class Management : IDisposable
    {
        RPCController _rpc = null;

        public Management(RPCController rpc)
        {
            _rpc = rpc;
        }

        public void Dispose()
        {
            _rpc = null;
        }

        public void UploadDeviceDescriptionFile(string filename, ref byte[] data, ulong familyID)
        {
            _rpc.ManagementUploadDeviceDescriptionFile(filename, ref data, familyID);
        }
    }
}
