using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ClearOnSelect : MonoBehaviour
{
    public TMP_InputField inputField;
    public GameObject placeHolder;

    private void Awake()
    {
        inputField.onSelect.AddListener(ClearEntryText);

        inputField.onSelect.AddListener(_ => placeHolder.SetActive(false));
        inputField.onDeselect.AddListener(_ =>
        {
            if (string.IsNullOrEmpty(inputField.text))
                placeHolder.SetActive(true);
        });
    }

    public void ClearEntryText(string _)
    {
        inputField.text = "";
    }
}
