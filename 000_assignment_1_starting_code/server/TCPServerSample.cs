using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Linq;
using server;

class TCPServerSample
{
    /**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */
    bool exit = false;

    public static void Main(string[] args)
    {
        TCPServerSample server = new TCPServerSample();

        Thread serverthread = new Thread(new ThreadStart(server.DoServerStuff));
        serverthread.Start();
        while (!server.exit)
        {
            string command = Console.ReadLine();
            server.exit = command == "exit";
        }

        serverthread.Join();
    }

    public void DoServerStuff()
    {
        Console.WriteLine("Server started on port 55555");

        TcpListener listener = new TcpListener(IPAddress.Any, 55555);
        listener.Start();

        List<string> todisconnect = new List<string>();

        Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        Dictionary<string, float> timeouts = new Dictionary<string, float>();

        Dictionary<string, string> credentials = new Dictionary<string, string>();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        float previousFrame = 0;
        while (!exit)
        {
            float currentTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
            float deltaTime = currentTime - previousFrame;
            previousFrame = currentTime;

            for (int i = 0; i < timeouts.Count; i++)
            {
                string name = timeouts.ElementAt(i).Key;
                timeouts[name] += deltaTime;
                if (timeouts[name] > 5f)
                {
                    todisconnect.Add(name);
                }
            }

            for (int i = 0; i < todisconnect.Count; i++)
            {
                if (i >= todisconnect.Count || i < 0)
                    continue;

                string name = todisconnect[i];

                timeouts.Remove(name);
                clients[name].Close();
                clients.Remove(name);
                todisconnect.RemoveAt(i);
                Console.WriteLine("Disconnected client: " + name);
            }

            todisconnect.Clear();

            //First big change with respect to example 001
            //We no longer block waiting for a client to connect, but we only block if we know
            //a client is actually waiting (in other words, we will not block)
            //In order to serve multiple clients, we add that client to a list
            while (listener.Pending())
            {

            }

            //Second big change, instead of blocking on one client, 
            //we now process all clients IF they have data available
            for (int i = 0; i < clients.Count; i++)
            {
                if (i >= clients.Count || i < 0)
                    continue;

                var element = clients.ElementAt(i);
                string name = element.Key;
                TcpClient client = element.Value;

                if (client.Available == 0) continue;
                NetworkStream stream = client.GetStream();
                byte[] bytes = StreamUtil.Read(stream);
                string message = Encoding.UTF8.GetString(bytes);

                if (message.StartsWith("/"))
                {
                    int paramToken = message.IndexOf(' ');
                    if (paramToken == 0)
                        return;

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
                        if(name == nick)
                        {
                            StreamUtil.Write(client.GetStream(), Encoding.UTF8.GetBytes("Nickname " + nick + " is already your name."));
                            continue;
                        }
                        else if (credentials.ContainsKey(nick))
                        {
                            StreamUtil.Write(client.GetStream(), Encoding.UTF8.GetBytes("Nickname " + nick + " is already taken."));
                            continue;
                        }

                        clients.Remove(name);
                        clients.Add(nick, client);

                        timeouts.Remove(name);
                        timeouts.Add(nick, 0f);

                        i--;

                        StreamUtil.Write(client.GetStream(), Encoding.UTF8.GetBytes("Changed nickname to: " + nick));
                        Console.WriteLine("User " + name + " changed their nickname to " + nick);
                    }
                    else if (command == "reconfirm")
                    {
                        timeouts[name] = 0f;
                        Console.WriteLine(name + " is still connected");
                    }
                    else
                    {
                        Console.WriteLine("Unknown command: " + command + " " + parameter);
                    }
                }
                else
                {
                    StreamUtil.Write(stream, Encoding.UTF8.GetBytes("You said: " + message));
                    message = name + " says: " + message;
                    Console.WriteLine(message);

                    foreach (var other in clients)
                    {
                        if (other.Key == name)
                            continue;
                        NetworkStream otherStream = other.Value.GetStream();
                        StreamUtil.Write(otherStream, Encoding.UTF8.GetBytes(message));
                    }
                }
            }

            //Although technically not required, now that we are no longer blocking, 
            //it is good to cut your CPU some slack
            //Thread.Sleep(100);
        }
    }
}


