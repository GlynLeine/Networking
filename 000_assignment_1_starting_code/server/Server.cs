using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Linq;
using shared;

namespace server
{
    class Server
    {
        TcpListener listener;
        List<string> todisconnect = new List<string>();

        Mutex clientListMutex = new Mutex();
        Dictionary<string, Client> clients = new Dictionary<string, Client>();

        Dictionary<string, string> credentials = new Dictionary<string, string>();

        bool exit;
        Stopwatch stopwatch;


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

                //HandleNewClients();
                //UpdateClients();
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

                    NetworkStream stream = client.GetStream();
                    byte[] bytes = StreamUtil.Read(stream);
                    string message = Encoding.UTF8.GetString(bytes);

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
                            else
                                parameters = new string[] { parameter };
                        }

                        if (command == "nickname" || command == "nick")
                        {
                            string nick = parameters[0];
                            nick = nick.ToLower();
                            if (client.name == nick)
                            {
                                StreamUtil.Write(client.GetStream(), Encoding.UTF8.GetBytes("Nickname " + nick + " is already your name."));
                                continue;
                            }
                            else if (credentials.ContainsKey(nick))
                            {
                                StreamUtil.Write(client.GetStream(), Encoding.UTF8.GetBytes("Nickname " + nick + " is already taken."));
                                continue;
                            }

                            clients.Remove(client.name);
                            clients.Add(nick, client);

                            i--;

                            StreamUtil.Write(client.GetStream(), Encoding.UTF8.GetBytes("Changed nickname to: " + nick));
                            Console.WriteLine("User " + client.name + " changed their nickname to " + nick);
                            client.name = nick;
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
                    }
                    else
                    {
                        StreamUtil.Write(stream, Encoding.UTF8.GetBytes("You said: " + message));
                        message = client.name + " says: " + message;
                        Console.WriteLine(message);

                        foreach (var other in clients)
                        {
                            if (other.Key == client.name)
                                continue;
                            other.Value.WriteString(message);
                        }
                    }
                }

                //Although technically not required, now that we are no longer blocking, 
                //it is good to cut your CPU some slack
                //Thread.Sleep(100);
            }
        }

        public void DisconnectClient(Client client)
        {
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
