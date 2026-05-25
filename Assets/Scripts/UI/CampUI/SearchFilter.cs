using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class SearchFilter : MonoBehaviour
{
    public TMP_InputField searchInput;

    void Awake()
    {
        searchInput.onValueChanged.AddListener(Filter);
    }

    void Filter(string searchText)
    {
        searchText = searchText.ToLower();

        foreach (CampItemEntry entry in CampManager.instance.CampItemEntries)
        {
            bool matches =
                entry.ItemName.text.ToLower().Contains(searchText);

            entry.gameObject.SetActive(matches);
        }

        foreach (CampAbilityEntry entry in CampManager.instance.CampAbilityEntries)
        {
            bool matches =
                entry.AbilityName.text.ToLower().Contains(searchText);

            entry.gameObject.SetActive(matches);
        }
    }
}
