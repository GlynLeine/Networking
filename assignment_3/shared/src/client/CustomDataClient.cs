using System.Net.Sockets;

namespace shared
{
    public class CustomDataClient<T> : Client
    {
        public CustomDataClient(T data, bool pinging = false, float interval = 4) : base(pinging, interval)
        {
            base.data = data;
        }

        public CustomDataClient(T data, TcpClient tcp) : base(tcp)
        {
            base.data = data;
        }

        public new T data { get { return (T)base.data; } set { base.data = value; } }

        public static implicit operator T(CustomDataClient<T> client)
        {
            return client.data;
        }
    }
}
