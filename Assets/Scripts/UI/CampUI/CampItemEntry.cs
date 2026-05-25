using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CampItemEntry : MonoBehaviour
{
    public ItemSO Item;
    public TextMeshProUGUI ItemName;

    public Button InspectButton;

    public void AssignItem(ItemSO item)
    {
        Item = item;
        ItemName.text = Item.ItemName;
    }
}
