using shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class DebugLogWriter : System.IO.TextWriter
{
    private StringBuilder buffer = new StringBuilder();

    public override void Flush()
    {
        Debug.Log(buffer.ToString());
        buffer.Length = 0;
    }

    public override void Write(string value)
    {
        buffer.Append(value);
        if (value != null)
        {
            var len = value.Length;
            if (len > 0)
            {
                var lastChar = value[len - 1];
                if (lastChar == '\n')
                {
                    Flush();
                }
            }
        }
    }

    public override void Write(char value)
    {
        buffer.Append(value);
        if (value == '\n')
        {
            Flush();
        }
    }

    public override void Write(char[] value, int index, int count)
    {
        Write(new string(value, index, count));
    }

    public override Encoding Encoding
    {
        get { return Encoding.Default; }
    }
    //public override void Write(string value)
    //{
    //    base.Write(value);
    //    Debug.Log(value);
    //}
    //public override Encoding Encoding
    //{
    //    get { return Encoding.UTF8; }
    //}
}

public class UnityClient : MonoBehaviour
{
    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    public UnityEvent onDisconnect;
    public UnityEvent onLogin;
    public UnityEvent onConnect;

    public PopUpManager popUpManager;

    private CustomDataClient<ClientData> client;

    public uint Id => client.data.id;
    public string UserName => client.data.username;

    public void Write(Protocol protocol)
    {
        if (protocol.Mode != ProtocolMode.read)
            client.Write(protocol);
    }

    public void RegisterToServer(string username, string password, string passwordRepeat)
    {
        if (password == passwordRepeat)
        {
            client.Write(new Registration(username, password));
            ClientData data = client.data;
            data.username = username;
            client.data = data;
        }
        else
            popUpManager.ShowPopUp("passwords don't match");
        Debug.Log("Attempting register with username: " + username + " and password: " + password);
    }

    public void LoginToServer(string username, string password)
    {
        client.Write(new Connection(username, password));
        ClientData data = client.data;
        data.username = username;
        client.data = data;
        Debug.Log("Attempting login with username: " + username + " and password: " + password);
    }

    public void OnServerLogin(uint acceptedId, string message)
    {
        if (acceptedId != 0)
        {
            ClientData data = client.data;
            data.id = acceptedId;
            client.data = data;
            Debug.Log("Login accepted with id: " + acceptedId);
            onLogin?.Invoke();
        }
        else
            popUpManager.ShowPopUp(message);
    }

    private void OnServerDisconnect(Client client, string message)
    {
        Debug.Log("Disconnected for reason: " + message);
        onDisconnect?.Invoke();
        popUpManager.ShowPopUp("Server disconnected");

        connectToServer();
    }

    private void Start()
    {
        Console.SetOut(new DebugLogWriter());
        Login.onLogin += OnServerLogin;
        connectToServer();
    }

    private void connectToServer()
    {
        client = new CustomDataClient<ClientData>(new ClientData(), true);
        client.onDisconnect += OnServerDisconnect;
        client.onPacketReceived += OnPacketReceived;
        IEnumerator TryConnect()
        {
            while (!client.Connect(_server, _port) && !client.connected)
            {
                Debug.Log("Failed to connect to server.");
                yield return new WaitForSeconds(1);
            }
            onConnect?.Invoke();
            Debug.Log("Connected to server.");
        }
        StartCoroutine(TryConnect());
    }

    private void Update()
    {
        client.UpdateAll(Time.deltaTime);
    }

    private PacketAction OnPacketReceived(Client client, Packet packet)
    {
        Protocol protocol = packet.Read<Protocol>();
        if (protocol == null)
            return PacketAction.irrelevant;

        protocol.Execute(client);

        return PacketAction.resolved;
    }

}
