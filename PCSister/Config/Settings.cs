using System.Collections.Generic;

namespace PCSister.Config
{
    public static class Settings
    {
        public const int FPS = 60;
        public const int SpriteSize = 128;
        public const int Scale = 2;
        public const int ScareThreshold = 30;

        public const int SPEED_WALK = 6;
        public const int SPEED_RUN = 9;
        public const int IDLE_TIMEOUT = 4;
        public const int RUN_TIMEOUT = 3;

        public const int AnimDelayFast = 3;
        public const int AnimDelaySlow = 10;

        public const string StateIdle = "afk";
        public const string StateIdle2 = "afk2";
        public const string StateWalk = "walk";
        public const string StateRun = "run";
        public const string StateJump = "jump";
        public const string StateHurt = "hurt";
        public const string StateDead = "dead";

        public static readonly Dictionary<string, int> FrameCounts = new Dictionary<string, int>
        {
            { StateIdle,  5 },
            { StateIdle2, 5 },
            { StateWalk,  8 },
            { StateRun,   8 },
            { StateJump,  7 },
            { StateDead,  5 },
            { StateHurt,  2 }
        };

        public static readonly List<string> DistractionApps = new List<string>
        {
            "youtube", "discord", "tiktok", "telegram", "steam"
        };

        public static readonly Dictionary<string, string> ClipboardReactions = new Dictionary<string, string>
        {
            { "youtube.com", "Знову відосики замість роботи?" },
            { "stackoverflow.com", "Копіпастимо? Сподіваюсь, ти розумієш цей код..." },
            { "error", "Ой-ой, помилочка в коді!" },
            { "exception", "Лови ексепшн! Хтось забув try-except?" },
            { "todo", "Так-так, 'зроблю потім'... Знаю я ці ваші TODO." }
        };
    }
}