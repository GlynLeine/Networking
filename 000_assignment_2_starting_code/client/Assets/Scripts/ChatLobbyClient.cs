using shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

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

public class ChatLobbyClient : MonoBehaviour
{
    private AvatarAreaManager _avatarAreaManager;
    private PanelWrapper _panelWrapper;

    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    private CustomDataClient<ClientData> client;

    public TMP_InputField username;
    public TMP_InputField password;

    public float spawnRange = 10;
    public float spawnMinAngle = 0;
    public float spawnMaxAngle = 180;

    public void Login()
    {
        client.Write(new Connection(username.text, password.text));
        Debug.Log("Attempting login with username: " + username.text + " and password: " + password.text);
    }

    public void OnRegistration(uint acceptedId, string message)
    {
        if (acceptedId != 0)
        {
            ClientData data = client.data;
            data.id = acceptedId;
            client.data = data;
            Debug.Log("Registration accepted with id: " + acceptedId);
            username.transform.parent.gameObject.SetActive(false);
            _panelWrapper.gameObject.SetActive(true);
            SpawnAvatar(client.data).ShowRing(true);
            client.Write(new UpdateData(client.data));
        }
        else
            Debug.Log(message);
    }

    private void OnOtherDisconnect(uint id)
    {
        if (_avatarAreaManager.HasAvatarView(id))
            _avatarAreaManager.RemoveAvatarView(id);
    }

    private void OnDisconnect(Client client, string message)
    {
        Debug.Log("Disconnected for reason: " + message);
        _panelWrapper.gameObject.SetActive(false);

        foreach (uint id in _avatarAreaManager.GetAllAvatarIds())
            _avatarAreaManager.RemoveAvatarView(id);

        connectToServer();
    }

    private void Start()
    {
        Console.SetOut(new DebugLogWriter());
        SimpleMessage.messageHandler += ShowMessage;
        Registration.onRegistration += OnRegistration;
        UserList.onUserListReceived += SpawnAvatars;
        UpdateData.onClientDataUpdate += OnClientDataUpdate;
        Disconnection.onOtherDisconnect += OnOtherDisconnect;
        connectToServer();

        _avatarAreaManager = FindObjectOfType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += OnAvatarAreaClicked;

        _panelWrapper = FindObjectOfType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += OnChatTextEntered;
        _panelWrapper.gameObject.SetActive(false);
    }

    public void ChangeSkin()
    {
        AvatarView avatarView = _avatarAreaManager.GetAvatarView(client.data.id);
        ClientData data = client.data;
        data.skinId = (uint)UnityEngine.Random.Range(0, 1000);
        client.data = data;
        avatarView.SetSkin((int)client.data.skinId);

        client.Write(new UpdateData(client.data));
    }

    private void OnClientDataUpdate(ClientData data)
    {
        if (_avatarAreaManager.HasAvatarView(data.id))
        {
            AvatarView avatarView = _avatarAreaManager.GetAvatarView(data.id);
            avatarView.SetSkin((int)data.skinId);
            avatarView.Move(new Vector3(data.x, 0, data.y));
        }
        else
            SpawnAvatar(data);

    }

    public void SpawnAvatars(List<ClientData> clientData)
    {
        foreach (ClientData data in clientData)
            SpawnAvatar(data);
    }

    private AvatarView SpawnAvatar(ClientData clientData)
    {
        AvatarView avatarView = _avatarAreaManager.AddAvatarView(clientData.id);
        avatarView.transform.localPosition = new Vector3(clientData.x, 0, clientData.y);

        //set a random skin
        avatarView.SetSkin((int)clientData.skinId);
        return avatarView;
    }

    private Vector3 getRandomPosition()
    {
        //set a random position
        float randomAngle = UnityEngine.Random.Range(spawnMinAngle, spawnMaxAngle) * Mathf.Deg2Rad;
        float randomDistance = UnityEngine.Random.Range(0, spawnRange);
        return new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)) * randomDistance;
    }

    private void connectToServer()
    {
        client = new CustomDataClient<ClientData>(new ClientData(), true);
        ClientData data = client.data;
        data.skinId = (uint)UnityEngine.Random.Range(0, 1000);
        client.data = data;
        client.onDisconnect += OnDisconnect;
        client.onPacketReceived += OnPacketReceived;

        StartCoroutine(TryConnect());
    }

    IEnumerator TryConnect()
    {
        while (!client.Connect(_server, _port) && !client.connected)
        {
            Debug.Log("Failed to connect to server.");
            yield return new WaitForSeconds(1);
        }
        username.transform.parent.gameObject.SetActive(true);
        Debug.Log("Connected to server.");
    }


    private void OnAvatarAreaClicked(Vector3 pClickPosition)
    {
        Debug.Log("ChatLobbyClient: you clicked on " + pClickPosition);
        AvatarView avatarView = _avatarAreaManager.GetAvatarView(client.data.id);
        ClientData data = client.data;
        data.x = pClickPosition.x;
        data.y = pClickPosition.z;
        client.data = data;

        avatarView.Move(pClickPosition);
        client.Write(new UpdateData(client.data));

    }

    private void OnChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();
        if (pText.StartsWith("/whisper ") || pText.StartsWith("/w "))
            WriteWhisper(pText.Substring(pText.IndexOf(' ')));
        else
            WriteMessage(pText);
    }

    private void WriteWhisper(string message)
    {
        Whisper whisper = new Whisper(client.data.id, message);
        Debug.Log("Sending whisper:" + whisper);
        client.Write(whisper);
        _avatarAreaManager.GetAvatarView(client.data.id).Say(message);
    }

    private void WriteMessage(string pOutString)
    {
        SimpleMessage message = new SimpleMessage(client.data.id, pOutString);
        Debug.Log("Sending:" + message);
        client.Write(message);
        _avatarAreaManager.GetAvatarView(client.data.id).Say(pOutString);
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

    public void ShowMessage(uint source, string pText)
    {
        if (!_avatarAreaManager.HasAvatarView(source))
        {
            _avatarAreaManager.AddAvatarView(source);
        }
        _avatarAreaManager.GetAvatarView(source).Say(pText);
    }

}
