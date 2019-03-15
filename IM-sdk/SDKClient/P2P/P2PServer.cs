using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SDKClient.P2P
{
    class P2PServer
    {
        CustomProtocolServer CustomProtocolServer = new CustomProtocolServer();
        private IPAddress IPAddress;
        public SuperSocket.SocketBase.ServerState ServerState
        {
            get
            {
                return CustomProtocolServer.State;
            }
        }

        private void Init(IPAddress iPEndPoint)
        {
            CustomProtocolServer = new CustomProtocolServer();
            bool result = false;
            int port = 10086;
            while (!result)
            {
                CustomProtocolServer = new CustomProtocolServer();
                
                if (CustomProtocolServer.Setup(IPAddress.ToString(), port))
                {
                    
                    result = CustomProtocolServer.Start();
                    if (!result)
                    {
                        port++;
                        //TODO:会出现端口资源紧张的情况么
                    }
                    else
                        break;
                }
            }

        }

        public  void Start(IPAddress iPEndPoint)
        {
            IPAddress = iPEndPoint;
            if (CustomProtocolServer.State != SuperSocket.SocketBase.ServerState.NotInitialized)
                return;
            else
            {
                Stop();
                Init(IPAddress);
            }
        }
        public  void Stop()
        {
            CustomProtocolServer.Stop();
        }
        private  string LocalIP()
        {
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            string ip = null;
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    ip = ipa.ToString();
                }

            }
            return ip;
        }
        public string GetLocalIP()
        {
            return CustomProtocolServer.Config.Ip;
        }

        public  int GetLocalPort()
        {
            return CustomProtocolServer.Config.Port;
        }
       

     

    }
}
