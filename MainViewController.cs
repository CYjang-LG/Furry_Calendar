using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainViewController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private Transform eventListContent;
    [SerializeField] private GameObject eventItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject emptyStatePanel;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Character")]
    [SerializeField] private GameObject miniCharacterPanel;

    private List<GameObject> eventItemInstances = new List<GameObject>();

    private void Start()
    {
        InitializeUI();
        SubscribeToEvents();
        LoadTodayEvents();
    }

    private void InitializeUI()
    {
        UpdateDateText();
        
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(OnRefreshClicked);
        }

        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }

        if (emptyStatePanel != null)
        {
            emptyStatePanel.SetActive(false);
        }
    }

    private void SubscribeToEvents()
    {
        if (EventDataManager.Instance != null)
        {
            EventDataManager.Instance.OnDataChanged += RefreshEventList;
        }

        if (GoogleCalendarAPI.Instance != null)
        {
            GoogleCalendarAPI.Instance.OnEventsLoaded += OnEventsLoaded;
            GoogleCalendarAPI.Instance.OnError += OnAPIError;
        }
    }

    private void UpdateDateText()
    {
        if (dateText != null)
        {
            dateText.text = System.DateTime.Now.ToString("yyyy년 MM월 dd일 dddd", 
                new System.Globalization.CultureInfo("ko-KR"));
        }
    }

    private void LoadTodayEvents()
    {
        if (GoogleOAuthManager.Instance != null && GoogleOAuthManager.Instance.IsAuthenticated)
        {
            ShowLoading(true);
            GoogleCalendarAPI.Instance.GetTodayEvents();
        }
        else
        {
            ShowEmptyState("로그인이 필요합니다.");
        }
    }

    private void OnRefreshClicked()
    {
        LoadTodayEvents();
        
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.PlayAnimation("Idle");
            CharacterManager.Instance.ShowRandomGreeting();
        }
    }

    private void OnEventsLoaded(List<CalendarEvent> events)
    {
        ShowLoading(false);
    }

    private void OnAPIError(string error)
    {
        ShowLoading(false);
        ShowEmptyState($"오류: {error}");
        Debug.LogError($"API 오류: {error}");
    }

    private void RefreshEventList()
    {
        ClearEventList();

        List<CalendarEvent> todayEvents = EventDataManager.Instance.GetTodayEvents();

        if (todayEvents.Count == 0)
        {
            ShowEmptyState("오늘 일정이 없습니다.");
            return;
        }

        HideEmptyState();

        foreach (var evt in todayEvents)
        {
            CreateEventItem(evt);
        }
    }

    private void CreateEventItem(CalendarEvent evt)
    {
        if (eventItemPrefab == null || eventListContent == null) return;

        GameObject itemObj = Instantiate(eventItemPrefab, eventListContent);
        EventItemUI itemUI = itemObj.GetComponent<EventItemUI>();

        if (itemUI != null)
        {
            itemUI.Initialize(evt);
            itemUI.OnCheckboxToggled += OnEventCheckboxToggled;
        }

        eventItemInstances.Add(itemObj);
    }

    private void ClearEventList()
    {
        foreach (var item in eventItemInstances)
        {
            if (item != null)
            {
                // 이벤트 구독 해제
                EventItemUI itemUI = item.GetComponent<EventItemUI>();
                if (itemUI != null)
                {
                    itemUI.OnCheckboxToggled -= OnEventCheckboxToggled;
                }
            
                Destroy(item);
            }
        }
        eventItemInstances.Clear();
    }

    private void OnEventCheckboxToggled(string eventId, bool isCompleted)
    {
        EventDataManager.Instance.ToggleEventCompletion(eventId);

        if (isCompleted)
        {
            CalendarEvent evt = EventDataManager.Instance.GetTodayEvents()
                .Find(e => e.id == eventId);

            if (evt != null && CompletionPopup.Instance != null)
            {
                CompletionPopup.Instance.Show(evt.title);
            }
        }
    }

    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(show);
        }
    }

    private void ShowEmptyState(string message)
    {
        if (emptyStatePanel != null)
        {
            emptyStatePanel.SetActive(true);
        }

        if (emptyStateText != null)
        {
            emptyStateText.text = message;
        }

        ClearEventList();
    }

    private void HideEmptyState()
    {
        if (emptyStatePanel != null)
        {
            emptyStatePanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (EventDataManager.Instance != null)
        {
            EventDataManager.Instance.OnDataChanged -= RefreshEventList;
        }

        if (GoogleCalendarAPI.Instance != null)
        {
            GoogleCalendarAPI.Instance.OnEventsLoaded -= OnEventsLoaded;
            GoogleCalendarAPI.Instance.OnError -= OnAPIError;
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(OnRefreshClicked);
        }
    }
}
