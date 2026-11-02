using System.Globalization;
using System.Reflection;
using HarmonyLib;

namespace UndisturbedBossFights
{
    // ubf_status shows what the mod sees; ubf_raidsoon (cheat) makes the next raid roll due in a few
    // seconds, to check that it waits for a boss fight to end.
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    static class Commands
    {
        const float RaidSoonSeconds = 10f;

        static readonly FieldInfo eventTimer = AccessTools.Field(typeof(RandEventSystem), "m_eventTimer");
        static readonly FieldInfo eventIntervalMin = AccessTools.Field(typeof(RandEventSystem), "m_eventIntervalMin");

        static void Postfix()
        {
            new Terminal.ConsoleCommand("ubf_status",
                "- UndisturbedBossFights: show boss fights the mod sees and when the next raid roll is due",
                args =>
                {
                    bool any = false;
                    foreach (string line in FightTracker.Describe())
                    {
                        if (!any) args.Context.AddString("Boss fights:");
                        any = true;
                        args.Context.AddString("  " + line);
                    }
                    if (!any) args.Context.AddString("No boss fight going on.");

                    RandEventSystem events = RandEventSystem.instance;
                    if (events == null || ZNet.instance == null || !ZNet.instance.IsServer())
                    {
                        args.Context.AddString("Raid timing is only known on the server.");
                        return;
                    }
                    if (Game.m_eventRate <= 0f)
                    {
                        args.Context.AddString("Raids are turned off in this world.");
                        return;
                    }
                    float left = EventDue(events) - (float)eventTimer.GetValue(events);
                    args.Context.AddString(RaidPatch.Holding
                        ? "A raid roll is due and waiting for the boss fight to end."
                        : "Next raid roll in " + (left / 60f).ToString("0.0", CultureInfo.InvariantCulture) + " min.");
                });

            new Terminal.ConsoleCommand("ubf_raidsoon",
                "- UndisturbedBossFights: make the next raid roll due in " + RaidSoonSeconds + " seconds (for testing)",
                args =>
                {
                    RandEventSystem events = RandEventSystem.instance;
                    if (events == null || ZNet.instance == null || !ZNet.instance.IsServer())
                    {
                        args.Context.AddString("Only works on the server (single player or the host).");
                        return;
                    }
                    eventTimer.SetValue(events, EventDue(events) - RaidSoonSeconds);
                    args.Context.AddString("The next raid roll is due in " + RaidSoonSeconds + " seconds. Its chance to start a raid is still the usual one.");
                }, true);
        }

        static float EventDue(RandEventSystem events)
        {
            return (float)eventIntervalMin.GetValue(events) * 60f * Game.m_eventRate;
        }
    }
}
