// Game content, loaded from Resources/SparkCityData/*.json (edit those files to change text and answers).
using System;
using System.Linq;
using UnityEngine;

namespace SparkCity
{
    [Serializable] public class ContentDef { public Mission1Def mission1; public Mission2Def mission2; }

    [Serializable] public class StepText
    {
        public string title, situation, instruction, more, remember, howTo;
    }

    // ---------------------------------------------------------------- mission 1
    [Serializable] public class Mission1Def
    {
        public string title, subtitle, story, hazardColor;
        public string[] districts;
        public string hazardDistrict, wrongDistrict, safeFrom, unsafeFrom;
        public int floodMinutes = 10;
        public SorterDef sorter;
        public CleanupDef cleanup;
        public PipelineDef pipeline;
        public ClusterDef cluster;
        public TrustDef trust;

        public string DistrictName(string id)
        {
            int i = 0;
            if (districts != null && id != null && id.Length == 2 && int.TryParse(id.Substring(1), out i) && i >= 1 && i <= districts.Length)
                return districts[i - 1];
            return id;
        }
    }

    [Serializable] public class SorterDef : StepText { public SourceItem[] sources; }
    [Serializable] public class SourceItem
    {
        public string name, color, variety, volume, velocity, reason;
        public int veracity;
        public bool keep;
    }

    [Serializable] public class CleanupDef : StepText { public ToolItem[] tools; public RecordItem[] records; public RecordItem practice; }
    [Serializable] public class ToolItem { public string id, label, code, color, hint; }
    [Serializable] public class RecordItem { public string source, row, tool, clue; }

    [Serializable] public class PipelineDef : StepText
    {
        public string[] order;
        public string[] prompts;
        public BlockItem[] blocks;
        public string checkQuestion;
        public string[] checkOptions;
        public int checkAnswer;
        public BlockItem Block(string id) { return blocks.FirstOrDefault(b => b.id == id); }
    }
    [Serializable] public class BlockItem { public string id, label, code, kind, role; }

    [Serializable] public class ClusterDef : StepText { public int[] partitions; }

    [Serializable] public class TrustDef : StepText { public ReportItem[] reports; }
    [Serializable] public class ReportItem
    {
        public string title, correct, result, why;
        public int trust;
        public string[] lines;
    }

    // ---------------------------------------------------------------- mission 2
    [Serializable] public class Mission2Def
    {
        public string title, subtitle, story;
        public WindowDef window;
        public CrashDef crash;
    }

    [Serializable] public class WindowDef : StepText
    {
        public int goalCaught, goalDelay;
        public WatermarkOption[] options;
    }
    [Serializable] public class WatermarkOption { public int minutes, caught, delay; }

    [Serializable] public class CrashDef : StepText { public CrashOption[] options; }
    [Serializable] public class CrashOption { public string id, label, why; public bool correct; }

    // ---------------------------------------------------------------- tutorial + quiz
    [Serializable] public class CardDef { public string id, title, body, picture, color; }
    [Serializable] public class TutorialDef { public CardDef[] cards; }
    [Serializable] public class QuestionDef { public string id, prompt; public string[] options; public int correct; }
    [Serializable] public class QuizDef { public QuestionDef[] questions; }

    public static class GameData
    {
        static ContentDef content;
        static TutorialDef tutorial;
        static QuizDef quiz;

        static T Load<T>(string name) where T : class
        {
            var ta = Resources.Load<TextAsset>("SparkCityData/" + name);
            if (ta == null) { Debug.LogError("[SparkCity] Missing Resources/SparkCityData/" + name + ".json"); return null; }
            try { return JsonUtility.FromJson<T>(ta.text); }
            catch (Exception e) { Debug.LogError("[SparkCity] Could not read " + name + ".json: " + e.Message); return null; }
        }

        public static ContentDef Content { get { if (content == null) content = Load<ContentDef>("content"); return content; } }

        public static TutorialDef Tutorial
        {
            get
            {
                if (tutorial == null) tutorial = Load<TutorialDef>("tutorial") ?? new TutorialDef { cards = new CardDef[0] };
                return tutorial;
            }
        }

        public static QuizDef Quiz
        {
            get
            {
                if (quiz == null) quiz = Load<QuizDef>("quiz") ?? new QuizDef { questions = new QuestionDef[0] };
                return quiz;
            }
        }
    }
}
