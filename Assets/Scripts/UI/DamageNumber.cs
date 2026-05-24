using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    public TMP_Text DamageText;

    public Vector3 velocity;
    public float gravity = -10f;
    public float lifeTime = 1.5f;
    public float fadeSpeed = 2f;

    public void SetText(float value)
    {
        transform.position += Random.insideUnitSphere * 0.3f;
        DamageText.text = Mathf.Abs(value).ToString();
        if (value > 0)
        {
            DamageText.color = Color.red;
        }
        else if (value == 0)
        {
            DamageText.color = Color.white;
        }
        else
        {
            DamageText.color = Color.green;
        }

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
