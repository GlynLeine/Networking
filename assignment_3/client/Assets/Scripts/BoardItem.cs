using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoardItem : MonoBehaviour
{
    public TMP_Text text;
    public Button button;

    bool free;

    public bool Free => free;

    public void Disable()
    {
        if (free)
            button.interactable = false;
    }

    public void Enable()
    {
        if (free)
            button.interactable = true;
    }

    public bool DoMove(string move)
    {
        if (!free)
            return false;

        Debug.Log("did move: " + move);

        text.text = move;
        button.interactable = false;
        free = false;
        return true;
    }

    public void Reset()
    {
        text.text = "";
        button.interactable = true;
        free = true;
    }
}
