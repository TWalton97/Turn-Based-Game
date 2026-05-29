using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityNamePresentationManager : MonoBehaviour
{
    public static AbilityNamePresentationManager instance;
    public TextMeshProUGUI AbilityNameText;
    public Animator Animator;
    public Image AbilityNamePanel;
    public RectTransform rectTransform;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void DisplayName(BaseAbility ability)
    {
        rectTransform.localEulerAngles = new Vector3(rectTransform.localEulerAngles.x, rectTransform.localEulerAngles.y, Random.Range(-2.5f, 2.5f));
        StartCoroutine(FadeToValue(0f, 1f));
        AbilityNameText.text = ability.AbilityName;
        Animator.SetTrigger("DisplayName");
        Invoke(nameof(DisablePanel), 2f);
    }

    private IEnumerator FadeToValue(float startValue, float endValue)
    {
        float elapsedTime = 0f;
        Color color = AbilityNamePanel.color;
        while (elapsedTime < 0.15f)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startValue, endValue, elapsedTime / 0.15f);
            AbilityNamePanel.color = new Color(color.r, color.g, color.b, newAlpha);
            yield return null;
        }
    }

    private void DisablePanel()
    {
        StartCoroutine(FadeToValue(1f, 0f));
    }
}
