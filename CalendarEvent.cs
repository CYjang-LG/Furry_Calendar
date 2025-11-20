using System;
using UnityEngine;

[Serializable]
public class CalendarEvent
{
    public string id;
    public string title;
    public string description;
    public string location;
    public DateTime startTime;
    public DateTime endTime;
    public bool isCompleted;

    public bool IsAllDay => startTime.TimeOfDay == TimeSpan.Zero && endTime.TimeOfDay == TimeSpan.Zero;

    public bool IsOngoing
    {
        get
        {
            DateTime now = DateTime.Now;
            return now >= startTime && now <= endTime;
        }
    }

    public bool IsPast => DateTime.Now > endTime;

    public string GetFormattedStartTime()
    {
        return DateTimeHelper.ToKoreanTimeFormat(startTime);
    }

    public string GetTimeRange()
    {
        if (IsAllDay)
        {
            return "종일";
        }

        return $"{DateTimeHelper.ToKoreanTimeFormat(startTime)} - {DateTimeHelper.ToKoreanTimeFormat(endTime)}";
    }

    public override string ToString()
    {
        return $"[{id}] {title} ({GetTimeRange()})";
    }
}
