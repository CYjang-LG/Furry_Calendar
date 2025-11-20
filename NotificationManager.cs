using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    private bool isEnabled = true;
    private int minutesBefore = 15;

#if UNITY_ANDROID
    private const string CHANNEL_ID = "calendar_notifications";
    private const string CHANNEL_NAME = "일정 알림";
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNotifications();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeNotifications()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = CHANNEL_ID,
            Name = CHANNEL_NAME,
            Importance = Importance.High,
            Description = "캘린더 일정 알림",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#elif UNITY_IOS
        StartCoroutine(RequestIOSAuthorization());
#endif
    }

#if UNITY_IOS
    private IEnumerator RequestIOSAuthorization()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsCompleted)
            {
                yield return null;
            }

            string res = "\n RequestAuthorization: \n";
            res += "\n finished: " + req.IsCompleted;
            res += "\n granted :  " + req.Granted;
            res += "\n error:  " + req.Error;
            res += "\n deviceToken:  " + req.DeviceToken;
            Debug.Log(res);
        }
    }
#endif

    public void ScheduleEventNotifications(List<CalendarEvent> events)
    {
        if (!isEnabled) return;

        CancelAllNotifications();

        foreach (var evt in events)
        {
            if (!evt.isCompleted && !evt.IsPast)
            {
                ScheduleNotification(evt);
            }
        }
    }

    private void ScheduleNotification(CalendarEvent evt)
    {
        DateTime notificationTime = evt.startTime.AddMinutes(-minutesBefore);
        
        if (notificationTime <= DateTime.Now)
        {
            return;
        }

        TimeSpan delay = notificationTime - DateTime.Now;

#if UNITY_ANDROID
        var notification = new AndroidNotification();
        notification.Title = "일정 알림";
        notification.Text = $"{minutesBefore}분 후 '{evt.title}' 일정이 시작됩니다.";
        notification.FireTime = DateTime.Now.Add(delay);
        notification.SmallIcon = "icon_small";
        notification.LargeIcon = "icon_large";

        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
#elif UNITY_IOS
        var notification = new iOSNotification()
        {
            Identifier = evt.id,
            Title = "일정 알림",
            Body = $"{minutesBefore}분 후 '{evt.title}' 일정이 시작됩니다.",
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "calendar_category",
            ThreadIdentifier = "calendar_thread",
            Trigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = delay,
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif

        Debug.Log($"알림 예약: {evt.title} - {notificationTime}");
    }

    public void CancelAllNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        Debug.Log("모든 알림 취소됨");
    }

    public void EnableNotifications()
    {
        isEnabled = true;
        
        if (EventDataManager.Instance != null)
        {
            List<CalendarEvent> events = EventDataManager.Instance.GetTodayEvents();
            ScheduleEventNotifications(events);
        }
    }

    public void DisableNotifications()
    {
        isEnabled = false;
        CancelAllNotifications();
    }

    public void UpdateNotificationTime(int minutes)
    {
        minutesBefore = minutes;
        
        if (isEnabled && EventDataManager.Instance != null)
        {
            List<CalendarEvent> events = EventDataManager.Instance.GetTodayEvents();
            ScheduleEventNotifications(events);
        }
    }
}
