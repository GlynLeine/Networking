using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace shared
{
    public struct ClientData
    {
        public uint id;
        public string username;
    }

    public class Server
    {
        TcpListener listener;

        Mutex clientListMutex = new Mutex();
        Dictionary<uint, CustomDataClient<ClientData>> clients = new Dictionary<uint, CustomDataClient<ClientData>>();

        Dictionary<string, string> credentials = new Dictionary<string, string>();

        List<Client> unregisteredClients = new List<Client>();

        bool exit;
        public bool hidePing = true;
        Stopwatch stopwatch;

        public bool relax = true;

        public Server()
        {

        }

        public void Run()
        {
            Console.WriteLine("Server started on port 55555");

            listener = new TcpListener(IPAddress.Any, 55555);
            listener.Start();

            Thread connectionThread = new Thread(new ThreadStart(GetNewClients));
            connectionThread.Start();

            Thread updateThread = new Thread(new ThreadStart(UpdateClients));
            updateThread.Start();

            while (!exit)
            {
                string command = Console.ReadLine();
                command = command.ToLower();
                exit = command == "exit";
                if (command == "show pinging")
                    hidePing = false;
                else if (command == "hide pinging")
                    hidePing = true;
                else if (command == "relax on" || command == "relax")
                    relax = true;
                else if (command == "relax off")
                    relax = false;

                if (relax)
                    Thread.Sleep(100);
            }

            connectionThread.Join();
            updateThread.Join();
        }

        private void GetNewClients()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            float previousFrame = 0;

            while (!exit)
            {
                float currentTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                float deltaTime = currentTime - previousFrame;
                previousFrame = currentTime;

                if (listener.Pending())
                {
                    Client client = new CustomDataClient<ClientData>(new ClientData(), listener.AcceptTcpClient());
                    Console.WriteLine("new client connected from ip: " + client.ip);
                    unregisteredClients.Add(client);
                    client.onPacketReceived += HandleClientRegistration;
                }

                var pendingClients = unregisteredClients.ToArray();
                for (int i = 0; i < pendingClients.Length; i++)
                {
                    Client client = pendingClients[i];

                    client.UpdateAll(deltaTime);

                    if (!client.connected || client.timedout)
                    {
                        unregisteredClients.Remove(client);
                        DisconnectClient(client, "Client timed out or was externally disconnected");
                    }
                }

                if (relax)
                    Thread.Sleep(100);
            }
        }

        public uint GetId(string username)
        {
            uint hash = 0x811c9dc5;
            uint prime = 0x1000193;

            for (int i = 0; i < username.Length; ++i)
            {
                char value = username[i];
                hash = hash ^ value;
                hash *= prime;
            }

            return hash;
        }

        private PacketAction HandleClientRegistration(Client baseClient, Packet packet)
        {
            CustomDataClient<ClientData> client = baseClient as CustomDataClient<ClientData>;
            Protocol protocol = packet.Read<Protocol>();
            if (protocol == null)
                return PacketAction.irrelevant;
            if (protocol is Connection)
            {
                Connection connection = protocol as Connection;

                if (credentials.ContainsKey(connection.Username))
                {
                    if (credentials[connection.Username] != connection.PassHash)
                    {
                        client.Write(new Login(0, "wrong password"));
                        return PacketAction.resolved;
                    }
                }
                else
                {
                    client.Write(new Login(0, "unknown user"));
                    return PacketAction.resolved;
                }

                ClientData data = client.data;
                data.id = GetId(connection.Username);
                client.data = data;

                if (clients.ContainsKey(client.data.id))
                {
                    DisconnectClient(client, "Account already logged in.");
                }
                else
                {
                    client.Write(new Login(client.data.id));

                    List<ClientData> clientData = new List<ClientData>();
                    foreach (var otherClient in clients.Values)
                        clientData.Add(otherClient);

                    client.Write(new UserList(clientData));

                    Console.WriteLine("Registered new connection with client: " + client.data.id);
                    client.onPacketReceived += HandleClientPacket;
                    client.onConnectionTimeout += HandleClientTimeout;
                    client.onDisconnect += HandleExternalDisconnect;

                    clientListMutex.WaitOne();
                    clients.Add(client.data.id, client);
                    clientListMutex.ReleaseMutex();
                }

                client.onPacketReceived -= HandleClientRegistration;
                unregisteredClients.Remove(client);

                return PacketAction.resolved;
            }
            //else if(protocol is something)
            //{

            //}
            return PacketAction.irrelevant;
        }

        private void HandleClientTimeout(Client client)
        {
            clients.Remove((client as CustomDataClient<ClientData>).data.id);
            DisconnectClient(client, "Client timeout.");
        }

        private PacketAction HandleClientPacket(Client client, Packet packet)
        {
            Protocol protocol = packet.Read<Protocol>();
            if (protocol != null)
            {
                protocol.Execute(this, client);
                return PacketAction.resolved;
            }

            return PacketAction.irrelevant;
        }

        public void Broadcast(Protocol protocol, Func<object, bool> selector = null)
        {
            foreach (var client in clients.ToArray())
            {
                if (selector != null)
                    if (!selector(client.Value.data))
                        continue;

                client.Value.Write(protocol);
            }
        }

        public void UpdateClientData(ClientData data)
        {
            if (!clients.ContainsKey(data.id))
                return;

            ClientData original = clients[data.id].data;
            original.username = data.username;

            clients[data.id].data = original;

            Broadcast(new UpdateData(data), (object clientData)=>{ return data.id != ((ClientData)clientData).id; });
        }

        private void UpdateClients()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            float previousFrame = 0;

            while (!exit)
            {
                float currentTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                float deltaTime = currentTime - previousFrame;
                previousFrame = currentTime;

                var clientList = clients.Values.ToArray();
                for (int i = 0; i < clientList.Length; i++)
                    clientList[i].UpdateAll(deltaTime);

                if (relax)
                    Thread.Sleep(100);
            }
        }

        private void HandleExternalDisconnect(Client client, string message)
        {
            client.onDisconnect -= HandleExternalDisconnect;
            clients.Remove((client as CustomDataClient<ClientData>).data.id);
            DisconnectClient(client);
        }

        public void DisconnectClient(Client client, string message = "")
        {
            client.Write(new Disconnection(message));
            Console.WriteLine("Disconnected client " + (client as CustomDataClient<ClientData>).data.id + " " + client.ip + " for reason:\n\t" + message);
            client.Close();

            Broadcast(new Disconnection((client as CustomDataClient<ClientData>).data.id));
        }
    }
}