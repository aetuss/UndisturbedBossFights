# UndisturbedBossFights

No raids or new creature spawns interrupt a boss fight.

## What changes?

Getting raided in the middle of a boss fight, or having a troll wander in while you dodge Bonemass, isn't a fair challenge, just bad luck. While a boss fight is going on, this mod:

- **Delays raids.** No raid ("The ground is shaking", "You are being hunted", ...) can start. A raid that was due waits until the fight is over, then the game rolls for it as usual. Raids are delayed, not cancelled, so you get as many as in vanilla.
- **Stops new spawns near the fight.** No new creatures spawn naturally near the boss or the players fighting it.

The fight itself stays exactly as hard as in vanilla: the boss's own summons and attacks are untouched, creatures that were already there stay, and a raid that was already running when the fight began keeps going.

A boss fight starts when a boss is alerted or has a target and a player is within 60 m of it. It ends 20 seconds after that stops being true (the boss died, or everyone left).

Raids started from the console (`randomevent`, `event`) are not held back.

## Configuration

Change these in game with F1 (Configuration Manager) or in your mod manager's config editor:

- `DelayRaids`: hold back raids during boss fights (default on).
- `BlockSpawnsNearFight`: stop new natural spawns near boss fights (default on).
- `SpawnBlockRadius`: how close (in meters) to the boss or a player fighting it a creature may not spawn (default 80). The game spawns creatures 40-80 m from a player, so 80 blocks almost all of them.
- `FightDistance`: how close (in meters) a player must be to an alerted boss for it to count as a fight (default 60).
- `DebugLog`: writes boss fights, held raids and blocked spawns to the BepInEx log.

## Console commands

- `ubf_status`: shows the boss fights the mod sees and when the next raid roll is due.
- `ubf_raidsoon` (needs `devcommands`): makes the next raid roll due in 10 seconds, to see it wait during a fight.

## Multiplayer

**Note:** Multiplayer and dedicated servers haven't been tested yet. It's built to work there, but if something seems off, please report it on GitHub Issues.

Install it on the server and on every player's game. Raids are decided by the server, and spawns by the player who owns that area, so both need the mod. The server's settings are used for everyone (except `DebugLog`). The server warns when a player joins without the mod or with a different version.

Raids are held back for the whole world while any boss fight is going on, even for players far from it. They only wait for the fight to end; none are lost.

## Feedback and bug reports

Found a bug or have an idea? Open an issue on [GitHub](https://github.com/aetuss/UndisturbedBossFights/issues). If an enemy or raid does something odd, turn on `DebugLog` and include the matching lines from `BepInEx/LogOutput.log`.

## Made with AI

aetuss decided what this mod does and how it should work, and tested it in game. The code, the icon and this description were written by Claude, an AI model by Anthropic (through Claude Code).
