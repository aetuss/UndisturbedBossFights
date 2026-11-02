using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace UndisturbedBossFights
{
    // Vanilla RandEventSystem.UpdateRandomEvent (server only) adds the frame time to m_eventTimer and,
    // once it passes m_eventIntervalMin minutes (scaled by the world's event rate), resets it and rolls
    // m_eventChance to start a raid. Events with their own m_standaloneInterval do the same with their
    // own m_time. During a boss fight this keeps those timers just short of due, so the roll waits
    // instead of being skipped: when the fight is over it happens on the next frame, with the usual chance.
    // A raid that is already running is left alone, and so are raids started from the console
    // (randomevent, event), which don't go through these timers.
    [HarmonyPatch(typeof(RandEventSystem), "UpdateRandomEvent")]
    static class RaidPatch
    {
        const float Margin = 0.01f;
        static bool holding;

        static void Prefix(float __0, ref float ___m_eventTimer, float ___m_eventIntervalMin, List<RandomEvent> ___m_events, RandomEvent ___m_activeEvent)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            float dt = __0;
            if (!Plugin.DelayRaids.Value || !FightTracker.AnyFightActive || Game.m_eventRate <= 0f)
            {
                if (holding)
                {
                    holding = false;
                    Plugin.Debug("Boss fight over: the held raid roll happens now.");
                }
                return;
            }

            bool held = false;
            float due = ___m_eventIntervalMin * 60f * Game.m_eventRate;
            if (___m_eventTimer + dt > due)
            {
                ___m_eventTimer = due - dt - Margin;
                held = true;
            }
            foreach (RandomEvent ev in ___m_events)
            {
                if (!ev.m_enabled || ev.m_standaloneInterval <= 0f || ev == ___m_activeEvent) continue;
                float evDue = ev.m_standaloneInterval * Game.m_eventRate;
                if (ev.m_time + dt > evDue)
                {
                    ev.m_time = evDue - dt - Margin;
                    held = true;
                }
            }
            if (held && !holding)
            {
                holding = true;
                Plugin.Debug("A raid roll is due, but a boss fight is going on. Holding it until the fight is over.");
            }
        }

        internal static bool Holding
        {
            get { return holding; }
        }
    }

    // Vanilla SpawnSystem.UpdateSpawnList is the only caller of SpawnSystem.Spawn, once per creature of
    // a natural spawn group (eventSpawner false) or a raid's spawn group (true). Boss summons don't go
    // through SpawnSystem, so they're never affected.
    [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
    static class SpawnPatch
    {
        static bool Prefix(SpawnSystem.SpawnData __0, Vector3 __1, bool __2)
        {
            if (__2 || !Plugin.BlockSpawns.Value) return true;
            string bossName;
            if (!FightTracker.IsNearFight(__1, out bossName)) return true;
            Plugin.Debug("Blocked a " + __0.m_prefab.name + " from spawning at " + __1.ToString("0") + " during the fight with " + bossName + ".");
            return false;
        }
    }
}
