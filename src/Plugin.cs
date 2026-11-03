using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

// Thunderstore asks mods made with AI to say so here and in the README.
[assembly: System.Reflection.AssemblyMetadata("AI_Assisted_Creation", "This assembly was fully created with the assistance of Generative AI: the code was written by Claude (Anthropic) through Claude Code.")]
[assembly: System.Reflection.AssemblyMetadata("AI_Model_Vendor", "Anthropic")]

namespace UndisturbedBossFights
{
    [BepInPlugin(Guid, ModName, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "UndisturbedBossFights";
        public const string ModName = "UndisturbedBossFights";
        public const string Version = "1.0.3";

        internal static Synced<bool> DelayRaids;
        internal static Synced<bool> BlockSpawns;
        internal static Synced<float> SpawnBlockRadius;
        internal static Synced<float> FightDistance;

        // Local only (not synced): each player's log is their own.
        internal static ConfigEntry<bool> DebugLog;

        internal static Plugin Instance;
        internal static BepInEx.Logging.ManualLogSource Log;

        // All settings except DebugLog are server-synced; see ConfigSync.
        Synced<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription description)
        {
            return new Synced<T>(Config.Bind(section, key, defaultValue,
                new ConfigDescription(description.Description + " In multiplayer, the server's value is used.",
                    description.AcceptableValues)));
        }

        Synced<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            return Bind(section, key, defaultValue, new ConfigDescription(description));
        }

        void Awake()
        {
            Instance = this;
            Log = Logger;

            DelayRaids = Bind("1 - General", "DelayRaids", true,
                "While a boss fight is going on, no raid (random event) can start. A raid that was due waits until the fight is over, " +
                "then the game rolls for it as usual. Raids started from the console are not held back.");
            BlockSpawns = Bind("1 - General", "BlockSpawnsNearFight", true,
                "While a boss fight is going on, no new creatures spawn naturally near the boss or the players fighting it. " +
                "Creatures already there, the boss's own summons and creatures of a raid that was already running are not affected.");
            SpawnBlockRadius = Bind("1 - General", "SpawnBlockRadius", 80f,
                new ConfigDescription("How close (in meters) to the boss or a player fighting it a new creature may not spawn. " +
                    "The game spawns creatures 40-80 m from a player, so 80 blocks almost all of them.",
                    new AcceptableValueRange<float>(20f, 200f)));
            FightDistance = Bind("1 - General", "FightDistance", 60f,
                new ConfigDescription("A boss counts as being fought when it is alerted or has a target and a player is within this many meters of it.",
                    new AcceptableValueRange<float>(20f, 150f)));

            DebugLog = Config.Bind("1 - General", "DebugLog", false,
                "Writes boss fights, held-back raids and blocked spawns to the BepInEx log. Useful when reporting a problem. Not synced in multiplayer.");

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
        }

        void Update()
        {
            FightTracker.Update();
        }

        internal static void Debug(string message)
        {
            if (DebugLog.Value) Log.LogInfo(message);
        }
    }
}
