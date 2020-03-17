using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using shared;

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
            name = "";
            timeout = 0;
            tcp = tcpClient;
        }

        public int Available => tcp.Available;

        public byte[] Read()
        {
            return StreamUtil.Read(GetStream());
        }

        public void Write(byte[] data)
        {
            StreamUtil.Write(GetStream(), data);
        }

        public string ReadString()
        {
            return Encoding.UTF8.GetString(Read());
        }

        public void WriteString(string data)
        {
            Write(Encoding.UTF8.GetBytes(data));
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
