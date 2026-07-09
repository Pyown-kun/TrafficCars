using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViolationManager : MonoBehaviour
{
    [Serializable]
    public class ViolationSetting
    {
        [Header("Detection")]
        public string tagName;
        public LayerMask layerMask;

        [Header("Penalty")]
        public string violationName = "Violation";
        public int penalty = 1000;
    }

    [Header("Violation Settings")]
    [SerializeField] private List<ViolationSetting> violationSettings = new();

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI totalPenaltyText;

    public int TotalPenalty { get; private set; }

    public event Action<int> OnPenaltyChanged;

    private void Start()
    {
        RefreshUI();
    }

    public bool TryAddViolation(GameObject target)
    {
        foreach (var setting in violationSettings)
        {
            bool tagMatched =
                !string.IsNullOrEmpty(setting.tagName) &&
                target.CompareTag(setting.tagName);

            bool layerMatched =
                ((1 << target.layer) & setting.layerMask.value) != 0;

            if (!tagMatched && !layerMatched)
                continue;

            AddPenalty(setting.penalty, setting.violationName);
            return true;
        }

        return false;
    }

    public void AddPenalty(int amount, string violationName)
    {
        TotalPenalty += amount;

        RefreshUI();

        Debug.Log(
            $"Violation : {violationName}\n" +
            $"Penalty : {amount}\n" +
            $"Total : {TotalPenalty}");

        OnPenaltyChanged?.Invoke(TotalPenalty);
    }

    public int GetPenalty(string violationName)
    {
        foreach (var setting in violationSettings)
        {
            if (setting.violationName == violationName)
                return setting.penalty;
        }

        return 0;
    }

    private void RefreshUI()
    {
        if (totalPenaltyText != null)
        {
            totalPenaltyText.text = $"{TotalPenalty:N0}";
        }
    }

    public bool TryAddViolation(string violationName)
    {
        foreach (var setting in violationSettings)
        {
            if (setting.violationName == violationName)
            {
                AddPenalty(setting.penalty, setting.violationName);
                return true;
            }
        }

        return false;
    }
}