using System.Collections.Generic;
using System.Text;
using HotPotato.Options;
using UnityEngine;

namespace HotPotato.Core;

// What was picked up, captured right before the pickup completes.
internal sealed class PickupRecord
{
    public WorkerBase Carrier;
    public string CarrierName;
    public string ItemName;
    public float TemperatureKelvin;
}

internal static class PickupAlert
{
    // Read by the in-game tests.
    internal static int RaisedCount;
    internal static PickupRecord LastRecord;
    internal static Notification LastNotification;

    public static void Raise(PickupRecord record)
    {
        RaisedCount++;
        LastRecord = record;

        var options = ModOptions.Instance;
        if (options.ShowNotification)
        {
            Notify(record);
        }
        if (options.PauseGame)
        {
            PauseOnce();
        }
    }

    public static string FormatMessage(PickupRecord record) =>
        string.Format(STRINGS.NOTIFICATION.MESSAGE, record.CarrierName, record.ItemName,
            GameUtil.GetFormattedTemperature(record.TemperatureKelvin));

    private static void Notify(PickupRecord record)
    {
        if (record.Carrier == null)
        {
            return;
        }

        // Every alert uses the same title, so the notification bar groups them into one entry.
        var notification = new Notification(
            STRINGS.NOTIFICATION.TITLE,
            NotificationType.BadMinor,
            BuildTooltip,
            FormatMessage(record),
            click_focus: record.Carrier.transform);
        record.Carrier.gameObject.AddOrGet<Notifier>().Add(notification);
        LastNotification = notification;
    }

    private static string BuildTooltip(List<Notification> notifications, object _)
    {
        var text = new StringBuilder(STRINGS.NOTIFICATION.TOOLTIP);
        foreach (var notification in notifications)
        {
            text.Append("\n• ").Append(notification.tooltipData);
        }
        return text.ToString();
    }

    // SpeedControlScreen counts pauses: a second Pause() would make the player press unpause twice.
    private static void PauseOnce()
    {
        var speedControl = SpeedControlScreen.Instance;
        if (speedControl != null && !speedControl.IsPaused)
        {
            speedControl.Pause();
        }
    }
}
