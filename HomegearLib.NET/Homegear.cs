using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomegearLib
{
    public class Homegear
    {
        public void Test()
        {
            RPCClient client = new RPCClient("homegear", 2003, true, true, "temp", "!55Weltzeit");
            client.CallMethod("getDeviceInfo", new List<RPCVariable> { new RPCVariable(143) });
        }
    }
}
