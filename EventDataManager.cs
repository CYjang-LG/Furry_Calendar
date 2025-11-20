using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventDataManager : MonoBehaviour
{
    public static EventDataManager Instance { get; private set; }

    private List<CalendarEvent> allEvents = new List<CalendarEvent>();
    private Dictionary<string, bool> completionStatus = new Dictionary<string, bool>();

    public event Action OnDataChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCompletionStatus();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (GoogleCalendarAPI.Instance != null)
        {
            GoogleCalendarAPI.Instance.OnEventsLoaded += SetEvents;
        }
    }

    public void SetEvents(List<CalendarEvent> events)
    {
        allEvents = events;
        
        foreach (var evt in allEvents)
        {
            if (completionStatus.ContainsKey(evt.id))
            {
                evt.isCompleted = completionStatus[evt.id];
            }
        }

        OnDataChanged?.Invoke();
        Debug.Log($"EventDataManager: {allEvents.Count}개 이벤트 설정됨");
    }

    /// <summary>
    /// 오늘의 일정 가져오기 (타임존 처리 개선)
    /// </summary>
    public List<CalendarEvent> GetTodayEvents()
    {
        DateTime now = DateTime.Now;
        DateTime todayStart = now.Date; // 오늘 00:00:00
        DateTime tomorrowStart = todayStart.AddDays(1); // 내일 00:00:00

        return allEvents
            .Where(e => {
                // 일정의 시작 날짜만 비교 (시간 무시)
                DateTime eventDate = e.startTime.Date;
                
                // 오늘 날짜인 일정만 포함
                return eventDate >= todayStart && eventDate < tomorrowStart;
            })
            .OrderBy(e => e.startTime)
            .ToList();
    }

    /// <summary>
    /// 특정 날짜의 일정 가져오기 (타임존 처리 개선)
    /// </summary>
    public List<CalendarEvent> GetEventsByDate(DateTime date)
    {
        DateTime startOfDay = date.Date; // 해당 날짜 00:00:00
        DateTime endOfDay = startOfDay.AddDays(1); // 다음 날 00:00:00

        return allEvents
            .Where(e => {
                DateTime eventDate = e.startTime.Date;
                return eventDate >= startOfDay && eventDate < endOfDay;
            })
            .OrderBy(e => e.startTime)
            .ToList();
    }

    public List<CalendarEvent> GetIncompleteEvents()
    {
        return allEvents
            .Where(e => !e.isCompleted && !e.IsPast)
            .OrderBy(e => e.startTime)
            .ToList();
    }

    public void ToggleEventCompletion(string eventId)
    {
        CalendarEvent evt = allEvents.FirstOrDefault(e => e.id == eventId);
        if (evt != null)
        {
            evt.isCompleted = !evt.isCompleted;
            completionStatus[eventId] = evt.isCompleted;
            SaveCompletionStatus();
            OnDataChanged?.Invoke();

            Debug.Log($"일정 '{evt.title}' 완료 상태: {evt.isCompleted}");
        }
    }

    public void CompleteEvent(string eventId)
    {
        CalendarEvent evt = allEvents.FirstOrDefault(e => e.id == eventId);
        if (evt != null && !evt.isCompleted)
        {
            evt.isCompleted = true;
            completionStatus[eventId] = true;
            SaveCompletionStatus();
            OnDataChanged?.Invoke();
        }
    }

    public int GetCompletedCount()
    {
        return allEvents.Count(e => e.isCompleted);
    }

    public int GetTodayCompletedCount()
    {
        return GetTodayEvents().Count(e => e.isCompleted);
    }

    private void SaveCompletionStatus()
    {
        CompletionData data = new CompletionData();
        data.eventIds = new List<string>(completionStatus.Keys);
        data.completedStates = new List<bool>(completionStatus.Values);
        
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("EventCompletions", json);
        PlayerPrefs.Save();
    }

    private void LoadCompletionStatus()
    {
        string json = PlayerPrefs.GetString("EventCompletions", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                CompletionData data = JsonUtility.FromJson<CompletionData>(json);
                completionStatus = new Dictionary<string, bool>();
                
                for (int i = 0; i < data.eventIds.Count; i++)
                {
                    completionStatus[data.eventIds[i]] = data.completedStates[i];
                }
            }
            catch
            {
                completionStatus = new Dictionary<string, bool>();
            }
        }
    }

    public void ClearOldCompletions()
    {
        DateTime today = DateTime.Today;
        List<string> keysToRemove = new List<string>();

        foreach (var evt in allEvents)
        {
            DateTime eventDate = evt.startTime.Date;
            if (eventDate < today && completionStatus.ContainsKey(evt.id))
            {
                keysToRemove.Add(evt.id);
            }
        }

        foreach (var key in keysToRemove)
        {
            completionStatus.Remove(key);
        }

        if (keysToRemove.Count > 0)
        {
            SaveCompletionStatus();
        }
    }

    [Serializable]
    private class CompletionData
    {
        public List<string> eventIds;
        public List<bool> completedStates;
    }
}
