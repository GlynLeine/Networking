using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace server
{
    class Client
    {
        public TcpClient tcp;
        public string name;
        public Room room;
        public float timeout;

        public Client(TcpClient tcpClient)
        {
            tcp = tcpClient;
        }

        public void Close()
        {
            tcp.Close();
        }

        public NetworkStream GetStream()
        {
            return tcp.GetStream();
        }

    }
}
