using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>Local top ten for jump park 4, persisted between game launches.</summary>
public static class EndlessSurvivalLeaderboard
{
    private const string SaveKey = "JumpPark4.Leaderboard.v1";
    public const int Capacity = 10;

    [Serializable]
    public sealed class Entry
    {
        public float seconds;
        public string playedAt;
    }

    [Serializable]
    private sealed class SaveData
    {
        public List<Entry> entries = new List<Entry>();
    }

    public static List<Entry> Load()
    {
        SaveData data = null;
        try
        {
            data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, ""));
        }
        catch (ArgumentException)
        {
            // A missing or invalid local record starts a fresh leaderboard.
        }
        List<Entry> entries = data != null && data.entries != null
            ? data.entries : new List<Entry>();
        entries.RemoveAll(e => e == null || float.IsNaN(e.seconds) ||
            float.IsInfinity(e.seconds) || e.seconds < 0f);
        entries = entries.OrderByDescending(e => e.seconds).ToList();
        if (entries.Count > Capacity)
            entries.RemoveRange(Capacity, entries.Count - Capacity);
        return entries;
    }

    // Older entries win ties. Returns zero if the run did not place in the top ten.
    public static int Record(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            return 0;
        List<Entry> entries = Load();
        // Match the hundredth-of-a-second precision shown to the player.
        var entry = new Entry
        {
            seconds = (float)(Math.Floor(seconds * 100d) / 100d),
            playedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };
        int index = entries.FindIndex(e => entry.seconds > e.seconds);
        if (index < 0)
            index = entries.Count;
        if (index >= Capacity)
            return 0;
        entries.Insert(index, entry);
        if (entries.Count > Capacity)
            entries.RemoveAt(Capacity);
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new SaveData { entries = entries }));
        PlayerPrefs.Save();
        return index + 1;
    }

    public static string BuildDisplay()
    {
        List<Entry> entries = Load();
        var text = new StringBuilder("本机排行榜 · 前 10 名\n");
        for (int i = 0; i < entries.Count; i++)
        {
            text.Append('\n').Append((i + 1).ToString("00")).Append("    ")
                .Append(EndlessSurvivalMode.FormatTime(entries[i].seconds))
                .Append("    ").Append(entries[i].playedAt);
        }
        if (entries.Count == 0)
            text.Append("\n暂无成绩");
        return text.ToString();
    }
}
