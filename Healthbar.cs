﻿using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Healthbars;
using UnityEngine.ProBuilder.MeshOperations;

public class Healthbar : MonoBehaviour
{
    public TextMeshProUGUI label;
    public Image bar;
    public Image barBG;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI damageLabel;

    public RectTransform rectTransform;
    public EnemyIdentifier enemy;
    public Transform head;

    public Color bossColor = new(1f, 0.28f, 0.28f);
    public Color normalColor = new(1, 0.82f, 0.28f);

    float timeSinceDamaged = 99.9f;
    float lastHealth = 0f;
    float maxHealth = 0f;
    float damage = 0f;

    public void Start()
    {
        rectTransform = transform as RectTransform;
        lastHealth = enemy.health;
        maxHealth = enemy.health;

        // change the buffer if we change how the bar looks!
        const int BUFFER = 6;

        // set size based on max health
        rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Mathf.Sqrt(maxHealth) * 25f), rectTransform.sizeDelta.y);
        bar.rectTransform.sizeDelta = new Vector2(-BUFFER, bar.rectTransform.sizeDelta.y);
        barBG.rectTransform.sizeDelta = bar.rectTransform.sizeDelta;

        // whether damage label appear
        head = enemy.GetComponentInChildren<SeasonalHats>(includeInactive: true)?.transform;
        if (head == null)
            head = enemy.transform;

        if (enemy.isBoss)
            bar.color = bossColor;
        else
            bar.color = normalColor;
    }

    void Update()
    {
        canvasGroup.alpha = Mathf.InverseLerp(4f, 3.5f, timeSinceDamaged);
        if (enemy == null || (enemy.health <= 0f && canvasGroup.alpha <= 0f))
        {
            Destroy(gameObject);
            return;
        }
        Vector3 point = MonoSingleton<CameraController>.Instance.cam.WorldToScreenPoint(head.position + head.localScale.y * head.up * 0.5f);
        if (point.z < 0f)
        {
            canvasGroup.alpha = 0f;
        }
        rectTransform.position = point + new Vector3(0, 12, 0);
        
        // update timeSinceDamaged
        if (lastHealth != enemy.health && enemy.health > 0f)
        {
            if (timeSinceDamaged <= 4f)
                damage += Mathf.Min(lastHealth - enemy.health, enemy.health);
            else
                damage = Mathf.Min(lastHealth - enemy.health, enemy.health);
            lastHealth = enemy.health;
            timeSinceDamaged = 0f;
        }

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

        // update UI
        bar.fillAmount = enemy.health / maxHealth;
        barBG.fillAmount = Mathf.Max(bar.fillAmount, 
            Mathf.MoveTowards(barBG.fillAmount, bar.fillAmount, Time.deltaTime * Mathf.Lerp(0.1f, 0.3f, barBG.fillAmount - bar.fillAmount))
        );
        label.text = $"{Mathf.CeilToInt(Mathf.Max(enemy.health * 100f, 0f))}<space=0.25em><size=75%><color=#FFFA>{Mathf.CeilToInt(maxHealth * 100f)}";
        timeSinceDamaged += Time.deltaTime;

    }
}