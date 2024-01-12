using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Healthbars;
using UnityEngine.ProBuilder.MeshOperations;
using PluginConfig.API.Fields;

public enum DisplayMode
{
    Normalized,
    Accurate
}

public class Healthbar : MonoBehaviour
{
    protected static Dictionary<EnemyIdentifier, Healthbar> activeHealthbars;
    public TextMeshProUGUI label;
    public Image bar;
    public Image barBG;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI damageLabel;

    public RectTransform rectTransform;
    public EnemyIdentifier enemy;
    public Transform head;

    // change the buffer if we change how the bar looks!
    private const int BUFFER = 6;

    float timeSinceDamaged = 99.9f;
    float lastHealth = 0f;
    float maxHealth = 0f;
    float damage = 0f;
    float healthMultiplier = 1f;

    public void Start()
    {
        rectTransform = transform as RectTransform;
        lastHealth = enemy.health;
        maxHealth = enemy.health;
        healthMultiplier = enemy.totalHealthModifier;

        Plugin.useTotalHealth.onValueChange += UpdateHealthbarLength;

        // set size based on max health
        if (Plugin.useTotalHealth.value)
            rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Sqrt(maxHealth * healthMultiplier) * 25f), rectTransform.sizeDelta.y);
        else
            rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Sqrt(maxHealth) * 25f), rectTransform.sizeDelta.y);

        bar.rectTransform.sizeDelta = new Vector2(-BUFFER, bar.rectTransform.sizeDelta.y);
        barBG.rectTransform.sizeDelta = bar.rectTransform.sizeDelta;

        // whether damage label appear
        head = enemy.GetComponentInChildren<SeasonalHats>(includeInactive: true)?.transform;
        if (head == null)
            head = enemy.transform;
    }

    void Update()
    {
        timeSinceDamaged += Time.deltaTime;
        canvasGroup.alpha = Mathf.InverseLerp(4f, 3.5f, timeSinceDamaged);
        if (enemy == null || (enemy.health <= 0f && canvasGroup.alpha <= 0f))
        {
            Destroy(gameObject);
            return;
        }
        // heads can get destroyed?
        if (head == null)
            head = enemy.transform;

        // calc screen position
        Vector3 point = MonoSingleton<CameraController>.Instance.cam.WorldToScreenPoint(head.position + head.localScale.y * Vector3.up * 0.5f);
        if (point.z < 0f)
        {
            canvasGroup.alpha = 0f;
        }
        rectTransform.position = point + new Vector3(0, 12, 0);

        float newHealthMultiplier = enemy.totalHealthModifier;
        if (newHealthMultiplier != healthMultiplier)
        {
            healthMultiplier = newHealthMultiplier;

            // set size based on max health
            if (Plugin.useTotalHealth.value)
            {
                rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Sqrt(maxHealth * healthMultiplier) * 25f), rectTransform.sizeDelta.y);
                bar.rectTransform.sizeDelta = new Vector2(-BUFFER, bar.rectTransform.sizeDelta.y);
                barBG.rectTransform.sizeDelta = bar.rectTransform.sizeDelta;
            }
        }

        // update timeSinceDamaged
        if (lastHealth != enemy.health && (lastHealth > 0f || enemy.health > 0f))
        {
            float actualHealth = Mathf.Max(lastHealth, 0f);
            if (timeSinceDamaged <= 4f)
                damage += Mathf.Min(lastHealth - enemy.health, actualHealth) * healthMultiplier;
            else
                damage = Mathf.Min(lastHealth - enemy.health, actualHealth) * healthMultiplier;
            lastHealth = enemy.health;
            timeSinceDamaged = 0f;
        }

        // update damage label

        // update UI
        bar.fillAmount = enemy.health / maxHealth;
        float barBGHealth = barBG.fillAmount * maxHealth;
        barBG.fillAmount = Mathf.Max(bar.fillAmount, 
            Mathf.MoveTowards(barBG.fillAmount, bar.fillAmount, Time.deltaTime * Mathf.Lerp(1f, 5f, (barBGHealth - enemy.health)) / maxHealth)
        );
        switch (Plugin.displayMode.value)
        {
            case DisplayMode.Normalized:
                label.text = 
                    $"{Mathf.CeilToInt(Mathf.Max(enemy.health * 100f * healthMultiplier, 0f))}<space=0.25em><size=75%><color=#FFFA>{Mathf.CeilToInt(maxHealth * healthMultiplier * 100f)}";
                break;
            case DisplayMode.Accurate:
                string maxHealthStr = (Mathf.CeilToInt(maxHealth * 100f * healthMultiplier) / 100f).ToString();
                string healthStr = (Mathf.CeilToInt(Mathf.Max(enemy.health * 100f * healthMultiplier, 0f)) / 100f).ToString("0.00");
                label.text = $"{healthStr}<space=0.25em><size=75%><color=#FFFA>{maxHealthStr}";
                break;
        }

        UpdateDamageLabel();

        switch (true)
        {
            case true when enemy.blessed:
                bar.color = Plugin.blessedColor.value;
                break;
            case true when enemy.isBoss:
                bar.color = Plugin.bossColor.value;
                break;
            default:
                bar.color = Plugin.normalColor.value;
                break;
        }
    }

    void UpdateDamageLabel()
    {
        switch (Plugin.displayMode.value)
        {
            case DisplayMode.Normalized:
                switch (Mathf.Sign(damage))
                {
                    case 1:
                        damageLabel.text = $"-{Mathf.FloorToInt(damage * 100f)}";
                        break;
                    case 0:
                        damageLabel.text = $"-0";
                        break;
                    case -1:
                        damageLabel.text = $"<color=#0F0>+{Mathf.FloorToInt(damage * -100f)}"; // -100 to convert it to positive.
                        break;
                }
                break;
            case DisplayMode.Accurate:
                switch (Mathf.Sign(damage))
                {
                    case 1:
                        damageLabel.text = $"-{Mathf.FloorToInt(damage * 100f) / 100f}";
                        break;
                    case 0:
                        damageLabel.text = $"-0";
                        break;
                    case -1:
                        damageLabel.text = $"<color=#0F0>+{Mathf.FloorToInt(damage * -100f) / 100f}"; // -100 to convert it to positive.
                        break;
                }
                break;
        }
    }

    void UpdateHealthbarLength(BoolField.BoolValueChangeEvent data)
    {
        // set size based on max health
        if (data.value)
        {
            rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Sqrt(maxHealth * healthMultiplier) * 25f), rectTransform.sizeDelta.y);
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Sqrt(maxHealth) * 25f), rectTransform.sizeDelta.y);
        }
        bar.rectTransform.sizeDelta = new Vector2(-BUFFER, bar.rectTransform.sizeDelta.y);
        barBG.rectTransform.sizeDelta = bar.rectTransform.sizeDelta;
    }

    void OnDestroy()
    {
        Plugin.useTotalHealth.onValueChange -= UpdateHealthbarLength;
    }
}