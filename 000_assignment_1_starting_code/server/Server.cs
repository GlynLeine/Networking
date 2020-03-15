using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace server
{
    class Server
    {
        TcpListener listener;
        List<string> todisconnect = new List<string>();
        Dictionary<string, Client> clients = new Dictionary<string, Client>();

        Dictionary<string, string> credentials = new Dictionary<string, string>();


        public void Start()
        {

        }

        public void HandleNewClients()
        {
            Client client = new Client(listener.AcceptTcpClient());

            
            stream = client.GetStream();
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
                }

                if (command == "login" && parameters.Length == 2)
                {
                    if (credentials.ContainsKey(parameters[0]))
                    {
                        if (credentials[parameters[0]] == parameters[1])
                        {
                            if (clients.ContainsKey(parameters[0]))
                            {
                                clients[parameters[0]].Close();
                                clients.Remove(parameters[0]);
                            }
                            clients.Add(parameters[0], client);
                            StreamUtil.Write(stream, Encoding.UTF8.GetBytes("/accept"));
                            Console.WriteLine("Accepted known client " + parameters[0] + '.');
                            StreamUtil.Write(stream, Encoding.UTF8.GetBytes("Welcome " + parameters[0] + "!"));

                        }
                        else
                        {
                            StreamUtil.Write(stream, Encoding.UTF8.GetBytes("Incorrect password, you've been disconnected from the server"));
                            Console.WriteLine("Incorrect password for user: " + parameters[0]);
                            client.Close();
                        }
                    }
                    else
                    {
                        StreamUtil.Write(stream, Encoding.UTF8.GetBytes("Credentials unknown, do you want to make a new account? Please type \"yes\""));
                        byte[] confirmationBytes = StreamUtil.Read(stream);
                        string confirmationMessage = Encoding.UTF8.GetString(confirmationBytes);
                        if (confirmationMessage == "yes")
                        {
                            credentials.Add(parameters[0], parameters[1]);
                            StreamUtil.Write(stream, Encoding.UTF8.GetBytes("/accept"));
                            clients.Add(parameters[0], client);
                            Console.WriteLine("Accepted new client " + parameters[0] + '.');
                            StreamUtil.Write(stream, Encoding.UTF8.GetBytes("Welcome " + parameters[0] + "!"));
                            timeouts.Add(parameters[0], 0f);
                        }
                        else
                        {
                            Console.WriteLine("Refused new account");
                            StreamUtil.Write(stream, Encoding.UTF8.GetBytes("/disconnect"));
                            client.Close();
                        }
                    }
                }
                else
                {
                    StreamUtil.Write(stream, Encoding.UTF8.GetBytes("Something went wrong, you've been disconnected from the server"));
                    Console.WriteLine("unknown command " + command + parameter);
                    client.Close();
                }
            }
        }

        public void UpdateClients()
        {

        }

        public void DisconnectClient(uint clientId)
        {

        }
    }
}
