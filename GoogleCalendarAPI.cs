using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Google Calendar API 호출을 처리하는 클래스
/// </summary>
public class GoogleCalendarAPI : MonoBehaviour
{
    public static GoogleCalendarAPI Instance { get; private set; }

    private const string CALENDAR_API_BASE = "https://www.googleapis.com/calendar/v3";
    private const int MAX_RETRY_COUNT = 3;

    public event Action<List<CalendarEvent>> OnEventsLoaded;
    public event Action<string> OnError;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GetTodayEvents()
    {
        DateTime today = DateTime.Today;
        DateTime tomorrow = today.AddDays(1);
        GetEvents(today, tomorrow);
    }

    public void GetEvents(DateTime startDate, DateTime endDate)
    {
        StartCoroutine(GetEventsCoroutine(startDate, endDate));
    }

    private IEnumerator GetEventsCoroutine(DateTime startDate, DateTime endDate, int retryCount = 0)
    {
        if (!GoogleOAuthManager.Instance.IsAuthenticated)
        {
            Debug.LogError("인증되지 않았습니다.");
            OnError?.Invoke("인증이 필요합니다.");
            yield break;
        }

        string timeMin = DateTimeHelper.ToRFC3339(startDate);
        string timeMax = DateTimeHelper.ToRFC3339(endDate);

        string url = $"{CALENDAR_API_BASE}/calendars/primary/events?timeMin={Uri.EscapeDataString(timeMin)}&timeMax={Uri.EscapeDataString(timeMax)}&singleEvents=true&orderBy=startTime";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", $"Bearer {GoogleOAuthManager.Instance.AccessToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    EventListResponse response = JsonUtility.FromJson<EventListResponse>(request.downloadHandler.text);
                    List<CalendarEvent> events = ParseEvents(response);
                    
                    OnEventsLoaded?.Invoke(events);
                    Debug.Log($"{events.Count}개의 일정을 불러왔습니다.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"JSON 파싱 오류: {ex.Message}");
                    OnError?.Invoke("데이터 파싱 오류");
                }
            }
            else
            {
                HandleError(request, startDate, endDate, retryCount);
            }
        }
    }

    private void HandleError(UnityWebRequest request, DateTime startDate, DateTime endDate, int retryCount)
    {
        long responseCode = request.responseCode;

        switch (responseCode)
        {
            case 401:
                Debug.Log("토큰 만료, 갱신 시도...");
                StartCoroutine(RefreshAndRetry(startDate, endDate));
                break;

            case 403:
                Debug.LogError("API 권한이 부족합니다.");
                OnError?.Invoke("권한이 부족합니다.");
                break;

            case 429:
                Debug.LogWarning("API 호출 제한 초과, 재시도 중...");
                if (retryCount < MAX_RETRY_COUNT)
                {
                    StartCoroutine(RetryAfterDelay(startDate, endDate, retryCount + 1, 2f));
                }
                else
                {
                    OnError?.Invoke("API 호출 제한 초과");
                }
                break;

            case 500:
            case 503:
                Debug.LogWarning("서버 오류, 재시도 중...");
                if (retryCount < MAX_RETRY_COUNT)
                {
                    StartCoroutine(RetryAfterDelay(startDate, endDate, retryCount + 1, 1f));
                }
                else
                {
                    OnError?.Invoke("서버 오류");
                }
                break;

            default:
                Debug.LogError($"API 호출 실패: {request.error}");
                OnError?.Invoke($"오류: {request.error}");
                break;
        }
    }

    private IEnumerator RefreshAndRetry(DateTime startDate, DateTime endDate)
    {
        yield return GoogleOAuthManager.Instance.RefreshAccessToken();
        
        if (GoogleOAuthManager.Instance.IsAuthenticated)
        {
            yield return GetEventsCoroutine(startDate, endDate);
        }
        else
        {
            OnError?.Invoke("토큰 갱신 실패");
        }
    }

    private IEnumerator RetryAfterDelay(DateTime startDate, DateTime endDate, int retryCount, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return GetEventsCoroutine(startDate, endDate, retryCount);
    }

    private List<CalendarEvent> ParseEvents(EventListResponse response)
    {
        List<CalendarEvent> events = new List<CalendarEvent>();

        if (response.items == null) return events;

        foreach (var item in response.items)
        {
            CalendarEvent calEvent = new CalendarEvent
            {
                id = item.id,
                title = item.summary ?? "제목 없음",
                description = item.description ?? "",
                location = item.location ?? "",
                startTime = ParseDateTime(item.start),
                endTime = ParseDateTime(item.end),
                isCompleted = false
            };

            events.Add(calEvent);
        }

        return events;
    }

    private DateTime ParseDateTime(EventDateTime eventDateTime)
    {
        if (eventDateTime == null) return DateTime.MinValue;

        if (!string.IsNullOrEmpty(eventDateTime.dateTime))
        {
            return DateTimeHelper.ParseRFC3339(eventDateTime.dateTime);
        }
        else if (!string.IsNullOrEmpty(eventDateTime.date))
        {
            DateTime.TryParse(eventDateTime.date, out DateTime date);
            return date;
        }

        return DateTime.MinValue;
    }

    [Serializable]
    private class EventListResponse
    {
        public EventItem[] items;
    }

    [Serializable]
    private class EventItem
    {
        public string id;
        public string summary;
        public string description;
        public string location;
        public EventDateTime start;
        public EventDateTime end;
    }

    [Serializable]
    private class EventDateTime
    {
        public string dateTime;
        public string date;
        public string timeZone;
    }
}
