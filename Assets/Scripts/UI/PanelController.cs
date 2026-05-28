using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelController : MonoBehaviour
{
    private Animator animator;
    public bool IsPanelOpened = false;


    public virtual void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void ActivatePanel()
    {
        if (IsPanelOpened)
        {
            TogglePanel(false);
        }
        else
        {
            TogglePanel(true);
        }
    }

    private void TogglePanel(bool value)
    {
        animator.SetBool("IsOpened", value);
        IsPanelOpened = value;
    }
}
