using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Registrator : MonoBehaviour
{
    public TMP_InputField username;
    public TMP_InputField password;
    public TMP_InputField passwordRepeat;

    public UnityClient client;

    public void Register()
    {
        client.RegisterToServer(username.text, password.text, passwordRepeat.text);
    }
}
