using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Toggle checkboxToggle;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI locationText;
    [SerializeField] private Image statusIndicator;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color completedColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color ongoingColor = new Color(1f, 0.71f, 0.77f);

    private CalendarEvent currentEvent;

    public event Action<string, bool> OnCheckboxToggled;

    public void Initialize(CalendarEvent evt)
    {
        currentEvent = evt;
        UpdateUI();

        if (checkboxToggle != null)
        {
            checkboxToggle.onValueChanged.AddListener(OnCheckboxChanged);
        }
    }

    private void UpdateUI()
    {
        if (currentEvent == null) return;

        if (timeText != null)
        {
            timeText.text = currentEvent.GetFormattedStartTime();
        }

        if (titleText != null)
        {
            titleText.text = currentEvent.title;
            
            if (currentEvent.isCompleted)
            {
                titleText.fontStyle = FontStyles.Strikethrough;
                titleText.color = completedColor;
            }
            else
            {
                titleText.fontStyle = FontStyles.Normal;
                titleText.color = normalColor;
            }
        }

        if (locationText != null)
        {
            if (!string.IsNullOrEmpty(currentEvent.location))
            {
                locationText.text = currentEvent.location;
                locationText.gameObject.SetActive(true);
            }
            else
            {
                locationText.gameObject.SetActive(false);
            }
        }

        if (checkboxToggle != null)
        {
            checkboxToggle.isOn = currentEvent.isCompleted;
        }

        UpdateStatusIndicator();
    }

    private void UpdateStatusIndicator()
    {
        if (statusIndicator == null) return;

        if (currentEvent.isCompleted)
        {
            statusIndicator.color = completedColor;
        }
        else if (currentEvent.IsOngoing)
        {
            statusIndicator.color = ongoingColor;
        }
        else
        {
            statusIndicator.color = normalColor;
        }
    }

    private void OnCheckboxChanged(bool isChecked)
    {
        if (currentEvent != null)
        {
            OnCheckboxToggled?.Invoke(currentEvent.id, isChecked);
            currentEvent.isCompleted = isChecked;
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        if (checkboxToggle != null)
        {
            checkboxToggle.onValueChanged.RemoveListener(OnCheckboxChanged);
        }
    }
}
