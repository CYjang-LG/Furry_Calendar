using System;
using System.Globalization;

public static class DateTimeHelper
{
    private static readonly CultureInfo koreanCulture = new CultureInfo("ko-KR");

    public static DateTime ParseRFC3339(string dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
        {
            return DateTime.MinValue;
        }

        try
        {
            return DateTime.Parse(dateTimeString, null, DateTimeStyles.RoundtripKind);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public static string ToRFC3339(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
    }

    public static string ToKoreanTimeFormat(DateTime dateTime)
    {
        string period = dateTime.Hour < 12 ? "오전" : "오후";
        int hour = dateTime.Hour % 12;
        if (hour == 0) hour = 12;

        return $"{period} {hour:D2}:{dateTime.Minute:D2}";
    }

    public static string ToKoreanDateFormat(DateTime dateTime)
    {
        return dateTime.ToString("yyyy년 MM월 dd일", koreanCulture);
    }

    public static string ToKoreanDateTimeFormat(DateTime dateTime)
    {
        string date = ToKoreanDateFormat(dateTime);
        string time = ToKoreanTimeFormat(dateTime);
        return $"{date} {time}";
    }

    public static bool IsToday(DateTime dateTime)
    {
        DateTime today = DateTime.Today;
        return dateTime.Date == today;
    }

    public static bool IsTomorrow(DateTime dateTime)
    {
        DateTime tomorrow = DateTime.Today.AddDays(1);
        return dateTime.Date == tomorrow;
    }

    public static bool IsThisWeek(DateTime dateTime)
    {
        DateTime now = DateTime.Now;
        DateTime startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
        DateTime endOfWeek = startOfWeek.AddDays(7);

        return dateTime >= startOfWeek && dateTime < endOfWeek;
    }

    public static string GetRelativeTimeString(DateTime dateTime)
    {
        TimeSpan diff = dateTime - DateTime.Now;

        if (diff.TotalMinutes < 0)
        {
            return "지남";
        }
        else if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes}분 후";
        }
        else if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours}시간 후";
        }
        else if (IsToday(dateTime))
        {
            return "오늘";
        }
        else if (IsTomorrow(dateTime))
        {
            return "내일";
        }
        else if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays}일 후";
        }
        else
        {
            return ToKoreanDateFormat(dateTime);
        }
    }

    public static string GetDayOfWeekKorean(DateTime dateTime)
    {
        switch (dateTime.DayOfWeek)
        {
            case DayOfWeek.Sunday: return "일요일";
            case DayOfWeek.Monday: return "월요일";
            case DayOfWeek.Tuesday: return "화요일";
            case DayOfWeek.Wednesday: return "수요일";
            case DayOfWeek.Thursday: return "목요일";
            case DayOfWeek.Friday: return "금요일";
            case DayOfWeek.Saturday: return "토요일";
            default: return "";
        }
    }
}
