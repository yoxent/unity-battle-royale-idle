using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Maps
{
    public enum MapGameMode
    {
        BattleRoyale,
        CaptureTheFlag
    }

    public enum MapModePointType
    {
        BattleRoyaleSpawn,
        TeamSpawn,
        TeamBase,
        TeamFlag
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

        public bool IsBattleRoyale => mode == MapGameMode.BattleRoyale;
        public bool IsCaptureTheFlag => mode == MapGameMode.CaptureTheFlag;
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
}
