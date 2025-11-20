using System;
using System.Collections;
using UnityEngine;

public class SyncManager : MonoBehaviour
{
    public static SyncManager Instance { get; private set; }

    private bool isAutoSyncEnabled = false;
    private int syncIntervalMinutes = 30;
    private Coroutine syncCoroutine;

    public event Action OnSyncStarted;
    public event Action OnSyncCompleted;
    public event Action<string> OnSyncError;

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

    public void StartAutoSync(int intervalMinutes)
    {
        syncIntervalMinutes = intervalMinutes;
        isAutoSyncEnabled = true;

        if (syncCoroutine != null)
        {
            StopCoroutine(syncCoroutine);
        }

        syncCoroutine = StartCoroutine(AutoSyncCoroutine());
        Debug.Log($"자동 동기화 시작: {intervalMinutes}분 간격");
    }

    public void StopAutoSync()
    {
        isAutoSyncEnabled = false;

        if (syncCoroutine != null)
        {
            StopCoroutine(syncCoroutine);
            syncCoroutine = null;
        }

        Debug.Log("자동 동기화 중지");
    }

    public void SyncNow()
    {
        StartCoroutine(SyncCoroutine());
    }

    private IEnumerator AutoSyncCoroutine()
    {
        while (isAutoSyncEnabled)
        {
            yield return SyncCoroutine();
            yield return new WaitForSeconds(syncIntervalMinutes * 60);
        }
    }

    private IEnumerator SyncCoroutine()
    {
        if (GoogleOAuthManager.Instance == null || !GoogleOAuthManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("동기화 실패: 인증되지 않음");
            OnSyncError?.Invoke("인증이 필요합니다.");
            yield break;
        }

        OnSyncStarted?.Invoke();
        Debug.Log("동기화 시작...");

        bool syncCompleted = false;
        bool syncFailed = false;
        string errorMessage = "";

        void OnEventsLoaded(System.Collections.Generic.List<CalendarEvent> events)
        {
            syncCompleted = true;
        }

        void OnError(string error)
        {
            syncFailed = true;
            errorMessage = error;
        }

        if (GoogleCalendarAPI.Instance != null)
        {
            GoogleCalendarAPI.Instance.OnEventsLoaded += OnEventsLoaded;
            GoogleCalendarAPI.Instance.OnError += OnError;

            GoogleCalendarAPI.Instance.GetTodayEvents();

            float timeout = 10f;
            float elapsed = 0f;

            while (!syncCompleted && !syncFailed && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            GoogleCalendarAPI.Instance.OnEventsLoaded -= OnEventsLoaded;
            GoogleCalendarAPI.Instance.OnError -= OnError;

            if (syncCompleted)
            {
                OnSyncCompleted?.Invoke();
                Debug.Log("동기화 완료");

                if (NotificationManager.Instance != null && SettingsManager.Instance != null)
                {
                    if (SettingsManager.Instance.NotificationEnabled)
                    {
                        var events = EventDataManager.Instance.GetTodayEvents();
                        NotificationManager.Instance.ScheduleEventNotifications(events);
                    }
                }
            }
            else if (syncFailed)
            {
                OnSyncError?.Invoke(errorMessage);
                Debug.LogError($"동기화 실패: {errorMessage}");
            }
            else
            {
                OnSyncError?.Invoke("시간 초과");
                Debug.LogError("동기화 시간 초과");
            }
        }
    }
}
