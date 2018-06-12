using HomegearLib;
using HomegearLib.RPC;
using System;
using System.Security.Authentication;
using System.Threading;

namespace Events
{
    class Program
    {
        static ManualResetEvent _connectedEvent = new ManualResetEvent(false);
        static string cloudmaticHost = "34672.meine-homematic.de";
        static string cloudmaticUsername = "easymate_eddy";
        static string cloudmaticPassword = "19edsc89";

        static string cloudmaticHostCom = "com.easy-smarthome.de";

        static void Main(string[] args)
        {
            #region With SSL support

            var sslClientInfo = new SSLClientInfo(cloudmaticUsername, cloudmaticPassword);

            var rpc = new RPCController(cloudmaticHostCom, 4431);

            #endregion

            Homegear homegear = new Homegear(rpc, true);

            homegear.ConnectError += homegear_ConnectError;
            homegear.Reloaded += homegear_Reloaded;
            homegear.SystemVariableUpdated += homegear_SystemVariableUpdated;
            homegear.DeviceVariableUpdated += homegear_DeviceVariableUpdated;
            homegear.MetadataUpdated += homegear_MetadataUpdated;
            homegear.DeviceConfigParameterUpdated += homegear_DeviceConfigParameterUpdated;
            homegear.DeviceLinkConfigParameterUpdated += homegear_DeviceLinkConfigParameterUpdated;

            Console.WriteLine("Connecting to Homegear...");
            _connectedEvent.WaitOne();

            if (!rpc.IsConnected)
            {
                Console.WriteLine("Exiting...");
                homegear.Dispose();
                Environment.Exit(1);
            }

            Console.WriteLine("Press 'q' to exit program.");
            Thread.Sleep(2000);

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
            } while (key.KeyChar != 'q');

            homegear.Dispose();
        }

        static void homegear_DeviceConfigParameterUpdated(Homegear sender, Device device, Channel channel, ConfigParameter parameter)
        {
            System.Diagnostics.Debug.WriteLine("Config parameter updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Parameter Name: \"" + parameter.Name + "\", Value: " + parameter.ToString());
        }

        static void homegear_DeviceLinkConfigParameterUpdated(Homegear sender, Device device, Channel channel, Link link, ConfigParameter parameter)
        {
            Console.WriteLine("Link config parameter updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Remote Peer: " + link.RemotePeerID.ToString() + ", Remote Channel: " + link.RemoteChannel.ToString() + ", Parameter Name: \"" + parameter.Name + "\", Value: " + parameter.ToString());
        }

        static void homegear_MetadataUpdated(Homegear sender, Device device, MetadataVariable variable)
        {
            Console.WriteLine("Metadata updated: Device: " + device.ID.ToString() + ", Value: " + variable.ToString());
        }

        static void homegear_SystemVariableUpdated(Homegear sender, SystemVariable variable)
        {
            Console.WriteLine("System variable updated: Value: " + variable.ToString());
        }

        static void homegear_DeviceVariableUpdated(Homegear sender, Device device, Channel channel, Variable variable)
        {
            Console.WriteLine("Variable updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Variable Name: \"" + variable.Name + "\", Value: " + variable.ToString());
        }

        static void homegear_ConnectError(Homegear sender, string message, string stackTrace)
        {
            Console.WriteLine("Error connecting to Homegear: " + message);
            _connectedEvent.Set();
        }

        static void homegear_Reloaded(Homegear sender)
        {
            Console.WriteLine("Connected. Got " + sender.Devices.Count.ToString() + " devices.");
            _connectedEvent.Set();
        }


    }
}
