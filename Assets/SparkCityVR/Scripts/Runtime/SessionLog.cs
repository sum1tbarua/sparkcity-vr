// Research / classroom logging. Each finished mission and each quiz writes one JSON line to
//   <persistentDataPath>/SparkCityLogs/spark_city_YYYYMMDD.jsonl
// On Quest that is /sdcard/Android/data/<your.package.name>/files/SparkCityLogs/
// Use Tools/merge_logs.py to turn the files into CSV tables.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SparkCity
{
    [Serializable] public class LogEvent { public float t; public string type; public string detail; }

    [Serializable] public class GameResult
    {
        public string id;
        public int points, maxPoints, correct, wrong, hints, stars;
        public float seconds;
    }

    [Serializable] public class MissionRecord
    {
        public string recordType = "mission";
        public string playerId;
        public string sessionId;
        public string device;
        public string startedAt;
        public string missionId;
        public string cityOutcome;
        public float floodSecondsLeft;
        public float totalSeconds;
        public int points, maxPoints, percent;
        public List<GameResult> games = new List<GameResult>();
        public List<LogEvent> events = new List<LogEvent>();
    }

    [Serializable] public class QuizRecord
    {
        public string recordType = "quiz";
        public string playerId;
        public string sessionId;
        public string device;
        public string startedAt;
        public string quizType; // "pre" | "post"
        public int correct;
        public int total;
        public float seconds;
        public string[] answers; // "q1=2:1" -> question id = chosen option : correct(1/0)
    }

    public static class SessionLog
    {
        public static readonly string SessionId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        static readonly List<LogEvent> events = new List<LogEvent>();
        static float eventsT0;

        public static int PlayerNumber
        {
            get { return PlayerPrefs.GetInt("sc_player", 1); }
            set { PlayerPrefs.SetInt("sc_player", Mathf.Clamp(value, 1, 999)); PlayerPrefs.Save(); }
        }

        public static string PlayerId { get { return "P" + PlayerNumber.ToString("000"); } }

        public static string Folder { get { return Path.Combine(Application.persistentDataPath, "SparkCityLogs"); } }

        public static void BeginEvents()
        {
            events.Clear();
            eventsT0 = Time.time;
        }

        public static void Event(string type, string detail)
        {
            events.Add(new LogEvent { t = Mathf.Round((Time.time - eventsT0) * 10f) / 10f, type = type, detail = detail });
            if (events.Count > 500) events.RemoveAt(0);
        }

        public static List<LogEvent> TakeEvents()
        {
            var copy = new List<LogEvent>(events);
            events.Clear();
            return copy;
        }

        /// <summary>Saving log files is OFF (no per-student logs needed). Set to true to write them again.</summary>
        public static bool Enabled = false;

        public static string Write(object record)
        {
            if (!Enabled) return null;   // nothing is written to the headset
            try
            {
                Directory.CreateDirectory(Folder);
                string file = Path.Combine(Folder, "spark_city_" + DateTime.Now.ToString("yyyyMMdd") + ".jsonl");
                File.AppendAllText(file, JsonUtility.ToJson(record) + "\n");
                Debug.Log("[SparkCity] Logged to " + file);
                return file;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SparkCity] Could not write log: " + ex.Message);
                return null;
            }
        }

        public static string Now() { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }
        public static string Device() { return SystemInfo.deviceModel; }
    }
}