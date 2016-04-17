using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomegearLib
{
    public static class SSDP
    {
        public static HashSet<Tuple<String, Int32>> Search(Int32 timeout = 5000)
        {
            EndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            EndPoint multicastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);

            DateTime startTime = DateTime.Now;
            byte[] buffer = new byte[10240];
            List<String> responses = new List<String>();
            Int32 receivedBytes = 0;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(localEndPoint);
                byte[] message = Encoding.UTF8.GetBytes("M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:" + ((IPEndPoint)socket.LocalEndPoint).Port.ToString() + "\r\nMAN: \"ssdp:discover\"\r\nMX: " + (timeout / 1000).ToString() + "\r\nST: urn:schemas-upnp-org:device:basic:1\r\n\r\n");
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), IPAddress.Any));
                timeout += 500;
                socket.SendTimeout = 1000;
                socket.ReceiveTimeout = timeout + 1000;
                socket.SendTo(message, 0, message.Length, SocketFlags.None, multicastEndPoint);

                while(DateTime.Now.Subtract(startTime).TotalMilliseconds < timeout)
                {
                    try
                    {
                        receivedBytes = socket.ReceiveFrom(buffer, ref localEndPoint);
                    }
                    catch (SocketException ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                    }
                    if (receivedBytes > 0) responses.Add(Encoding.UTF8.GetString(buffer, 0, receivedBytes));
                    timeout = timeout - (int)DateTime.Now.Subtract(startTime).TotalMilliseconds;
                    if (timeout < 0) break;
                    socket.ReceiveTimeout = timeout;
                }

                socket.Close();
            }

            HashSet<Tuple<String, Int32>> devices = new HashSet<Tuple<String, Int32>>();

            foreach(String response in responses)
            {
                if(!string.IsNullOrEmpty(response))
                {
                    if (response.StartsWith("HTTP/1.1 200 OK"))
                    {
                        StringReader reader = new StringReader(response);
                        List<String> lines = new List<String>();
                        String line;
                        while((line = reader.ReadLine()) != null)
                        {
                            if (line != "") lines.Add(line);
                        }
                        String host = lines.Where(lin => lin.ToLower().StartsWith("server:")).First();
                        String location = lines.Where(lin => lin.ToLower().StartsWith("location:")).First();
                        if (!string.IsNullOrEmpty(host) && host.StartsWith("Server: Homegear ") && !string.IsNullOrEmpty(location))
                        {
                            Int32 startPos1 = location.IndexOf('/') + 2;
                            Int32 endPos1 = location.LastIndexOf(':');
                            Int32 startPos2 = endPos1 + 1;
                            Int32 endPos2 = location.LastIndexOf('/');
                            if (startPos1 == -1 || endPos1 == -1 || startPos2 == -1 || endPos2 == -1 || (startPos1 - 2) == endPos2) continue;
                            Int32 port = -1;
                            if(!Int32.TryParse(location.Substring(startPos2, endPos2 - startPos2), out port)) continue;
                            devices.Add(new Tuple<String, Int32>(location.Substring(startPos1, endPos1 - startPos1), port));
                        }
                    }
                }
            }

            return devices;
        }
    }
}
