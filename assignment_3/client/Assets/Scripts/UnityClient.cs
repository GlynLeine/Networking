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

    private CustomDataClient<ClientData> client;

    public void RegisterToServer(string username, string password, string passwordRepeat)
    {

    }

    public void LoginToServer(string username, string password)
    {
        client.Write(new Connection(username, password));
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
            client.Write(new UpdateData(client.data));
        }
        else
            Debug.Log(message);
    }

    private void OnOtherDisconnect(uint id)
    {

    }

    private void OnServerDisconnect(Client client, string message)
    {
        Debug.Log("Disconnected for reason: " + message);
        onDisconnect?.Invoke();
        connectToServer();
    }

    private void Start()
    {
        Console.SetOut(new DebugLogWriter());
        Login.onLogin += OnServerLogin;
        UserList.onUserListReceived += OnUserListReceived;
        UpdateData.onClientDataUpdate += OnClientDataUpdate;
        Disconnection.onOtherDisconnect += OnOtherDisconnect;
        connectToServer();
    }

    private void OnClientDataUpdate(ClientData data)
    {
        
    }

    public void OnUserListReceived(List<ClientData> clientData)
    {

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

    // RECEIVING CODE

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
