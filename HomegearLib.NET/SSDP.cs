using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HomegearLib
{
    public static class SSDP
    {
        public static HashSet<Tuple<string, int>> Search(int timeout = 5000)
        {
            EndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            EndPoint multicastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);

            DateTime startTime = DateTime.Now;
            byte[] buffer = new byte[10240];
            List<string> responses = new List<string>();
            int receivedBytes = 0;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(localEndPoint);
                byte[] message = Encoding.UTF8.GetBytes("M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:" + ((IPEndPoint)socket.LocalEndPoint).Port.ToString() + "\r\nMAN: \"ssdp:discover\"\r\nMX: " + (timeout / 1000).ToString() + "\r\nST: urn:schemas-upnp-org:device:basic:1\r\n\r\n");
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), IPAddress.Any));
                timeout += 500;
                socket.SendTimeout = 1000;
                socket.ReceiveTimeout = timeout + 1000;
                socket.SendTo(message, 0, message.Length, SocketFlags.None, multicastEndPoint);

                while (DateTime.Now.Subtract(startTime).TotalMilliseconds < timeout)
                {
                    try
                    {
                        receivedBytes = socket.ReceiveFrom(buffer, ref localEndPoint);
                    }
                    catch (SocketException ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                    }
                    if (receivedBytes > 0)
                    {
                        responses.Add(Encoding.UTF8.GetString(buffer, 0, receivedBytes));
                    }

                    timeout = timeout - (int)DateTime.Now.Subtract(startTime).TotalMilliseconds;
                    if (timeout < 0)
                    {
                        break;
                    }

                    socket.ReceiveTimeout = timeout;
                }

                socket.Close();
            }

            HashSet<Tuple<string, int>> devices = new HashSet<Tuple<string, int>>();

            foreach (string response in responses)
            {
                if (!string.IsNullOrEmpty(response))
                {
                    if (response.StartsWith("HTTP/1.1 200 OK"))
                    {
                        StringReader reader = new StringReader(response);
                        List<string> lines = new List<string>();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line != "")
                            {
                                lines.Add(line);
                            }
                        }
                        string host = lines.Where(lin => lin.ToLower().StartsWith("server:")).First();
                        string location = lines.Where(lin => lin.ToLower().StartsWith("location:")).First();
                        if (!string.IsNullOrEmpty(host) && host.StartsWith("Server: Homegear ") && !string.IsNullOrEmpty(location))
                        {
                            int startPos1 = location.IndexOf('/') + 2;
                            int endPos1 = location.LastIndexOf(':');
                            int startPos2 = endPos1 + 1;
                            int endPos2 = location.LastIndexOf('/');
                            if (startPos1 == -1 || endPos1 == -1 || startPos2 == -1 || endPos2 == -1 || (startPos1 - 2) == endPos2)
                            {
                                continue;
                            }

                            int port = -1;
                            if (!int.TryParse(location.Substring(startPos2, endPos2 - startPos2), out port))
                            {
                                continue;
                            }

                            devices.Add(new Tuple<string, int>(location.Substring(startPos1, endPos1 - startPos1), port));
                        }
                    }
                }
            }

            return devices;
        }
    }
}
