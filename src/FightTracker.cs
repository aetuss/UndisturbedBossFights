using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace UndisturbedBossFights
{
    // Knows where boss fights are going on. Every peer scans the creatures it has loaded once a second:
    // a boss counts as being fought when it is alive, alerted or has a target (both are synced through
    // the boss's ZDO, so this works on any peer that has it loaded), and a player is within FightDistance.
    // Clients report their fights to the server, because raids are decided there and a dedicated server
    // doesn't load creatures itself.
    static class FightTracker
    {
        const float ScanInterval = 1f;
        // A fight still counts this long after the boss last qualified, so a boss briefly losing sight
        // of everyone doesn't open a gap for raids and spawns.
        const float EndDelay = 20f;
        const float ReportInterval = 2f;
        const float ReportTimeout = 6f;
        const string ReportRpc = "UndisturbedBossFights_Fight";

        class Fight
        {
            public string BossName;
            public Vector3 BossPos;
            public readonly List<Vector3> Players = new List<Vector3>();
            public float LastSeen;
        }

        class RemoteFight
        {
            public string PlayerName;
            public string BossNames;
            public float Until;
        }

        static readonly Dictionary<Character, Fight> fights = new Dictionary<Character, Fight>();
        static readonly Dictionary<long, RemoteFight> remoteFights = new Dictionary<long, RemoteFight>();
        static readonly List<Character> toRemove = new List<Character>();
        static readonly List<long> remoteToRemove = new List<long>();
        static float scanTimer;
        static float reportTimer;
        static bool wasActive;

        // True while any boss fight is going on that this peer knows about (on the server: anywhere in the world).
        internal static bool AnyFightActive
        {
            get { return fights.Count > 0 || remoteFights.Count > 0; }
        }

        internal static void Update()
        {
            if (ZNet.instance == null)
            {
                fights.Clear();
                remoteFights.Clear();
                wasActive = false;
                return;
            }

            scanTimer += Time.deltaTime;
            if (scanTimer >= ScanInterval)
            {
                scanTimer = 0f;
                Scan();
                ExpireRemote();
            }

            if (!ZNet.instance.IsServer() && fights.Count > 0)
            {
                reportTimer += Time.deltaTime;
                if (reportTimer >= ReportInterval)
                {
                    reportTimer = 0f;
                    Report();
                }
            }
            else
                reportTimer = ReportInterval;

            bool active = AnyFightActive;
            if (active != wasActive)
            {
                wasActive = active;
                Plugin.Debug(active ? "A boss fight started: raids and nearby spawns are held back." : "No boss fight anymore: raids and spawns are back to normal.");
            }
        }

        static void Scan()
        {
            float now = Time.time;
            float maxDistance = Plugin.FightDistance.Value;
            foreach (Character boss in Character.GetAllCharacters())
            {
                if (boss == null || !boss.IsBoss() || boss.IsDead()) continue;
                BaseAI ai = boss.GetBaseAI();
                if (ai == null || !(ai.IsAlerted() || ai.HaveTarget())) continue;

                Vector3 bossPos = boss.transform.position;
                Fight fight = null;
                foreach (Player player in Player.GetAllPlayers())
                {
                    if (player == null || player.IsDead()) continue;
                    Vector3 playerPos = player.transform.position;
                    if (Vector3.Distance(playerPos, bossPos) > maxDistance) continue;
                    if (fight == null)
                    {
                        if (!fights.TryGetValue(boss, out fight))
                        {
                            fight = new Fight();
                            fight.BossName = boss.GetHoverName();
                            fights[boss] = fight;
                            Plugin.Debug("Boss fight with " + fight.BossName + " at " + bossPos.ToString("0") + ".");
                        }
                        fight.Players.Clear();
                        fight.BossPos = bossPos;
                        fight.LastSeen = now;
                    }
                    fight.Players.Add(playerPos);
                }
            }

            toRemove.Clear();
            foreach (KeyValuePair<Character, Fight> entry in fights)
                if (now - entry.Value.LastSeen > EndDelay) toRemove.Add(entry.Key);
            foreach (Character boss in toRemove)
            {
                Plugin.Debug("Boss fight with " + fights[boss].BossName + " is over.");
                fights.Remove(boss);
            }
        }

        // Is this spot close to a boss fight this peer can see? Spawns are run by the zone's owner,
        // which is always a player nearby, so its own scan is enough.
        internal static bool IsNearFight(Vector3 pos, out string bossName)
        {
            float radius = Plugin.SpawnBlockRadius.Value;
            float radiusSqr = radius * radius;
            foreach (Fight fight in fights.Values)
            {
                bossName = fight.BossName;
                if ((fight.BossPos - pos).sqrMagnitude <= radiusSqr) return true;
                foreach (Vector3 playerPos in fight.Players)
                    if ((playerPos - pos).sqrMagnitude <= radiusSqr) return true;
            }
            bossName = null;
            return false;
        }

        static string LocalBossNames()
        {
            List<string> names = new List<string>();
            foreach (Fight fight in fights.Values) names.Add(fight.BossName);
            return string.Join(", ", names.ToArray());
        }

        static void Report()
        {
            ZRpc rpc = ZNet.instance.GetServerRPC();
            if (rpc != null) rpc.Invoke(ReportRpc, LocalBossNames());
        }

        // Server: a client is in a boss fight.
        static void OnReport(ZRpc rpc, string bossNames)
        {
            ZNetPeer peer = null;
            foreach (ZNetPeer p in ZNet.instance.GetPeers())
                if (p.m_rpc == rpc) peer = p;
            if (peer == null) return;

            RemoteFight fight;
            if (!remoteFights.TryGetValue(peer.m_uid, out fight))
            {
                fight = new RemoteFight();
                remoteFights[peer.m_uid] = fight;
                Plugin.Debug(peer.m_playerName + " is in a boss fight with " + bossNames + ".");
            }
            fight.PlayerName = peer.m_playerName;
            fight.BossNames = bossNames;
            fight.Until = Time.time + ReportTimeout;
        }

        static void ExpireRemote()
        {
            remoteToRemove.Clear();
            foreach (KeyValuePair<long, RemoteFight> entry in remoteFights)
                if (Time.time > entry.Value.Until) remoteToRemove.Add(entry.Key);
            foreach (long uid in remoteToRemove)
            {
                Plugin.Debug(remoteFights[uid].PlayerName + "'s boss fight is over.");
                remoteFights.Remove(uid);
            }
        }

        internal static IEnumerable<string> Describe()
        {
            foreach (Fight fight in fights.Values)
                yield return fight.BossName + " at " + fight.BossPos.ToString("0") + ", " + fight.Players.Count + " player(s) near, last seen fighting " +
                    (Time.time - fight.LastSeen).ToString("0") + " s ago";
            foreach (RemoteFight fight in remoteFights.Values)
                yield return fight.PlayerName + " is fighting " + fight.BossNames + " (reported by their game)";
        }

        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        static class NewConnectionPatch
        {
            static void Postfix(ZNet __instance, ZNetPeer __0)
            {
                if (__instance.IsServer())
                    __0.m_rpc.Register<string>(ReportRpc, OnReport);
            }
        }
    }
}
