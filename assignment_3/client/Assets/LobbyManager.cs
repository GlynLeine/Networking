using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using shared;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public PanelWrapper panelWrapper;
    public UnityClient client;
    public TMP_Text readyButtonText;


    public void Ready()
    {
        client.Write(new Ready());
    }

    // Start is called before the first frame update
    void Start()
    {
        Message.messageHandler += ReceiveMessage;
        panelWrapper.OnChatTextEntered += WriteMessage;
        Disconnection.onOtherDisconnect += OnOtherDisconnect;
        gameObject.SetActive(false);
    }

    private void OnOtherDisconnect(ClientData data)
    {
        panelWrapper.AddOutput(data.username + " disconnected.");
    }

    void WriteMessage(string message)
    {
        client.Write(new Message(client.Id, message));
        panelWrapper.AddOutput("You said: " + message);
        panelWrapper.ClearInput();
    }

    void ReceiveMessage(uint id, string message)
    {
        panelWrapper.AddOutput(message);
    }
}
