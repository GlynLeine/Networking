using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PopUpManager : MonoBehaviour
{
    public TMP_Text popUpText;
    public Animator popUpAnimator;

    Queue<string> popupQueue = new Queue<string>();

    public void ShowPopUp(string text)
    {
        if (popupQueue.Count > 0 || popUpAnimator.GetCurrentAnimatorStateInfo(0).IsName("PopUp"))
            popupQueue.Enqueue(text);
        else
        {
            popUpText.text = text;
            popUpAnimator.SetTrigger("Show");
        }
    }

    public void HidePopUp()
    {
        popUpAnimator.SetTrigger("Hide");
    }

    public void OnAnimationDone()
    {
        if(popupQueue.Count > 0 && popUpAnimator.GetCurrentAnimatorStateInfo(0).IsName("PopDown"))
        {
            popUpText.text = popupQueue.Dequeue();
            popUpAnimator.SetTrigger("Show");
        }
    }
}
