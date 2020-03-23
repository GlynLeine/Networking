using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace server
{
    public class Server
    {
        TcpListener listener;
        List<string> todisconnect = new List<string>();
        List<string> todestroy = new List<string>();

        Mutex clientListMutex = new Mutex();
        public Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        public Dictionary<string, Client> clients = new Dictionary<string, Client>();

        Dictionary<string, string> credentials = new Dictionary<string, string>();

        bool exit;
        Stopwatch stopwatch;

        public Dictionary<string, Command> commands = new Dictionary<string, Command>();

        public Server()
        {
            rooms.Add("global", new Room("global"));
            RegisterCommand<Reconfirm>();
            RegisterCommand<Whisper>();
            RegisterCommand<List>();
            RegisterCommand<ListRooms>();
            RegisterCommand<JoinRoom>();
            RegisterCommand<ListAll>();
            RegisterCommand<CreateRoom>();
            RegisterCommand<NickName>();
            RegisterCommand<Help>();
            RegisterCommand<Disconnect>();
        }

        void RegisterCommand<CommandType>() where CommandType : Command
        {
            CommandType command = (CommandType)Activator.CreateInstance(typeof(CommandType), new object[] { this });
            commands.Add(command.GetName(), command);
        }

        public void Run()
        {
            Console.WriteLine("Server started on port 55555");

            listener = new TcpListener(IPAddress.Any, 55555);
            listener.Start();

            Thread connectionThread = new Thread(new ThreadStart(HandleNewClients));
            connectionThread.Start();

            Thread updateThread = new Thread(new ThreadStart(UpdateClients));
            updateThread.Start();

            while (!exit)
            {
                string command = Console.ReadLine();
                exit = command == "exit";
            }

            connectionThread.Join();
            updateThread.Join();
        }

        public void HandleNewClients()
        {
            while (!exit)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(100);
                    continue;
                }

                Client client = new Client(listener.AcceptTcpClient());

                string message = client.ReadString();

                if (message.StartsWith("/"))
                {
                    int paramToken = message.IndexOf(' ');
                    if (paramToken == 0)
                        continue;

                    string command;
                    string[] parameters = null;
                    string parameter = "";

                    if (paramToken < 0)
                        command = message.Substring(1);
                    else
                    {
                        command = message.Substring(1, paramToken - 1);
                        parameter = message.Substring(paramToken + 1);
                        if (parameter.Contains(" "))
                            parameters = parameter.Split(' ');
                    }

                    if (command == "login" && parameters.Length == 2)
                    {
                        if (credentials.ContainsKey(parameters[0]))
                        {
                            if (credentials[parameters[0]] == parameters[1])
                            {
                                clientListMutex.WaitOne();
                                if (clients.ContainsKey(parameters[0]))
                                {
                                    DisconnectClient(parameters[0]);
                                }
                                clients.Add(parameters[0], client);
                                clientListMutex.ReleaseMutex();

                                client.name = parameters[0];
                                client.WriteString("/accept");
                                Console.WriteLine("Accepted known client " + parameters[0] + '.');
                                client.WriteString("Welcome " + parameters[0] + "!");

                                MoveClientToRoom(client, rooms["global"], "");
                            }
                            else
                            {
                                client.WriteString("Incorrect password, you've been disconnected from the server");
                                Console.WriteLine("Incorrect password for user: " + parameters[0]);

                                DisconnectClient(client);
                            }
                        }
                        else
                        {
                            client.WriteString("Credentials unknown, do you want to make a new account? Please type \"yes\"");

                            bool processed = false;
                            while (!processed && !exit)
                            {
                                if (client.Available == 0)
                                    continue;

                                string confirmationMessage = client.ReadString();
                                if (confirmationMessage == "yes")
                                {
                                    credentials.Add(parameters[0], parameters[1]);

                                    clientListMutex.WaitOne();
                                    clients.Add(parameters[0], client);
                                    clientListMutex.ReleaseMutex();

                                    client.name = parameters[0];
                                    client.WriteString("/accept");
                                    Console.WriteLine("Accepted new client " + parameters[0] + '.');
                                    client.WriteString("Welcome " + parameters[0] + "!");

                                    MoveClientToRoom(client, rooms["global"], "");

                                    client.timeout = 0;
                                    processed = true;
                                }
                                else
                                {
                                    Console.WriteLine("Refused new account");
                                    DisconnectClient(client);
                                    processed = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        client.WriteString("Something went wrong, you've been disconnected from the server");
                        Console.WriteLine("unknown command " + command + parameter);
                        DisconnectClient(client);
                    }
                }
            }
        }

        public void UpdateClients()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            float previousFrame = 0;

            while (!exit)
            {
                float currentTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                float deltaTime = currentTime - previousFrame;
                previousFrame = currentTime;

                for (int i = 0; i < clients.Count; i++)
                {
                    Client client = clients.ElementAt(i).Value;
                    client.timeout += deltaTime;
                    if (client.timeout > 5f)
                    {
                        todisconnect.Add(client.name);
                    }
                }

                clientListMutex.WaitOne();
                for (int i = 0; i < todisconnect.Count; i++)
                {
                    string name = todisconnect[i];

                    DisconnectClient(name);
                }
                clientListMutex.ReleaseMutex();

                todisconnect.Clear();

                //Second big change, instead of blocking on one client, 
                //we now process all clients IF they have data available
                for (int i = 0; i < clients.Count; i++)
                {
                    if (i >= clients.Count || i < 0)
                        continue;

                    Client client = clients.ElementAt(i).Value;

                    if (client.Available == 0)
                        continue;

                    string message = client.ReadString();

                    if (message.StartsWith("/"))
                    {
                        i += HandleCommand(message, client);
                    }
                    else
                    {
                        client.WriteString("You said: " + message);
                        message = client.name + " says: " + message;

                        if (client.room != null)
                        {
                            foreach (var other in client.room.clients)
                            {
                                if (other.Key == client.name)
                                    continue;
                                other.Value.WriteString(message);
                            }
                            message += " in room " + client.room.name;
                        }

                        Console.WriteLine(message);
                    }
                }

                foreach (string roomName in todestroy)
                    rooms.Remove(roomName);

                //Although technically not required, now that we are no longer blocking, 
                //it is good to cut your CPU some slack
                //Thread.Sleep(100);
            }
        }

        public int HandleCommand(string message, Client client)
        {
            int paramToken = message.IndexOf(' ');
            if (paramToken == 0)
                return 0;

            string command;
            string[] parameters = null;
            string parameter = "";

            if (paramToken < 0)
                command = message.Substring(1);
            else
            {
                command = message.Substring(1, paramToken - 1).ToLower();
                parameter = message.Substring(paramToken + 1);
                if (parameter.Contains(" "))
                    parameters = parameter.Split(' ');
                else
                    parameters = new string[] { parameter };
            }

            foreach (Command commandObj in commands.Values)
            {
                if (commandObj.Identify(command))
                {
                    return commandObj.Execute(client, parameter, parameters);
                }
            }

            Console.WriteLine("Unknown command: " + command + " " + parameter);
            return 0;
        }

        public string[] GetClientList()
        {
            string[] clientNames = new string[clients.Keys.Count];
            clients.Keys.CopyTo(clientNames, 0);
            return clientNames;
        }

        public string[] GetRoomList()
        {
            string[] roomNames = new string[rooms.Keys.Count];
            rooms.Keys.CopyTo(roomNames, 0);
            return roomNames;
        }

        public void ChangeNickName(Client client, string name)
        {
            if (client.name == name)
            {
                client.WriteString("Nickname " + name + " is already your name.");
                return;
            }
            else if (credentials.ContainsKey(name))
            {
                client.WriteString("Nickname " + name + " is already taken.");
                return;
            }

            clients.Remove(client.name);
            clients.Add(name, client);

            if (client.room != null)
            {
                client.room.clients.Remove(client.name);
                client.room.clients.Add(name, client);
            }

            client.WriteString("Changed nickname to: " + name);
            Console.WriteLine("User " + client.name + " changed their nickname to " + name);
            client.name = name;
        }

        public void DisconnectClientFromRoom(Client client)
        {
            if (client.room == null)
                return;

            Room room = client.room;
            room.RemoveClient(client);
            if (room.clients.Count == 0 && room.owner != null)
                todestroy.Add(room.name);
        }

        public void MoveClientToRoom(Client client, Room room, string password)
        {
            if (room.password != password)
            {
                client.WriteString("Incorrect password");
                Console.WriteLine(client.name + " tried to join " + room.name + " with the wrong password.");
                return;
            }

            DisconnectClientFromRoom(client);

            room.AddClient(client);
            client.room = room;
        }

        public void DisconnectClient(Client client)
        {
            DisconnectClientFromRoom(client);

            client.WriteString("/disconnect");
            client.Close();

            clientListMutex.WaitOne();
            if (clients.ContainsKey(client.name))
                clients.Remove(client.name);
            clientListMutex.ReleaseMutex();

            Console.WriteLine("Disconnected client: " + client.name);
        }

        public void DisconnectClient(string name)
        {
            DisconnectClientFromRoom(clients[name]);

            try
            {
                clients[name].WriteString("/disconnect");
                clients[name].Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            clients.Remove(name);
            Console.WriteLine("Disconnected client: " + name);
        }
    }
}
