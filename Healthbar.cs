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

public enum SegmentMode
{
    Disabled,
    Per5Health,
#if PER_PHASE_SEGMENTING
    PerPhase
#endif
}
public class Healthbar : MonoBehaviour
{
    protected static Dictionary<EnemyIdentifier, Healthbar> activeHealthbars;

    public TextMeshProUGUI label;
    public HorizontalLayoutGroup barLayout;
    public BarSegment bar;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI damageLabel;
    public Image staminaBar;

    [HideInInspector]
    public bool setupStamina;
    public float[] segmentValues;
    public BarSegment[] barSegments;
    public EnemyType enemyType;
    //public MonoBehaviour enemyAI; // either we cast this or we make tons of fields for every type of enemy we want a stamina bar for.
    public RectTransform rectTransform;
    public EnemyIdentifier enemy;
    public Transform head;
    public float enemyHeight;

    // change the buffer if we change how the bar looks!
    private const int BUFFER = 6;

    float timeSinceDamaged = 99.9f;
    float lastHealth = 0f;
    float maxHealth = 0f;
    float damage = 0f;
    float healthMultiplier = 1f;
    float dpsFill = 1f;

    public void Start()
    {
        rectTransform = transform as RectTransform;
        enemyType = enemy.enemyType;

        if (enemyType == EnemyType.Drone && enemy.GetComponent<Mandalore>() != null)
            enemyType = EnemyType.Mandalore;

        lastHealth = enemy.health;
        maxHealth = enemy.health;

        healthMultiplier = enemy.totalHealthModifier;

        int difficulty = enemy.GetPrivateField<int>("difficulty");

        if (enemy.isBoss)
        {
            switch (difficulty) 
            {
                case 4:
                    healthMultiplier *= 1.5f;
                    break;
                case 5:
                    healthMultiplier *= 2f;
                    break;
            }
        }

        Collider collider = enemy.GetComponent<Collider>();
        enemyHeight = (collider.bounds.center - enemy.transform.position).y + collider.bounds.extents.y;
        Debug.Log(enemyHeight);

        Plugin.useTotalHealth.onValueChange += UpdateHealthbarLength;

        int heightOffset = -3;
        int barHeightOffset = -3 + Plugin.height.value;

        setupStamina = false;
        // set stamina bar invisible, etc.
        staminaBar.fillAmount = 0f;

        float[] segments = GetSegments();
        barSegments = new BarSegment[segments.Length];
        bar.SegmentHealth = segments[0];
        barSegments[0] = bar;

        for (int i = 1; i < segments.Length; i++)
        {
            barSegments[i] = Instantiate(bar.gameObject, bar.transform.parent).GetComponent<BarSegment>();
            barSegments[i].SegmentHealth = segments[i];
        }

        float baseBarWidth = 25f * Plugin.width.value;
        int totalBarOffset = heightOffset + barHeightOffset;

        // set size based on max health 
        if (Plugin.useTotalHealth.value)
            rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Pow(maxHealth * healthMultiplier, Plugin.widthExponent.value) * baseBarWidth), rectTransform.sizeDelta.y + totalBarOffset);
        else
            rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Pow(maxHealth, Plugin.widthExponent.value) * baseBarWidth), rectTransform.sizeDelta.y + totalBarOffset);

        (barLayout.transform as RectTransform).sizeDelta = new Vector2(-BUFFER, (barLayout.transform as RectTransform).sizeDelta.y + barHeightOffset);

        LayoutRebuilder.ForceRebuildLayoutImmediate(barLayout.transform as RectTransform);

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
        Vector3 point = MonoSingleton<CameraController>.Instance.cam.WorldToScreenPoint(GetHealthbarPosition());
        if (point.z < 0f)
        {
            canvasGroup.alpha = 0f;
        }

        int width = PostProcessV2_Handler.Instance.GetPrivateField<int>("width");
        int height = PostProcessV2_Handler.Instance.GetPrivateField<int>("height");

        point.x *= Screen.width;
        point.y *= Screen.height;
        point.x /= width;
        point.y /= height;

        rectTransform.position = point + new Vector3(0, 12, 0);

        float newHealthMultiplier = enemy.totalHealthModifier;
        if (newHealthMultiplier != healthMultiplier)
        {
            healthMultiplier = newHealthMultiplier;

            // set size based on max health
            if (Plugin.useTotalHealth.value)
            {
                rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Sqrt(maxHealth * healthMultiplier) * 25f), rectTransform.sizeDelta.y);
                (barLayout.transform as RectTransform).sizeDelta = new Vector2(-BUFFER, (barLayout.transform as RectTransform).sizeDelta.y);
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

        float healthTotal = 0f;
        float DPSHealthTotal = 0f;
        dpsFill = Mathf.Max(enemy.health,
            Mathf.MoveTowards(dpsFill, enemy.health, Time.deltaTime * Mathf.Lerp(1f, 10f, dpsFill - enemy.health))
        );
        foreach (BarSegment segment in barSegments)
        {
            segment.bar.fillAmount = Mathf.InverseLerp(healthTotal, healthTotal + segment.SegmentHealth, enemy.health);
            segment.DPSBar.fillAmount = Mathf.InverseLerp(healthTotal, healthTotal + segment.SegmentHealth, dpsFill);
            healthTotal += segment.SegmentHealth;
            DPSHealthTotal += segment.SegmentHealth;
        }

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
                foreach (BarSegment segment in barSegments)
                    segment.bar.color = Plugin.blessedColor.value;
                break;
            case true when enemy.isBoss:
                foreach (BarSegment segment in barSegments)
                    segment.bar.color = Plugin.bossColor.value;
                break;
            default:
                foreach (BarSegment segment in barSegments)
                    segment.bar.color = Plugin.normalColor.value;
                break;
        }
    }

    Vector3 GetHealthbarPosition()
    {
        return enemy.transform.position + enemyHeight * Vector3.up;
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
        (barLayout.transform as RectTransform).sizeDelta = new Vector2(-BUFFER, (barLayout.transform as RectTransform).sizeDelta.y);
    }

    void OnDestroy()
    {
        Plugin.useTotalHealth.onValueChange -= UpdateHealthbarLength;
    }

    /// <summary>
    /// A utility method to return how many segments (and of which lengths)<br><br></br></br>
    /// 
    /// Modders looking to extend this method are able to do so using a HarmonyPostfix.
    /// </summary>
    /// <returns>An array.<br><br></br></br>Its length is the amount of segments, each value is the amount of health belonging to that segment, and its sum should total to the BASE max health of the enemy.</returns>
    float[] GetSegments()
    {
        float[] segments;
        switch (Plugin.segmentedBars.value)
        {
            case SegmentMode.Per5Health:

                // instantiating too many bars is laggy and stupid so we should just disable segments at that point
                // setting max to 250 since no vanilla enemy should(?) have more health than that
                if (maxHealth > 250) 
                    return new float[] { maxHealth };

                segments = new float[Mathf.CeilToInt(maxHealth / 5.0f)];

                for (int i = 0; i < segments.Length - 1; i++)
                    segments[i] = 5.0f;
                segments[segments.Length - 1] = maxHealth % 5.0f <= 0 ? 5.0f : maxHealth % 5.0f;

                return segments;

            default:
            case SegmentMode.Disabled:
                return new float[] { maxHealth };
        }
    }
}