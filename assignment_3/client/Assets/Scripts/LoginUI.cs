using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField username;
    public TMP_InputField password;

    public UnityClient client;

    public void Login()
    {
        client.LoginToServer(username.text, password.text);
    }
}
