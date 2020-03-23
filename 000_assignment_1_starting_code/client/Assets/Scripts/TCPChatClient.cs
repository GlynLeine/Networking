using shared;
using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections;

/**
 * Assignment 1 - Starting project.
 */

public class TCPChatClient : MonoBehaviour
{
    [SerializeField] private PanelWrapper _panelWrapper = null;
    [SerializeField] private string _hostname = "localhost";
    [SerializeField] private int _port = 55555;

    private TcpClient _client;

    private bool connected;
    private bool accepted;

    private string username;
    private string password;

    void Start()
    {
        _panelWrapper.OnChatTextEntered += onTextEntered;
    }

    IEnumerator reconfirm()
    {
        while (connected)
        {
            while (!accepted)
            {
                yield return new WaitForSeconds(1f);
            }

            try
            {
                StreamUtil.Write(_client.GetStream(), Encoding.UTF8.GetBytes("/reconfirm"));
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                connected = false;
                accepted = false;
                _client.Close();
                _panelWrapper.ClearOutput();
                _panelWrapper.AddOutput("Disconnected from server");
            }
            yield return new WaitForSeconds(4f);
        }
    }

    IEnumerator connectToServer()
    {
        while (!connected)
        {
            _panelWrapper.AddOutput("Connecting to server...");
            try
            {
                _client = new TcpClient();
                _client.Connect(_hostname, _port);

                connected = true;
                byte[] outBytes = Encoding.UTF8.GetBytes("/login " + username + " " + password);
                StreamUtil.Write(_client.GetStream(), outBytes);

                _panelWrapper.ClearOutput();
                _panelWrapper.AddOutput("Connected to server.");
                StartCoroutine(reconfirm());
            }
            catch (Exception e)
            {
                connected = false;
                accepted = false;
                _panelWrapper.AddOutput("Could not connect to server:");
                _panelWrapper.AddOutput(e.Message);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private void Update()
    {
        if (!connected)
            return;

        if (!_client.Connected)
        {
            connected = false;
            accepted = false;
            _client.Close();
            _panelWrapper.ClearOutput();
            _panelWrapper.AddOutput("Disconnected from server");
        }

        if (_client.Available == 0)
            return;

        byte[] inBytes = StreamUtil.Read(_client.GetStream());
        string inString = Encoding.UTF8.GetString(inBytes);

        if (inString.Length > 1)
            if (inString.StartsWith("/"))
            {
                int paramToken = inString.IndexOf(' ');
                if (paramToken == 0)
                    return;

                string command;
                string[] parameters = null;
                string parameter = "";

                if (paramToken < 0)
                    command = inString.Substring(1);
                else
                {
                    command = inString.Substring(1, paramToken - 1);
                    parameter = inString.Substring(paramToken + 1);
                    if (parameter.Contains(" "))
                        parameters = parameter.Split(' ');
                }

                if (command == "accept")
                {
                    accepted = true;
                    _panelWrapper.ClearOutput();
                    return;
                }
                else if (command == "disconnect")
                {
                    connected = false;
                    accepted = false;
                    _client.Close();
                    _panelWrapper.ClearOutput();
                    _panelWrapper.AddOutput("Disconnected from server");
                    return;
                }
            }

        _panelWrapper.AddOutput(inString);
    }

    private void onTextEntered(string pInput)
    {
        if (pInput == null || pInput.Length == 0) return;

        _panelWrapper.ClearInput();

        if (pInput.Length > 1)
            if (pInput.StartsWith("/"))
            {
                int paramToken = pInput.IndexOf(' ');
                if (paramToken == 0)
                    return;

                string command;
                string[] parameters = null;
                string parameter = "";

                if (paramToken < 0)
                    command = pInput.Substring(1);
                else
                {
                    command = pInput.Substring(1, paramToken - 1);
                    parameter = pInput.Substring(paramToken + 1);
                    if (parameter.Contains(" "))
                        parameters = parameter.Split(' ');
                    else
                        parameters = new string[] { parameter };
                }

                if (command == "login")
                {
                    if (connected)
                    {
                        _panelWrapper.AddOutput("You're already logged in.");
                        return;
                    }

                    if (parameters == null || parameters.Length != 2)
                    {
                        _panelWrapper.AddOutput("Username and/or password invalid.");
                        return;
                    }

                    username = parameters[0];
                    password = parameters[1];

                    if (username == "" || password == "")
                    {
                        _panelWrapper.AddOutput("Please login with valid username and password first.");
                        return;
                    }
                    StartCoroutine(connectToServer());
                    return;
                }
                else if (command == "help" || command == "h")
                {
                    if (!connected)
                    {
                        string message = "";
                        if (parameters == null)
                        {
                            message = "Commands:";

                            message += "\n\thelp";
                            message += "\n\tlogin";
                        }
                        else
                        {
                            if(parameters[0] == "login")
                            {
                                message = "\nThis command is used to log into the server or register a new account. the command expects 2 parameters. The first being your username and the second your password.";
                            }
                            else if(parameters[0] == "help" || parameters[0] == "h")
                            {
                                message = "\nYou just used this command... this command is used to get a list of all commands or get more information on a certain command.\nAliases: h";
                            }
                            else
                            {
                                message = "Command " + parameters[0] + " is unknown.";
                            }
                        }

                        _panelWrapper.AddOutput(message);
                    }
                }
                else if (command == "disconnect")
                {
                    if (!connected)
                    {
                        _panelWrapper.AddOutput("Can't disconnect without being connected");
                        return;
                    }

                    connected = false;
                    accepted = false;
                    _client.Close();
                    _panelWrapper.ClearOutput();
                    _panelWrapper.AddOutput("Disconnected from server");
                }
            }

        if (connected)
        {
            try
            {
                byte[] outBytes = Encoding.UTF8.GetBytes(pInput);
                StreamUtil.Write(_client.GetStream(), outBytes);
            }
            catch (Exception e)
            {
                _panelWrapper.ClearOutput();
                Debug.LogWarning(e.Message);
                connected = false;
                accepted = false;
                _client.Close();
                _panelWrapper.ClearOutput();
                _panelWrapper.AddOutput("Disconnected from server");
                _panelWrapper.AddOutput("Reconnecting...");
                StartCoroutine(connectToServer());
            }
        }
    }

}

