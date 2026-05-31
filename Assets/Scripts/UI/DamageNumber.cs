using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    public TMP_Text DamageText;

    public Vector3 velocity;
    public float gravity = -10f;
    public float lifeTime = 1.5f;
    public float fadeSpeed = 2f;

    public Color BlockedDamageColor;
    public Color PhysicalDamageColor;
    public Color FireDamageColor;
    public Color ColdDamageColor;
    public Color PsychicDamageColor;
    public Color HealColor;

    public void SetText(HitResult result)
    {
        transform.position += Random.insideUnitSphere * 0.3f;
        string text = Mathf.Abs(result.Damage).ToString();
        if (result.Crit)
            text += "!";

        DamageText.text = text;

        if (result.Blocked)
        {
            DamageText.color = BlockedDamageColor;
        }
        else
        {
            switch (result.DamageType)
            {
                case DamageType.Physical:
                    DamageText.color = PhysicalDamageColor;
                    break;
                case DamageType.Fire:
                    DamageText.color = FireDamageColor;
                    break;
                case DamageType.Cold:
                    DamageText.color = ColdDamageColor;
                    break;
                case DamageType.Psychic:
                    DamageText.color = PsychicDamageColor;
                    break;
                case DamageType.Heal:
                    DamageText.color = HealColor;
                    break;
            }
        }

        Initialize();
    }

    public void SetTextByString(string text)
    {
        transform.position += Random.insideUnitSphere * 0.3f;
        DamageText.text = text;

        Initialize();
    }

    public void Initialize()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        velocity = new Vector3(randomDir.x, 1f, randomDir.y) * Random.Range(1f, 1.5f);
    }

    private void Update()
    {
        velocity.y += gravity * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;

        lifeTime -= Time.deltaTime;

        // fade
        Color c = DamageText.color;
        c.a = Mathf.Lerp(c.a, 0, fadeSpeed * Time.deltaTime);
        DamageText.color = c;

        if (lifeTime <= 0)
            Destroy(gameObject);
    }

}
