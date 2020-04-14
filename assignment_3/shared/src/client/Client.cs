using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace shared
{
    public enum PacketAction
    {
        resolved = 1, unresolved = 2, irrelevant = 1
    }

    public class Client
    {
        private TcpClient tcp;

        float timeoutThreshold;
        float timeout;

        //public uint id = 0;

        public object data;

        Mutex packetBufferMutex = new Mutex();
        List<Packet> packetBuffer = new List<Packet>();

        public delegate PacketAction OnPacketReceived(Client client, Packet packet);
        public delegate void OnConnectionTimeout(Client client);
        public delegate void OnDisconnect(Client client, string message);

        public OnPacketReceived onPacketReceived;
        public OnConnectionTimeout onConnectionTimeout;
        public OnDisconnect onDisconnect;

        float pingBuffer = 0;
        public bool autoPing = false;
        public float pingInterval = 4;

        public Client(TcpClient tcpClient)
        {
            timeout = 0;
            timeoutThreshold = 5;
            tcp = tcpClient;
        }

        public Client(bool pinging = false, float interval = 4)
        {
            autoPing = pinging;
            pingInterval = interval;
            timeout = 0;
            timeoutThreshold = 5;
            tcp = new TcpClient();
        }

        public string ip
        {
            get
            {
                try
                {
                    return tcp.Client.RemoteEndPoint.ToString();
                }
                catch (Exception e)
                {
                    return "ip unavailable";
                }
            }
        }

        public bool connected => tcp.Connected;

        public bool timedout => timeout >= timeoutThreshold;

        public bool Connect(string hostname, int port)
        {
            try
            {
                tcp.Connect(hostname, port);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public void SetTimeout(float threshold) => timeoutThreshold = threshold;

        public int Available => tcp.Available;

        public void UpdateAll(float deltaTime)
        {
            RetrievePackets();
            UpdateTimeout(deltaTime);
        }

        public void UpdateTimeout(float deltaTime)
        {
            timeout += deltaTime;
            if (autoPing)
            {
                pingBuffer += deltaTime;
                if (pingBuffer >= pingInterval)
                {
                    pingBuffer -= pingInterval;
                    Write(new Ping());
                }
            }

            if (timeout >= timeoutThreshold)
                onConnectionTimeout?.Invoke(this);
        }

        public void RetrievePackets()
        {
            packetBufferMutex.WaitOne();
            try
            {
                while (tcp.Available > 0)
                {
                    timeout = 0;
                    packetBuffer.Add(ReadPacket());
                }
            }
            catch (ObjectDisposedException e)
            {
                Close();
                onDisconnect?.Invoke(this, e.Message);
            }

            if (onPacketReceived != null)
            {
                var packets = packetBuffer.ToArray();
                for (int i = 0; i < packets.Length; i++)
                {
                    Packet packet = packets[i];
                    int discard = 2;

                    foreach (OnPacketReceived invocation in onPacketReceived.GetInvocationList())
                        discard = Math.Min(discard, (int)invocation(this, packet));

                    if (discard == 1)
                        packetBuffer.Remove(packet);
                }
            }
            packetBufferMutex.ReleaseMutex();
        }

        public Packet ReadPacket()
        {
            return new Packet(ReadBytes());
        }

        public byte[] ReadBytes()
        {
            try
            {
                return StreamUtil.Read(GetStream());
            }
            catch (Exception e)
            {
                Close();
                onDisconnect?.Invoke(this, e.Message);
                return null;
            }
        }

        public void Write(ISerializable serializable)
        {
            Packet packet = new Packet();
            packet.Write(serializable);
            try
            {
                packet.Send(GetStream());
            }
            catch (Exception e)
            {
                Close();
                onDisconnect?.Invoke(this, e.Message);
            }
        }

        public void Write(byte[] data)
        {
            try
            {
                StreamUtil.Write(GetStream(), data);
            }
            catch (Exception e)
            {
                Close();
                onDisconnect?.Invoke(this, e.Message);
            }
        }

        public string ReadString()
        {
            return Encoding.UTF8.GetString(ReadBytes());
        }

        public void WriteString(string data)
        {
            Write(Encoding.UTF8.GetBytes(data));
        }

        public void Close()
        {
            try
            {
                tcp.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Connection already closed.");
            }
        }

        public NetworkStream GetStream()
        {
            return tcp.GetStream();
        }

    }
}
