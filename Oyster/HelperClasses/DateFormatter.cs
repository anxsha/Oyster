using System;

namespace Oyster.HelperClasses {
/// <summary>
/// This class helps retain consistent date/time formatting across pages.
/// </summary>
public static class DateFormatter {
  public static string GetDefaultPostFormat(DateTime createdAtUtc, TimeZoneInfo userTz) {
    // For calculating the time difference between now and post's creation
    TimeSpan postTimeSpan;
    // Different display format depending on postTimeSpan
    if ((postTimeSpan = DateTime.Now.Subtract(
          TimeZoneInfo.ConvertTimeFromUtc(createdAtUtc, userTz))) >
        // created more than a year ago
        new TimeSpan(365, 0, 0, 0)) {
      return TimeZoneInfo.ConvertTimeFromUtc(createdAtUtc, userTz).Date.ToShortDateString();
    }

    if (postTimeSpan > new TimeSpan(24, 0, 0)) {
      return $"{postTimeSpan.Days} days ago";
    }

    if (postTimeSpan > new TimeSpan(1, 0, 0)) {
      return $"{postTimeSpan.Hours} hours ago";
    }

    if (postTimeSpan > new TimeSpan(0, 1, 0)) {
      return $"{postTimeSpan.Minutes} mins ago";
    }

    // Less than a minute ago
    return "now";
  }

  public static string GetDetailedPostFormat(DateTime createdAtUtc, TimeZoneInfo userTz) {
    var dateTime = TimeZoneInfo.ConvertTimeFromUtc(createdAtUtc, userTz);
    return dateTime.ToString("yyyy/MM/dd") + " " + dateTime.ToShortTimeString();
  }

  public static string GetCommentFormat(DateTime createdAtUtc, TimeZoneInfo userTz) {
    var dateTime = TimeZoneInfo.ConvertTimeFromUtc(createdAtUtc, userTz);

    if (dateTime.Date == DateTime.Now.Date) {
      return dateTime.ToShortTimeString();
    }

    return dateTime.ToString("yyyy/MM/dd") + " " + dateTime.ToShortTimeString();
  }
}
}