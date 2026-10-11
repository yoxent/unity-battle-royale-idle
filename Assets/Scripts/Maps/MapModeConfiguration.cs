using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    public enum GameModeOpposition
    {
        PvP,   // The opponent is another user's team.
        PvE,   // The opponent is an enemy spawn. No other user's team is required.
        PvPvE  // The match needs both another user's team and enemy spawns.
    }

    public enum MapGameMode
    {
        BattleRoyale,   // PvP. Last survivor. A shrinking safe zone, and no respawn.
        CaptureTheFlag, // PvP. Steal the enemy flag and return it to your base.
        TeamDeathmatch, // PvP. First team to the kill score. Respawn at the team spawn.
        Control,        // PvP. Hold several zones at once. Also called Strongholds.
        KingOfTheHill,  // PvP. Hold the active hill. Also called Hardpoint.
        Assault,        // PvP. Plant and defuse, or take a defended site. Also called Search and Destroy.
        Extraction,     // PvPvE. Other teams and hostile spawns both contest the route out.
        Oddball,        // PvP. Hold one relic to score. Also called Relic Hold.
        Infection,      // PvP. Survivors versus infected players. A kill moves the victim onto that team.
        Firefight,      // PvE. Players defend against enemy waves. Also called PvE Defense.
        BountyHunt,     // PvP. Hunt or protect the marked player. Also called VIP.
        Payload,        // PvP. Push a payload along a path. Also called Escort.
        Survival,       // PvE. Hold out forever. The horde grows stronger, and the score is time survived.
        Extermination,  // PvE. Clear every enemy spawn on the map.
        Raid            // PvE. Defeat the boss spawn.
    }

    public static class MapGameModes
    {
        public static bool HasPlacement(MapGameMode mode)
        {
            return mode is MapGameMode.BattleRoyale
                or MapGameMode.CaptureTheFlag
                or MapGameMode.TeamDeathmatch;
        }

        public static GameModeOpposition Opposition(MapGameMode mode)
        {
            return mode switch
            {
                MapGameMode.BattleRoyale => GameModeOpposition.PvP,
                MapGameMode.CaptureTheFlag => GameModeOpposition.PvP,
                MapGameMode.TeamDeathmatch => GameModeOpposition.PvP,
                MapGameMode.Control => GameModeOpposition.PvP,
                MapGameMode.KingOfTheHill => GameModeOpposition.PvP,
                MapGameMode.Assault => GameModeOpposition.PvP,
                MapGameMode.Extraction => GameModeOpposition.PvPvE,
                MapGameMode.Oddball => GameModeOpposition.PvP,
                MapGameMode.Infection => GameModeOpposition.PvP,
                MapGameMode.Firefight => GameModeOpposition.PvE,
                MapGameMode.BountyHunt => GameModeOpposition.PvP,
                MapGameMode.Payload => GameModeOpposition.PvP,
                MapGameMode.Survival => GameModeOpposition.PvE,
                MapGameMode.Extermination => GameModeOpposition.PvE,
                MapGameMode.Raid => GameModeOpposition.PvE,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }

    public enum MapModePointType
    {
        BattleRoyaleSpawn,
        TeamSpawn,
        TeamBase,
        TeamFlag,
        TeamDeathmatchSpawn
    }

    [CreateAssetMenu(
        fileName = "MapModeConfiguration",
        menuName = "Battle Royale/Maps/Map Mode Configuration")]
    public sealed class MapModeConfiguration : ScriptableObject
    {
        public MapDefinition map;
        public MapGameMode mode;
        public string variantName = "Default";

        [Min(1)]
        public int teamCount = 2;

        [Min(1)]
        public int teamSize = 1;

        public List<MapModePoint> battleRoyaleSpawnPoints = new();
        public List<MapModeTeamPoints> captureTheFlagTeams = new();
        public List<MapModeTeamSpawn> teamDeathmatchTeams = new();

        [Min(1)]
        public int scoreLimit = 50;

        [Min(0f)]
        public float respawnSeconds = 3f;

        public bool IsBattleRoyale => mode == MapGameMode.BattleRoyale;
        public bool IsCaptureTheFlag => mode == MapGameMode.CaptureTheFlag;
        public bool IsTeamDeathmatch => mode == MapGameMode.TeamDeathmatch;
    }

    [Serializable]
    public sealed class MapModePoint
    {
        public string id = "Spawn";
        public Vector3 position;
        [Min(0f)]
        public float radius;
    }

    [Serializable]
    public sealed class MapModeTeamPoints
    {
        public string teamId = "Team 1";
        public Vector3 spawnPosition;
        public Vector3 basePosition;
        public Vector3 flagPosition;
    }

    [Serializable]
    public sealed class MapModeTeamSpawn
    {
        public string teamId = "Team 1";
        public Vector3 spawnPosition;

        [Min(0f)]
        public float radius = 8f;
    }
}
