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
        Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        Dictionary<string, Client> clients = new Dictionary<string, Client>();

        Dictionary<string, string> credentials = new Dictionary<string, string>();

        bool exit;
        Stopwatch stopwatch;

        public Server()
        {
            rooms.Add("global", new Room("global"));
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

            if (command == "nickname" || command == "nick")
            {
                if (parameters == null)
                {
                    client.WriteString("0 parameters found, 1 expected");
                    return 0;
                }

                string nick = parameters[0];
                nick = nick.ToLower();

                ChangeNickName(client, nick);

                return -1;
            }
            else if (command == "ls" || command == "list")
            {
                if (client.room == null)
                    client.WriteString("You're currently not in a room.");
                else
                {
                    string list = "Users in room " + client.room.name + ":";

                    foreach (string name in client.room.clients.Keys)
                        list += "\n\t" + name;

                    client.WriteString(list);
                }
            }
            else if (command == "lsa" || command == "listall")
            {
                string list = "All users:";

                foreach (string name in clients.Keys)
                    list += "\n\t" + name;

                client.WriteString(list);
            }
            else if (command == "listrm" || command == "listrooms" || command == "lsrm")
            {
                string list = "Rooms:";

                foreach (string roomName in rooms.Keys)
                    list += "\n\t" + roomName;

                client.WriteString(list);
            }
            else if (command == "createroom" || command == "crm")
            {
                if (parameters == null)
                {
                    client.WriteString("0 parameters found, 1 or 2 expected");
                    return 0;
                }
                string roomName = parameters[0];

                if (rooms.ContainsKey(roomName))
                {
                    client.WriteString("Room " + roomName + " already exists.");
                    return 0;
                }

                string password = "";
                if (parameters.Length > 1)
                    password = parameters[1];

                Room room = new Room(client, roomName, password);
                rooms.Add(roomName, room);

                MoveClientToRoom(client, room, password);
            }
            else if (command == "join" || command == "joinroom")
            {
                if (parameters != null)
                {
                    if (!rooms.ContainsKey(parameters[0]))
                    {
                        client.WriteString("Room " + parameters[0] + " does not exist.");
                        return 0;
                    }

                    if (parameters.Length > 1)
                        MoveClientToRoom(client, rooms[parameters[0]], parameters[1]);
                    else
                        MoveClientToRoom(client, rooms[parameters[0]], "");
                }
                else
                    client.WriteString("0 parameters found, 1 or 2 expected");
            }
            else if (command == "reconfirm")
            {
                client.timeout = 0f;
                Console.WriteLine(client.name + " is still connected");
            }
            else
            {
                Console.WriteLine("Unknown command: " + command + " " + parameter);
            }
            return 0;
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
