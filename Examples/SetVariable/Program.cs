using HomegearLib;
using HomegearLib.RPC;
using System;
using System.Threading;

namespace SetVariable
{
    class Program
    {
        static ManualResetEvent _connectedEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.Write("Please enter the hostname or IP address of your server running Homegear: ");
            string homegearHost = Console.ReadLine();

            #region Without SSL support
            RPCController rpc = new RPCController
                                (
                                    homegearHost,   //Hostname of your server running Homegear
                                    2001            //Port Homegear listens on
                                );
            #endregion

            #region With SSL support
            /*
            SSLClientInfo sslClientInfo = new SSLClientInfo
                                            (
                                                "MyComputer",   //Hostname of the computer your program runs on.
                //This hostname is used for certificate verification.
                                                "user",
                                                "secret",
                                                true            //Enable certificate verification
                                            );
            //You can create the certificate file with: openssl pkcs12 -export -inkey YourPrivateKey.key -in YourCA.pem -in YourPublicCert.pem -out MyCertificate.pfx
            SSLServerInfo sslServerInfo = new SSLServerInfo
                                            (
                                                "MyCertificate.pfx",    //Path to the certificate the callback server
                //will use.
                                                "secret",               //Certificate password
                                                "localUser",            //The username Homegear needs to use to connect
                //to our callback server
                                                "localSecret"           //The password Homegear needs to use to connect
                //to our callback server
                                            );
            RPCController rpc = new RPCController(homegearHost, 2003, "", "", -1, sslClientInfo, sslServerInfo);
            */
            #endregion

            Homegear homegear = new Homegear(rpc, false);

            homegear.ConnectError += homegear_ConnectError;
            homegear.Reloaded += homegear_Reloaded;

            Console.WriteLine("Connecting to Homegear...");
            _connectedEvent.WaitOne();

            if (!rpc.IsConnected)
            {
                Console.WriteLine("Exiting...");
                homegear.Dispose();
                Environment.Exit(1);
            }

            ConsoleKeyInfo key;
            do
            {
                ShowMenu();
                key = Console.ReadKey();
                Console.WriteLine();

                switch (key.KeyChar)
                {
                    case '1':
                        {
                            Console.Write("Please enter peer ID: ");
                            string peerIdString = Console.ReadLine();
                            Console.Write("Please enter channel: ");
                            string channelString = Console.ReadLine();
                            Console.Write("Please enter variable name: ");
                            string variableName = Console.ReadLine();

                            // {{{ Convert Strings to numbers
                            Int32 peerId = 0;
                            if (!Int32.TryParse(peerIdString, out peerId))
                            {
                                Console.WriteLine("Peer ID was not a number.");
                                break;
                            }

                            Int32 channel = 0;
                            if (!Int32.TryParse(channelString, out channel))
                            {
                                Console.WriteLine("Channel was not a number.");
                                break;
                            }
                            // }}}

                            // {{{ Get and print value
                            Variable variable;
                            try
                            {
                                variable = homegear.Devices[peerId].Channels[channel].Variables[variableName];
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.GetType().ToString() + ": " + ex.Message);
                                break;
                            }
                            Console.WriteLine("Value is: " + variable.ToString() + " " + variable.Unit);
                            // }}}
                        }
                        break;
                    case '2':
                        {
                            Console.Write("Please enter peer ID: ");
                            string peerIdString = Console.ReadLine();
                            Console.Write("Please enter channel: ");
                            string channelString = Console.ReadLine();
                            Console.Write("Please enter variable name: ");
                            string variableName = Console.ReadLine();
                            Console.Write("Please enter value: ");
                            string value = Console.ReadLine();

                            // {{{ Convert Strings to numbers
                            Int32 peerId = 0;
                            if (!Int32.TryParse(peerIdString, out peerId))
                            {
                                Console.WriteLine("Peer ID was not a number.");
                                break;
                            }

                            Int32 channel = 0;
                            if (!Int32.TryParse(channelString, out channel))
                            {
                                Console.WriteLine("Channel was not a number.");
                                break;
                            }
                            // }}}

                            // {{{ Set value
                            Variable variable;
                            try
                            {
                                variable = homegear.Devices[peerId].Channels[channel].Variables[variableName];
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.GetType().ToString() + ": " + ex.Message);
                                break;
                            }

                            switch (variable.Type)
                            {
                                case VariableType.tAction:
                                    Boolean actionValue = false;
                                    if (Boolean.TryParse(value, out actionValue)) variable.BooleanValue = actionValue;
                                    break;
                                case VariableType.tBoolean:
                                    Boolean booleanValue = false;
                                    if (Boolean.TryParse(value, out booleanValue)) variable.BooleanValue = booleanValue;
                                    break;
                                case VariableType.tInteger:
                                    Int32 integerValue = 0;
                                    if (Int32.TryParse(value, out integerValue)) variable.IntegerValue = integerValue;
                                    break;
                                case VariableType.tInteger64:
                                    Int64 integerValue64 = 0;
                                    if (Int64.TryParse(value, out integerValue64)) variable.IntegerValue64 = integerValue64;
                                    break;
                                case VariableType.tEnum:
                                    Int32 enumValue = 0;
                                    if (Int32.TryParse(value, out enumValue)) variable.IntegerValue = enumValue;
                                    break;
                                case VariableType.tDouble:
                                    Double doubleValue = 0;
                                    if (Double.TryParse(value, out doubleValue)) variable.DoubleValue = doubleValue;
                                    break;
                                case VariableType.tString:
                                    variable.StringValue = value;
                                    break;
                            }
                            // }}}
                        }
                        break;
                }
            } while (key.KeyChar != '0');

            homegear.Dispose();
        }

        static void ShowMenu()
        {
            Console.WriteLine();
            Console.WriteLine("1  Get value");
            Console.WriteLine("2  Set value");
            Console.WriteLine();
            Console.WriteLine("0  Exit program");
            Console.WriteLine();
            Console.Write("> ");
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
