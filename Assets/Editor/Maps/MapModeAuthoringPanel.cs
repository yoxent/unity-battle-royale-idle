#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BattleRoyale.GameModes.Elimination;
using BattleRoyale.GameModes.Deathmatch;
using BattleRoyale.Maps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleRoyale.Maps.EditorTools
{
    [Serializable]
    internal sealed class MapModeAuthoringPanel
    {
        private const string MarkerRootName = MapModeMarkerCleanup.MarkerRootName;
        private const string MarkerDiscName = "MarkerDisc";
        private const float MinimumMarkerDiameter = 8f;
        private const float NewSpawnRingRadius = 36f;
        private const string MarkerMaterialPath = "Assets/Materials/MarkerCube.mat";

        [SerializeField] private MapGameMode selectedMode = MapGameMode.BattleRoyale;
        [SerializeField] private MapModeConfiguration selectedConfiguration;
        [SerializeField] private string variantName = "Solo";
        [SerializeField] private int teamCount = 20;
        [SerializeField] private int teamSize = 1;
        [SerializeField] private int scoreLimit = DeathmatchRules.DefaultScoreLimit;
        [SerializeField] private float respawnSeconds =
            DeathmatchRules.DefaultRespawnSeconds;
        [SerializeField] private int roundWinLimit =
            EliminationRules.DefaultRoundWinLimit;
        [SerializeField] private float roundSeconds =
            EliminationRules.DefaultRoundSeconds;
        [SerializeField] private float intermissionSeconds =
            EliminationRules.DefaultIntermissionSeconds;
        [SerializeField] private bool editPointsInSceneView = true;

        [NonSerialized] private MapDefinition activeMap;
        [NonSerialized] private MapDefinition lastMap;
        [NonSerialized] private bool allowAutomaticConfigurationSelection = true;
        [NonSerialized] private bool sceneGuiRegistered;

        public void Draw(MapDefinition mapDefinition, string definitionsFolder)
        {
            activeMap = mapDefinition;
            if (lastMap != mapDefinition)
            {
                lastMap = mapDefinition;
                allowAutomaticConfigurationSelection = true;
                StopSceneGui();
                BindSavedSettings(mapDefinition);
            }

            if (mapDefinition == null)
            {
                StopSceneGui();
                return;
            }

            mapDefinition.supportedGameModes ??= new List<MapGameMode>();
            mapDefinition.modeConfigurations ??= new List<MapModeConfiguration>();
            editPointsInSceneView = mapDefinition.editPointsInSceneView;

            if (AuthoringSectionHeader.Draw("Game Modes"))
            {
            DrawSupportedModes(mapDefinition);

            var supportedModes = GetSupportedModes(mapDefinition);
            if (supportedModes.Count == 0)
            {
                StopSceneGui();
                EditorGUILayout.HelpBox(
                    "Select at least one game mode that this map supports.",
                    MessageType.None);
                return;
            }

            if (!mapDefinition.SupportsMode(selectedMode))
            {
                selectedMode = supportedModes[0];
                selectedConfiguration = FindConfiguration(mapDefinition, selectedMode);
                allowAutomaticConfigurationSelection = true;
                LoadSelectedConfiguration();
            }

            var previousMode = selectedMode;
            selectedMode = (MapGameMode)EditorGUILayout.EnumPopup(
                "Mode",
                selectedMode);
            if (previousMode != selectedMode)
            {
                InitializeDraftForMode();
                selectedConfiguration = FindConfiguration(mapDefinition, selectedMode);
                allowAutomaticConfigurationSelection = true;
                LoadSelectedConfiguration();
            }

            if (!MapGameModes.HasPlacement(selectedMode))
            {
                StopSceneGui();
                EditorGUILayout.HelpBox(
                    "This mode is listed so maps can reserve it. Placement and match rules are not implemented yet.",
                    MessageType.None);
                return;
            }

            if (selectedConfiguration == null &&
                allowAutomaticConfigurationSelection)
            {
                var existingConfiguration = FindConfiguration(mapDefinition, selectedMode);
                if (existingConfiguration != null)
                {
                    selectedConfiguration = existingConfiguration;
                    LoadSelectedConfiguration();
                }
            }

            var previousConfiguration = selectedConfiguration;
            selectedConfiguration = (MapModeConfiguration)EditorGUILayout.ObjectField(
                "Mode Configuration",
                selectedConfiguration,
                typeof(MapModeConfiguration),
                false);
            if (previousConfiguration != selectedConfiguration)
            {
                allowAutomaticConfigurationSelection = selectedConfiguration != null;
                LoadSelectedConfiguration();
            }

            if (selectedConfiguration != null &&
                (selectedConfiguration.map != mapDefinition ||
                 selectedConfiguration.mode != selectedMode))
            {
                EditorGUILayout.HelpBox(
                    "The selected configuration belongs to a different map or mode.",
                    MessageType.Error);
                StopSceneGui();
                return;
            }
            }

            DrawConfigurationSettings(mapDefinition, definitionsFolder);
            DrawConfigurationPoints(mapDefinition);
        }

        public void Dispose()
        {
            StopSceneGui();
        }

        public bool ValidateForGeneration(
            MapDefinition mapDefinition,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (mapDefinition == null ||
                mapDefinition.modeConfigurations == null)
            {
                return true;
            }

            foreach (var configuration in mapDefinition.modeConfigurations)
            {
                if (configuration == null ||
                    configuration.map != mapDefinition ||
                    !mapDefinition.SupportsMode(configuration.mode))
                {
                    continue;
                }

                if (configuration is BattleRoyaleModeConfiguration battleRoyale &&
                    (battleRoyale.spawnPoints == null ||
                     battleRoyale.spawnPoints.Count == 0))
                {
                    errorMessage =
                        $"Battle Royale configuration '{configuration.name}' needs at least one starting spawn point.";
                    return false;
                }

                if (configuration is CaptureTheFlagModeConfiguration captureTheFlag &&
                    (captureTheFlag.teams == null ||
                     captureTheFlag.teams.Count <
                     Mathf.Max(2, configuration.teamCount)))
                {
                    errorMessage =
                        $"Capture the Flag configuration '{configuration.name}' needs spawn, base, and flag points for every team.";
                    return false;
                }

                if (configuration is DeathmatchModeConfiguration deathmatch &&
                    (deathmatch.teams == null ||
                     deathmatch.teams.Count <
                     DeathmatchRules.RequiredTeamCount(configuration.teamCount)))
                {
                    errorMessage =
                        $"Deathmatch configuration '{configuration.name}' needs a spawn point for every team.";
                    return false;
                }

                if (configuration is EliminationModeConfiguration elimination &&
                    (elimination.teams == null ||
                     elimination.teams.Count <
                     EliminationRules.RequiredTeamCount(configuration.teamCount)))
                {
                    errorMessage =
                        $"Elimination configuration '{configuration.name}' needs a spawn point for every team.";
                    return false;
                }
            }

            return true;
        }

        public void GenerateMarkers(Transform mapRoot, MapDefinition mapDefinition)
        {
            if (mapRoot == null || mapDefinition == null)
            {
                return;
            }

            var existingMarkerRoot = mapRoot.Find(MarkerRootName);
            if (existingMarkerRoot != null)
            {
                Undo.DestroyObjectImmediate(existingMarkerRoot.gameObject);
            }

            if (mapDefinition.modeConfigurations == null ||
                mapDefinition.modeConfigurations.Count == 0)
            {
                return;
            }

            var markerRoot = new GameObject(MarkerRootName);
            markerRoot.transform.SetParent(mapRoot, false);

            foreach (var configuration in mapDefinition.modeConfigurations)
            {
                if (configuration == null ||
                    configuration.map != mapDefinition ||
                    !mapDefinition.SupportsMode(configuration.mode))
                {
                    continue;
                }

                var configurationRoot = new GameObject(
                    $"{configuration.mode}_{SanitizeName(configuration.variantName)}");
                configurationRoot.transform.SetParent(markerRoot.transform, false);

                if (configuration is BattleRoyaleModeConfiguration battleRoyale)
                {
                    AddBattleRoyaleMarkers(configurationRoot.transform, battleRoyale);
                }
                else if (configuration is CaptureTheFlagModeConfiguration captureTheFlag)
                {
                    AddCaptureTheFlagMarkers(configurationRoot.transform, captureTheFlag);
                }
                else if (configuration is DeathmatchModeConfiguration deathmatch)
                {
                    AddDeathmatchMarkers(configurationRoot.transform, deathmatch);
                }
                else if (configuration is EliminationModeConfiguration elimination)
                {
                    AddEliminationMarkers(configurationRoot.transform, elimination);
                }
            }
        }

        private void DrawSupportedModes(MapDefinition mapDefinition)
        {
            foreach (MapGameMode mode in Enum.GetValues(typeof(MapGameMode)))
            {
                var isSupported = mapDefinition.SupportsMode(mode);
                var shouldSupport = EditorGUILayout.ToggleLeft(
                    ObjectNames.NicifyVariableName(mode.ToString()),
                    isSupported);
                if (shouldSupport == isSupported)
                {
                    continue;
                }

                Undo.RecordObject(mapDefinition, "Change Supported Map Modes");
                if (shouldSupport)
                {
                    mapDefinition.supportedGameModes.Add(mode);
                }
                else
                {
                    mapDefinition.supportedGameModes.Remove(mode);
                    MapModeMarkerCleanup.RemoveFromDefinition(mapDefinition, mode);
                    if (selectedMode == mode)
                    {
                        selectedConfiguration = null;
                        allowAutomaticConfigurationSelection = true;
                        StopSceneGui();
                    }
                }

                EditorUtility.SetDirty(mapDefinition);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawConfigurationSettings(
            MapDefinition mapDefinition,
            string definitionsFolder)
        {
            if (!AuthoringSectionHeader.Draw(
                    $"{ObjectNames.NicifyVariableName(selectedMode.ToString())} Settings"))
            {
                return;
            }


            variantName = EditorGUILayout.TextField("Variant", variantName);
            teamCount = Mathf.Max(
                MinimumTeamCount(selectedMode),
                EditorGUILayout.IntField("Team Count", teamCount));
            teamSize = Mathf.Max(
                1,
                EditorGUILayout.IntField("Players Per Team", teamSize));
            if (selectedMode == MapGameMode.BattleRoyale)
            {
                EditorGUILayout.HelpBox(
                    "Battle Royale creates one starting spawn point for each team. Move the circles in the scene, then save those positions back to the map data.",
                    MessageType.None);
            }
            else if (selectedMode == MapGameMode.Deathmatch)
            {
                scoreLimit = Mathf.Max(
                    1,
                    EditorGUILayout.IntField("Score Limit", scoreLimit));
                respawnSeconds = Mathf.Max(
                    0f,
                    EditorGUILayout.FloatField("Respawn Seconds", respawnSeconds));
                EditorGUILayout.HelpBox(
                    "Deathmatch creates one spawn for each team. Eliminations score for the killer's team, and characters respawn at that spawn.",
                    MessageType.None);
            }
            else if (selectedMode == MapGameMode.Elimination)
            {
                roundWinLimit = Mathf.Max(
                    1,
                    EditorGUILayout.IntField("Round Wins To Win", roundWinLimit));
                roundSeconds = Mathf.Max(
                    1f,
                    EditorGUILayout.FloatField("Round Seconds", roundSeconds));
                intermissionSeconds = Mathf.Max(
                    0f,
                    EditorGUILayout.FloatField("Intermission Seconds", intermissionSeconds));
                EditorGUILayout.HelpBox(
                    "Elimination creates one spawn for each team. A team wins the round by eliminating every other team. Nobody respawns until the next round.",
                    MessageType.None);
            }

            if (selectedConfiguration != null)
            {
                ApplySettingsToConfiguration();
            }

            if (GUILayout.Button(
                    selectedConfiguration == null
                        ? "Create Mode Configuration"
                        : "Save Mode Configuration"))
            {
                CreateOrUpdateConfiguration(mapDefinition, definitionsFolder);
            }

            if (selectedConfiguration != null &&
                GUILayout.Button("Create New Mode Variant"))
            {
                selectedConfiguration = null;
                allowAutomaticConfigurationSelection = false;
                InitializeDraftForMode();
                StopSceneGui();
            }

            if (selectedConfiguration == null)
            {
                EditorGUILayout.HelpBox(
                    "Create a mode configuration to place this mode's points.",
                    MessageType.None);
            }
        }

        private void DrawConfigurationPoints(MapDefinition mapDefinition)
        {
            if (selectedConfiguration == null)
            {
                StopSceneGui();
                return;
            }

            if (selectedConfiguration is BattleRoyaleModeConfiguration)
            {
                DrawBattleRoyalePoints(mapDefinition);
            }
            else if (selectedConfiguration is CaptureTheFlagModeConfiguration)
            {
                DrawCaptureTheFlagPoints(mapDefinition);
            }
            else if (selectedConfiguration is DeathmatchModeConfiguration)
            {
                DrawDeathmatchPoints(mapDefinition);
            }
            else if (selectedConfiguration is EliminationModeConfiguration)
            {
                DrawEliminationPoints(mapDefinition);
            }
            else
            {
                StopSceneGui();
            }
        }

        private void DrawBattleRoyalePoints(MapDefinition mapDefinition)
        {
            if (selectedConfiguration is not BattleRoyaleModeConfiguration battleRoyale)
            {
                return;
            }

            battleRoyale.spawnPoints ??= new List<MapModePoint>();
            if (!AuthoringSectionHeader.Draw("Starting Spawn Points"))
            {
                StopSceneGui();
                return;
            }


            for (var i = 0; i < battleRoyale.spawnPoints.Count; i++)
            {
                var point = battleRoyale.spawnPoints[i];
                if (point == null)
                {
                    point = new MapModePoint();
                    battleRoyale.spawnPoints[i] = point;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Spawn {i + 1}", GUILayout.Width(60f));
                    EditorGUI.BeginChangeCheck();
                    var pointId = EditorGUILayout.TextField(point.id);
                    var pointIdChanged = EditorGUI.EndChangeCheck();
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        teamCount = Mathf.Max(1, teamCount - 1);
                        Undo.RecordObject(battleRoyale, "Remove Battle Royale Spawn Point");
                        battleRoyale.teamCount = teamCount;
                        EnsureBattleRoyaleSpawnPoints(battleRoyale);
                        EditorUtility.SetDirty(battleRoyale);
                        SyncBattleRoyaleMarkers(battleRoyale);
                        i--;
                        continue;
                    }

                    if (pointIdChanged)
                    {
                        Undo.RecordObject(
                            battleRoyale,
                            "Rename Battle Royale Spawn Point");
                        point.id = pointId;
                        EditorUtility.SetDirty(battleRoyale);
                    }
                }

                DrawPointPosition(battleRoyale, point, "Position");
                EditorGUI.BeginChangeCheck();
                var radius = Mathf.Max(
                    0f,
                    EditorGUILayout.FloatField("Scatter Radius", point.radius));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        battleRoyale,
                        "Edit Battle Royale Spawn Radius");
                    point.radius = radius;
                    EditorUtility.SetDirty(battleRoyale);
                    UpdateMarkerDiscDiameter(
                        battleRoyale,
                        MapModePointType.BattleRoyaleSpawn,
                        i,
                        radius);
                }
            }

            if (GUILayout.Button("Add Battle Royale Spawn Point"))
            {
                teamCount++;
                Undo.RecordObject(battleRoyale, "Add Battle Royale Spawn Point");
                battleRoyale.teamCount = teamCount;
                EnsureBattleRoyaleSpawnPoints(battleRoyale);
                EditorUtility.SetDirty(battleRoyale);
                SyncBattleRoyaleMarkers(battleRoyale);
            }

            DrawPointSceneControls(mapDefinition);
        }

        private void DrawCaptureTheFlagPoints(MapDefinition mapDefinition)
        {
            if (selectedConfiguration is not CaptureTheFlagModeConfiguration captureTheFlag)
            {
                return;
            }

            captureTheFlag.teams ??= new List<MapModeTeamPoints>();
            EnsureCaptureTheFlagTeams(captureTheFlag);
            if (!AuthoringSectionHeader.Draw("Team Points"))
            {
                StopSceneGui();
                return;
            }


            for (var i = 0; i < captureTheFlag.teams.Count; i++)
            {
                var team = captureTheFlag.teams[i];
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField(team.teamId, EditorStyles.miniBoldLabel);
                var teamId = EditorGUILayout.TextField("Team Id", team.teamId);
                var spawnPosition = EditorGUILayout.Vector3Field(
                    "Team Spawn",
                    team.spawnPosition);
                var basePosition = EditorGUILayout.Vector3Field(
                    "Base",
                    team.basePosition);
                var flagPosition = EditorGUILayout.Vector3Field(
                    "Flag",
                    team.flagPosition);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        captureTheFlag,
                        "Edit Capture The Flag Points");
                    team.teamId = teamId;
                    team.spawnPosition = spawnPosition;
                    team.basePosition = basePosition;
                    team.flagPosition = flagPosition;
                    EditorUtility.SetDirty(captureTheFlag);
                    UpdateMarkerTransform(
                        captureTheFlag,
                        MapModePointType.TeamSpawn,
                        i,
                        i,
                        spawnPosition);
                    UpdateMarkerTransform(
                        captureTheFlag,
                        MapModePointType.TeamBase,
                        i,
                        i,
                        basePosition);
                    UpdateMarkerTransform(
                        captureTheFlag,
                        MapModePointType.TeamFlag,
                        i,
                        i,
                        flagPosition);
                }
                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("Reset Team Points To Map Center"))
            {
                Undo.RecordObject(captureTheFlag, "Reset Capture The Flag Points");
                for (var i = 0; i < captureTheFlag.teams.Count; i++)
                {
                    var center = GetMapCenter(mapDefinition);
                    captureTheFlag.teams[i].spawnPosition = center;
                    captureTheFlag.teams[i].basePosition = center;
                    captureTheFlag.teams[i].flagPosition = center;
                }

                EditorUtility.SetDirty(captureTheFlag);
            }

            DrawPointSceneControls(mapDefinition);
        }

        private void DrawDeathmatchPoints(MapDefinition mapDefinition)
        {
            if (selectedConfiguration is not DeathmatchModeConfiguration deathmatch)
            {
                return;
            }

            deathmatch.teams ??= new List<MapTeamSpawn>();
            DeathmatchRules.EnsureTeamSpawns(deathmatch);
            if (!AuthoringSectionHeader.Draw("Team Spawns"))
            {
                StopSceneGui();
                return;
            }


            for (var i = 0; i < deathmatch.teams.Count; i++)
            {
                var team = deathmatch.teams[i];
                if (team == null)
                {
                    team = new MapTeamSpawn();
                    deathmatch.teams[i] = team;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(team.teamId, EditorStyles.miniBoldLabel);
                    if (deathmatch.teams.Count >
                            DeathmatchRules.MinimumTeamCount &&
                        GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        teamCount = Mathf.Max(
                            DeathmatchRules.MinimumTeamCount,
                            teamCount - 1);
                        Undo.RecordObject(
                            deathmatch,
                            "Remove Deathmatch Spawn");
                        deathmatch.teamCount = teamCount;
                        DeathmatchRules.EnsureTeamSpawns(deathmatch);
                        EditorUtility.SetDirty(deathmatch);
                        SyncDeathmatchMarkers(deathmatch);
                        i--;
                        continue;
                    }
                }

                EditorGUI.BeginChangeCheck();
                var teamId = EditorGUILayout.TextField("Team Id", team.teamId);
                var spawnPosition = EditorGUILayout.Vector3Field(
                    "Team Spawn",
                    team.spawnPosition);
                var radius = Mathf.Max(
                    0f,
                    EditorGUILayout.FloatField("Scatter Radius", team.radius));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        deathmatch,
                        "Edit Deathmatch Spawn");
                    team.teamId = teamId;
                    team.spawnPosition = spawnPosition;
                    team.radius = radius;
                    EditorUtility.SetDirty(deathmatch);
                    UpdateMarkerTransform(
                        deathmatch,
                        MapModePointType.DeathmatchSpawn,
                        i,
                        i,
                        spawnPosition);
                    UpdateMarkerDiscDiameter(
                        deathmatch,
                        MapModePointType.DeathmatchSpawn,
                        i,
                        radius);
                }

                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("Add Team Spawn"))
            {
                teamCount++;
                Undo.RecordObject(deathmatch, "Add Deathmatch Spawn");
                deathmatch.teamCount = teamCount;
                DeathmatchRules.EnsureTeamSpawns(deathmatch);
                EditorUtility.SetDirty(deathmatch);
                SyncDeathmatchMarkers(deathmatch);
            }

            if (GUILayout.Button("Reset Team Spawns Around Map Center"))
            {
                Undo.RecordObject(
                    deathmatch,
                    "Reset Deathmatch Spawns");
                var center = GetMapCenter(mapDefinition);
                var count = deathmatch.teams.Count;
                for (var i = 0; i < count; i++)
                {
                    var team = deathmatch.teams[i];
                    if (team == null)
                    {
                        continue;
                    }

                    var angle = (i + 0.5f) / count * Mathf.PI * 2f;
                    team.spawnPosition = center + new Vector3(
                        Mathf.Cos(angle) * DeathmatchRules.SpawnRingRadius,
                        0f,
                        Mathf.Sin(angle) * DeathmatchRules.SpawnRingRadius);
                    UpdateMarkerTransform(
                        deathmatch,
                        MapModePointType.DeathmatchSpawn,
                        i,
                        i,
                        team.spawnPosition);
                }

                EditorUtility.SetDirty(deathmatch);
            }

            DrawPointSceneControls(mapDefinition);
        }

        private void DrawEliminationPoints(MapDefinition mapDefinition)
        {
            if (selectedConfiguration is not EliminationModeConfiguration elimination)
            {
                return;
            }

            elimination.teams ??= new List<MapTeamSpawn>();
            EliminationRules.EnsureTeamSpawns(elimination);
            if (!AuthoringSectionHeader.Draw("Team Spawns"))
            {
                StopSceneGui();
                return;
            }


            for (var i = 0; i < elimination.teams.Count; i++)
            {
                var team = elimination.teams[i];
                if (team == null)
                {
                    team = new MapTeamSpawn();
                    elimination.teams[i] = team;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(team.teamId, EditorStyles.miniBoldLabel);
                    if (elimination.teams.Count >
                            EliminationRules.MinimumTeamCount &&
                        GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        teamCount = Mathf.Max(
                            EliminationRules.MinimumTeamCount,
                            teamCount - 1);
                        Undo.RecordObject(
                            elimination,
                            "Remove Elimination Spawn");
                        elimination.teamCount = teamCount;
                        EliminationRules.EnsureTeamSpawns(elimination);
                        EditorUtility.SetDirty(elimination);
                        SyncEliminationMarkers(elimination);
                        i--;
                        continue;
                    }
                }

                EditorGUI.BeginChangeCheck();
                var teamId = EditorGUILayout.TextField("Team Id", team.teamId);
                var spawnPosition = EditorGUILayout.Vector3Field(
                    "Team Spawn",
                    team.spawnPosition);
                var radius = Mathf.Max(
                    0f,
                    EditorGUILayout.FloatField("Scatter Radius", team.radius));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        elimination,
                        "Edit Elimination Spawn");
                    team.teamId = teamId;
                    team.spawnPosition = spawnPosition;
                    team.radius = radius;
                    EditorUtility.SetDirty(elimination);
                    UpdateMarkerTransform(
                        elimination,
                        MapModePointType.EliminationSpawn,
                        i,
                        i,
                        spawnPosition);
                    UpdateMarkerDiscDiameter(
                        elimination,
                        MapModePointType.EliminationSpawn,
                        i,
                        radius);
                }

                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("Add Team Spawn"))
            {
                teamCount++;
                Undo.RecordObject(elimination, "Add Elimination Spawn");
                elimination.teamCount = teamCount;
                EliminationRules.EnsureTeamSpawns(elimination);
                EditorUtility.SetDirty(elimination);
                SyncEliminationMarkers(elimination);
            }

            if (GUILayout.Button("Reset Team Spawns Around Map Center"))
            {
                Undo.RecordObject(
                    elimination,
                    "Reset Elimination Spawns");
                var center = GetMapCenter(mapDefinition);
                var count = elimination.teams.Count;
                for (var i = 0; i < count; i++)
                {
                    var team = elimination.teams[i];
                    if (team == null)
                    {
                        continue;
                    }

                    var angle = (i + 0.5f) / count * Mathf.PI * 2f;
                    team.spawnPosition = center + new Vector3(
                        Mathf.Cos(angle) * EliminationRules.SpawnRingRadius,
                        0f,
                        Mathf.Sin(angle) * EliminationRules.SpawnRingRadius);
                    UpdateMarkerTransform(
                        elimination,
                        MapModePointType.EliminationSpawn,
                        i,
                        i,
                        team.spawnPosition);
                }

                EditorUtility.SetDirty(elimination);
            }

            DrawPointSceneControls(mapDefinition);
        }

        private void DrawPointSceneControls(MapDefinition mapDefinition)
        {
            if (selectedConfiguration != null)
            {
                EnsureMarkerDiscs(selectedConfiguration);
            }

            if (selectedConfiguration != null &&
                GUILayout.Button("Save Point Positions To Map Data"))
            {
                SaveScenePointsToConfiguration();
            }

            var editPoints = EditorGUILayout.Toggle(
                new GUIContent(
                    "Edit Points In Scene View",
                    "Show position handles for the selected configuration in the Scene view."),
                editPointsInSceneView);
            if (editPoints != mapDefinition.editPointsInSceneView)
            {
                Undo.RecordObject(mapDefinition, "Edit Points In Scene View");
                mapDefinition.editPointsInSceneView = editPoints;
                EditorUtility.SetDirty(mapDefinition);
            }

            editPointsInSceneView = editPoints;
            UpdateSceneGuiRegistration();
        }

        private void DrawPointPosition(
            BattleRoyaleModeConfiguration configuration,
            MapModePoint point,
            string label)
        {
            EditorGUI.BeginChangeCheck();
            var position = EditorGUILayout.Vector3Field(label, point.position);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(configuration, "Move Battle Royale Spawn Point");
            point.position = position;
            EditorUtility.SetDirty(configuration);
            UpdateMarkerTransform(
                configuration,
                MapModePointType.BattleRoyaleSpawn,
                configuration.spawnPoints.IndexOf(point),
                -1,
                position);
        }

        private void CreateOrUpdateConfiguration(
            MapDefinition mapDefinition,
            string definitionsFolder)
        {
            var normalizedVariantName = string.IsNullOrWhiteSpace(variantName)
                ? "Default"
                : variantName.Trim();
            var safeVariantName = SanitizeName(normalizedVariantName);
            var folder = EnsureDefinitionsFolder(mapDefinition.layoutJson);
            if (string.IsNullOrEmpty(folder))
            {
                folder = definitionsFolder;
            }

            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            if (selectedConfiguration == null)
            {
                var mapName = mapDefinition.name.Replace("_Map", string.Empty);
                var path = $"{folder}/{mapName}_{selectedMode}_{safeVariantName}.asset";
                selectedConfiguration = AssetDatabase.LoadAssetAtPath<MapModeConfiguration>(path);
                if (selectedConfiguration == null)
                {
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    selectedConfiguration = CreateConfigurationInstance(selectedMode);
                    selectedConfiguration.name =
                        $"{mapName}_{selectedMode}_{safeVariantName}";
                    AssetDatabase.CreateAsset(selectedConfiguration, path);
                }
            }

            selectedConfiguration.map = mapDefinition;
            selectedConfiguration.mode = selectedMode;
            selectedConfiguration.variantName = normalizedVariantName;
            selectedConfiguration.teamCount = teamCount;
            selectedConfiguration.teamSize = teamSize;
            EnsureLists(selectedConfiguration);
            ApplySettingsToConfiguration();

            if (!mapDefinition.supportedGameModes.Contains(selectedMode))
            {
                mapDefinition.supportedGameModes.Add(selectedMode);
            }

            if (!mapDefinition.modeConfigurations.Contains(selectedConfiguration))
            {
                mapDefinition.modeConfigurations.Add(selectedConfiguration);
            }

            allowAutomaticConfigurationSelection = true;
            EditorUtility.SetDirty(selectedConfiguration);
            EditorUtility.SetDirty(mapDefinition);
            AssetDatabase.SaveAssets();
            Selection.activeObject = selectedConfiguration;
        }

        private static MapModeConfiguration CreateConfigurationInstance(MapGameMode mode)
        {
            return mode switch
            {
                MapGameMode.BattleRoyale =>
                    ScriptableObject.CreateInstance<BattleRoyaleModeConfiguration>(),
                MapGameMode.CaptureTheFlag =>
                    ScriptableObject.CreateInstance<CaptureTheFlagModeConfiguration>(),
                MapGameMode.Deathmatch =>
                    ScriptableObject.CreateInstance<DeathmatchModeConfiguration>(),
                MapGameMode.Elimination =>
                    ScriptableObject.CreateInstance<EliminationModeConfiguration>(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    "This mode has no configuration asset type yet.")
            };
        }

        private void ApplySettingsToConfiguration()
        {
            if (selectedConfiguration == null)
            {
                return;
            }

            EnsureLists(selectedConfiguration);
            var battleRoyale = selectedConfiguration as BattleRoyaleModeConfiguration;
            var captureTheFlag = selectedConfiguration as CaptureTheFlagModeConfiguration;
            var deathmatch = selectedConfiguration as DeathmatchModeConfiguration;
            var elimination = selectedConfiguration as EliminationModeConfiguration;
            var nextVariant = string.IsNullOrWhiteSpace(variantName)
                ? "Default"
                : variantName.Trim();
            var nextTeamCount = Mathf.Max(MinimumTeamCount(selectedMode), teamCount);
            var nextTeamSize = Mathf.Max(1, teamSize);
            var nextScoreLimit = Mathf.Max(1, scoreLimit);
            var nextRespawnSeconds = Mathf.Max(0f, respawnSeconds);
            var nextRoundWinLimit = Mathf.Max(1, roundWinLimit);
            var nextRoundSeconds = Mathf.Max(1f, roundSeconds);
            var nextIntermissionSeconds = Mathf.Max(0f, intermissionSeconds);
            var spawnCountChanged = false;
            var settingsMatch =
                selectedConfiguration.variantName == nextVariant &&
                selectedConfiguration.teamCount == nextTeamCount &&
                selectedConfiguration.teamSize == nextTeamSize;
            if (deathmatch != null)
            {
                settingsMatch = settingsMatch &&
                    deathmatch.scoreLimit == nextScoreLimit &&
                    Mathf.Approximately(
                        deathmatch.respawnSeconds,
                        nextRespawnSeconds);
            }
            else if (elimination != null)
            {
                settingsMatch = settingsMatch &&
                    elimination.roundWinLimit == nextRoundWinLimit &&
                    Mathf.Approximately(
                        elimination.roundSeconds,
                        nextRoundSeconds) &&
                    Mathf.Approximately(
                        elimination.intermissionSeconds,
                        nextIntermissionSeconds);
            }

            if (settingsMatch)
            {
                if (captureTheFlag != null)
                {
                    EnsureCaptureTheFlagTeams(captureTheFlag);
                }
                else if (deathmatch != null)
                {
                    spawnCountChanged = DeathmatchRules.EnsureTeamSpawns(deathmatch);
                }
                else if (elimination != null)
                {
                    spawnCountChanged = EliminationRules.EnsureTeamSpawns(elimination);
                }
                else if (battleRoyale != null &&
                         SpawnCountNeedsUpdate(battleRoyale))
                {
                    Undo.RecordObject(
                        selectedConfiguration,
                        "Update Battle Royale Spawn Points");
                    spawnCountChanged = EnsureBattleRoyaleSpawnPoints(battleRoyale);
                }

                if (spawnCountChanged)
                {
                    EditorUtility.SetDirty(selectedConfiguration);
                    SyncModeMarkers(selectedConfiguration);
                }

                return;
            }

            Undo.RecordObject(selectedConfiguration, "Update Map Mode Settings");
            selectedConfiguration.variantName = nextVariant;
            selectedConfiguration.teamCount = nextTeamCount;
            selectedConfiguration.teamSize = nextTeamSize;
            if (deathmatch != null)
            {
                deathmatch.scoreLimit = nextScoreLimit;
                deathmatch.respawnSeconds = nextRespawnSeconds;
                spawnCountChanged = DeathmatchRules.EnsureTeamSpawns(deathmatch);
            }
            else if (elimination != null)
            {
                elimination.roundWinLimit = nextRoundWinLimit;
                elimination.roundSeconds = nextRoundSeconds;
                elimination.intermissionSeconds = nextIntermissionSeconds;
                spawnCountChanged = EliminationRules.EnsureTeamSpawns(elimination);
            }
            else if (captureTheFlag != null)
            {
                EnsureCaptureTheFlagTeams(captureTheFlag);
            }
            else if (battleRoyale != null)
            {
                spawnCountChanged = EnsureBattleRoyaleSpawnPoints(battleRoyale);
            }

            EditorUtility.SetDirty(selectedConfiguration);
            if (spawnCountChanged)
            {
                SyncModeMarkers(selectedConfiguration);
            }
        }

        private static void SyncModeMarkers(MapModeConfiguration configuration)
        {
            if (configuration is BattleRoyaleModeConfiguration battleRoyale)
            {
                SyncBattleRoyaleMarkers(battleRoyale);
            }
            else if (configuration is DeathmatchModeConfiguration deathmatch)
            {
                SyncDeathmatchMarkers(deathmatch);
            }
            else if (configuration is EliminationModeConfiguration elimination)
            {
                SyncEliminationMarkers(elimination);
            }
        }

        internal int BoundTeamCount => teamCount;

        internal int BoundTeamSize => teamSize;

        internal int BoundScoreLimit => scoreLimit;

        internal float BoundRespawnSeconds => respawnSeconds;

        internal int BoundRoundWinLimit => roundWinLimit;

        internal float BoundRoundSeconds => roundSeconds;

        internal float BoundIntermissionSeconds => intermissionSeconds;

        internal string BoundVariantName => variantName;

        internal MapGameMode BoundMode => selectedMode;

        internal MapModeConfiguration BoundConfiguration => selectedConfiguration;

        internal bool BoundEditPointsInSceneView => editPointsInSceneView;

        internal void BindSavedSettings(MapDefinition mapDefinition)
        {
            selectedConfiguration = null;
            if (mapDefinition == null)
            {
                return;
            }

            mapDefinition.supportedGameModes ??= new List<MapGameMode>();
            mapDefinition.modeConfigurations ??= new List<MapModeConfiguration>();
            editPointsInSceneView = mapDefinition.editPointsInSceneView;
            var supportedModes = GetSupportedModes(mapDefinition);
            if (supportedModes.Count == 0)
            {
                return;
            }

            if (!mapDefinition.SupportsMode(selectedMode))
            {
                selectedMode = supportedModes[0];
            }

            selectedConfiguration = FindConfiguration(mapDefinition, selectedMode);
            LoadSelectedConfiguration();
        }

        private void LoadSelectedConfiguration()
        {
            if (selectedConfiguration == null)
            {
                InitializeDraftForMode();
                return;
            }

            selectedMode = selectedConfiguration.mode;
            variantName = selectedConfiguration.variantName;
            teamCount = selectedConfiguration.teamCount;
            teamSize = selectedConfiguration.teamSize;
            EnsureLists(selectedConfiguration);
            if (selectedConfiguration is CaptureTheFlagModeConfiguration captureTheFlag)
            {
                EnsureCaptureTheFlagTeams(captureTheFlag);
            }
            else if (selectedConfiguration is DeathmatchModeConfiguration deathmatch)
            {
                scoreLimit = Mathf.Max(1, deathmatch.scoreLimit);
                respawnSeconds = Mathf.Max(0f, deathmatch.respawnSeconds);
                DeathmatchRules.EnsureTeamSpawns(deathmatch);
            }
            else if (selectedConfiguration is EliminationModeConfiguration elimination)
            {
                roundWinLimit = Mathf.Max(1, elimination.roundWinLimit);
                roundSeconds = Mathf.Max(1f, elimination.roundSeconds);
                intermissionSeconds = Mathf.Max(0f, elimination.intermissionSeconds);
                EliminationRules.EnsureTeamSpawns(elimination);
            }
        }

        private void InitializeDraftForMode()
        {
            if (selectedMode == MapGameMode.BattleRoyale)
            {
                variantName = "Solo";
                teamCount = 20;
                teamSize = 1;
            }
            else if (selectedMode == MapGameMode.Deathmatch)
            {
                variantName = "Default";
                teamCount = DeathmatchRules.MinimumTeamCount;
                teamSize = DeathmatchRules.DefaultTeamSize;
                scoreLimit = DeathmatchRules.DefaultScoreLimit;
                respawnSeconds = DeathmatchRules.DefaultRespawnSeconds;
            }
            else if (selectedMode == MapGameMode.Elimination)
            {
                variantName = "Default";
                teamCount = EliminationRules.MinimumTeamCount;
                teamSize = EliminationRules.DefaultTeamSize;
                roundWinLimit = EliminationRules.DefaultRoundWinLimit;
                roundSeconds = EliminationRules.DefaultRoundSeconds;
                intermissionSeconds = EliminationRules.DefaultIntermissionSeconds;
            }
            else
            {
                variantName = "Default";
                teamCount = 2;
                teamSize = 3;
            }
        }

        private void UpdateSceneGuiRegistration()
        {
            var shouldRegister = editPointsInSceneView &&
                                 selectedConfiguration != null &&
                                 selectedConfiguration.map == activeMap &&
                                 selectedConfiguration.mode == selectedMode;
            if (shouldRegister == sceneGuiRegistered)
            {
                return;
            }

            if (shouldRegister)
            {
                SceneView.duringSceneGui += OnSceneGUI;
                sceneGuiRegistered = true;
            }
            else
            {
                StopSceneGui();
            }

            SceneView.RepaintAll();
        }

        private void StopSceneGui()
        {
            if (!sceneGuiRegistered)
            {
                return;
            }

            SceneView.duringSceneGui -= OnSceneGUI;
            sceneGuiRegistered = false;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (selectedConfiguration == null ||
                selectedConfiguration.map == null ||
                selectedConfiguration.map != activeMap)
            {
                return;
            }

            var mapRoot = FindMapRoot(selectedConfiguration.map);
            if (mapRoot == null)
            {
                Handles.BeginGUI();
                EditorGUILayout.HelpBox(
                    "Generate this map root to edit mode points in the Scene view.",
                    MessageType.Info);
                Handles.EndGUI();
                return;
            }

            if (selectedConfiguration is BattleRoyaleModeConfiguration)
            {
                DrawBattleRoyaleHandles(mapRoot);
            }
            else if (selectedConfiguration is CaptureTheFlagModeConfiguration)
            {
                DrawCaptureTheFlagHandles(mapRoot);
            }
            else if (selectedConfiguration is DeathmatchModeConfiguration)
            {
                DrawDeathmatchHandles(mapRoot);
            }
            else if (selectedConfiguration is EliminationModeConfiguration)
            {
                DrawEliminationHandles(mapRoot);
            }
        }

        private void DrawBattleRoyaleHandles(Transform mapRoot)
        {
            if (selectedConfiguration is not BattleRoyaleModeConfiguration battleRoyale)
            {
                return;
            }

            if (battleRoyale.spawnPoints == null)
            {
                return;
            }

            for (var i = 0; i < battleRoyale.spawnPoints.Count; i++)
            {
                var point = battleRoyale.spawnPoints[i];
                if (point == null)
                {
                    continue;
                }

                DrawPositionHandle(
                    mapRoot,
                    battleRoyale,
                    MapModePointType.BattleRoyaleSpawn,
                    i,
                    -1,
                    $"BR Spawn {i + 1}",
                    Color.cyan,
                    ref point.position);
            }
        }

        private void DrawCaptureTheFlagHandles(Transform mapRoot)
        {
            if (selectedConfiguration is not CaptureTheFlagModeConfiguration captureTheFlag)
            {
                return;
            }

            if (captureTheFlag.teams == null)
            {
                return;
            }

            for (var i = 0; i < captureTheFlag.teams.Count; i++)
            {
                var team = captureTheFlag.teams[i];
                if (team == null)
                {
                    continue;
                }

                DrawPositionHandle(
                    mapRoot,
                    captureTheFlag,
                    MapModePointType.TeamSpawn,
                    i,
                    i,
                    $"{team.teamId} Spawn",
                    Color.green,
                    ref team.spawnPosition);
                DrawPositionHandle(
                    mapRoot,
                    captureTheFlag,
                    MapModePointType.TeamBase,
                    i,
                    i,
                    $"{team.teamId} Base",
                    Color.yellow,
                    ref team.basePosition);
                DrawPositionHandle(
                    mapRoot,
                    captureTheFlag,
                    MapModePointType.TeamFlag,
                    i,
                    i,
                    $"{team.teamId} Flag",
                    Color.red,
                    ref team.flagPosition);
            }
        }

        private void DrawDeathmatchHandles(Transform mapRoot)
        {
            if (selectedConfiguration is not DeathmatchModeConfiguration deathmatch)
            {
                return;
            }

            if (deathmatch.teams == null)
            {
                return;
            }

            for (var i = 0; i < deathmatch.teams.Count; i++)
            {
                var team = deathmatch.teams[i];
                if (team == null)
                {
                    continue;
                }

                DrawPositionHandle(
                    mapRoot,
                    deathmatch,
                    MapModePointType.DeathmatchSpawn,
                    i,
                    i,
                    $"{team.teamId} Spawn",
                    new Color(0.65f, 0.35f, 1f),
                    ref team.spawnPosition);
            }
        }

        private void DrawEliminationHandles(Transform mapRoot)
        {
            if (selectedConfiguration is not EliminationModeConfiguration elimination)
            {
                return;
            }

            if (elimination.teams == null)
            {
                return;
            }

            for (var i = 0; i < elimination.teams.Count; i++)
            {
                var team = elimination.teams[i];
                if (team == null)
                {
                    continue;
                }

                DrawPositionHandle(
                    mapRoot,
                    elimination,
                    MapModePointType.EliminationSpawn,
                    i,
                    i,
                    $"{team.teamId} Spawn",
                    new Color(1f, 0.72f, 0.15f),
                    ref team.spawnPosition);
            }
        }

        private static void DrawPositionHandle(
            Transform mapRoot,
            MapModeConfiguration configuration,
            MapModePointType pointType,
            int index,
            int teamIndex,
            string label,
            Color color,
            ref Vector3 localPosition)
        {
            var worldPosition = mapRoot.TransformPoint(localPosition);
            Handles.color = color;
            Handles.SphereHandleCap(
                0,
                worldPosition,
                Quaternion.identity,
                HandleUtility.GetHandleSize(worldPosition) * 0.2f,
                EventType.Repaint);
            Handles.Label(worldPosition + Vector3.up * 2f, label);

            EditorGUI.BeginChangeCheck();
            var newWorldPosition = Handles.PositionHandle(
                worldPosition,
                Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(configuration, $"Move {label}");
            localPosition = mapRoot.InverseTransformPoint(newWorldPosition);
            EditorUtility.SetDirty(configuration);
            UpdateMarkerTransform(
                configuration,
                pointType,
                index,
                teamIndex,
                localPosition);
        }

        private static void UpdateMarkerTransform(
            MapModeConfiguration configuration,
            MapModePointType pointType,
            int index,
            int teamIndex,
            Vector3 localPosition)
        {
            foreach (var marker in Resources.FindObjectsOfTypeAll<MapModePointMarker>())
            {
                if (marker.configuration != configuration ||
                    marker.pointType != pointType ||
                    marker.index != index ||
                    marker.teamIndex != teamIndex ||
                    !marker.gameObject.scene.IsValid() ||
                    !marker.gameObject.scene.isLoaded)
                {
                    continue;
                }

                marker.transform.localPosition = localPosition;
                EditorUtility.SetDirty(marker.transform);
            }
        }

        private static Transform FindMapRoot(MapDefinition mapDefinition)
        {
            foreach (var mapInstance in Resources.FindObjectsOfTypeAll<MapInstance>())
            {
                if (mapInstance.definition == mapDefinition &&
                    mapInstance.gameObject.scene.IsValid() &&
                    mapInstance.gameObject.scene.isLoaded)
                {
                    return mapInstance.transform;
                }
            }

            return null;
        }

        private static void AddBattleRoyaleMarkers(
            Transform parent,
            BattleRoyaleModeConfiguration configuration)
        {
            if (configuration.spawnPoints == null)
            {
                return;
            }

            for (var i = 0; i < configuration.spawnPoints.Count; i++)
            {
                var point = configuration.spawnPoints[i];
                if (point == null)
                {
                    continue;
                }

                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.BattleRoyaleSpawn,
                    i,
                    -1,
                    point.id,
                    point.position);
            }
        }

        private static void AddCaptureTheFlagMarkers(
            Transform parent,
            CaptureTheFlagModeConfiguration configuration)
        {
            if (configuration.teams == null)
            {
                return;
            }

            for (var i = 0; i < configuration.teams.Count; i++)
            {
                var team = configuration.teams[i];
                if (team == null)
                {
                    continue;
                }

                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.TeamSpawn,
                    i,
                    i,
                    $"{team.teamId}_Spawn",
                    team.spawnPosition);
                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.TeamBase,
                    i,
                    i,
                    $"{team.teamId}_Base",
                    team.basePosition);
                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.TeamFlag,
                    i,
                    i,
                    $"{team.teamId}_Flag",
                    team.flagPosition);
            }
        }

        private static void AddDeathmatchMarkers(
            Transform parent,
            DeathmatchModeConfiguration configuration)
        {
            if (configuration.teams == null)
            {
                return;
            }

            for (var i = 0; i < configuration.teams.Count; i++)
            {
                var team = configuration.teams[i];
                if (team == null)
                {
                    continue;
                }

                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.DeathmatchSpawn,
                    i,
                    i,
                    $"{team.teamId}_Spawn",
                    team.spawnPosition);
            }
        }

        private static void AddEliminationMarkers(
            Transform parent,
            EliminationModeConfiguration configuration)
        {
            if (configuration.teams == null)
            {
                return;
            }

            for (var i = 0; i < configuration.teams.Count; i++)
            {
                var team = configuration.teams[i];
                if (team == null)
                {
                    continue;
                }

                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.EliminationSpawn,
                    i,
                    i,
                    $"{team.teamId}_Spawn",
                    team.spawnPosition);
            }
        }

        private static void AddMarker(
            Transform parent,
            MapModeConfiguration configuration,
            MapModePointType pointType,
            int index,
            int teamIndex,
            string label,
            Vector3 localPosition)
        {
            var markerObject = new GameObject(
                $"{pointType}_{SanitizeName(label)}");
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.localPosition = localPosition;
            var marker = markerObject.AddComponent<MapModePointMarker>();
            marker.configuration = configuration;
            marker.pointType = pointType;
            marker.index = index;
            marker.teamIndex = teamIndex;
            var scatterRadius = GetScatterRadius(configuration, pointType, index);
            AddMarkerDisc(
                markerObject.transform,
                scatterRadius,
                MapModePointMarker.GetColor(pointType));
        }

        private void SaveScenePointsToConfiguration()
        {
            if (selectedConfiguration == null)
            {
                return;
            }

            Undo.RecordObject(selectedConfiguration, "Save Point Positions To Map Data");
            EnsureLists(selectedConfiguration);
            var battleRoyale = selectedConfiguration as BattleRoyaleModeConfiguration;
            var captureTheFlag = selectedConfiguration as CaptureTheFlagModeConfiguration;
            var deathmatch = selectedConfiguration as DeathmatchModeConfiguration;
            var elimination = selectedConfiguration as EliminationModeConfiguration;
            foreach (var marker in Resources.FindObjectsOfTypeAll<MapModePointMarker>())
            {
                if (marker.configuration != selectedConfiguration ||
                    !marker.gameObject.scene.IsValid() ||
                    !marker.gameObject.scene.isLoaded)
                {
                    continue;
                }

                var position = marker.transform.localPosition;
                if (marker.pointType == MapModePointType.BattleRoyaleSpawn &&
                    battleRoyale != null &&
                    battleRoyale.spawnPoints != null &&
                    marker.index >= 0 &&
                    marker.index < battleRoyale.spawnPoints.Count &&
                    battleRoyale.spawnPoints[marker.index] != null)
                {
                    battleRoyale.spawnPoints[marker.index].position =
                        position;
                    continue;
                }

                if (marker.pointType == MapModePointType.DeathmatchSpawn &&
                    deathmatch != null &&
                    deathmatch.teams != null &&
                    marker.index >= 0 &&
                    marker.index < deathmatch.teams.Count &&
                    deathmatch.teams[marker.index] != null)
                {
                    deathmatch.teams[marker.index].spawnPosition =
                        position;
                    continue;
                }

                if (marker.pointType == MapModePointType.EliminationSpawn &&
                    elimination != null &&
                    elimination.teams != null &&
                    marker.index >= 0 &&
                    marker.index < elimination.teams.Count &&
                    elimination.teams[marker.index] != null)
                {
                    elimination.teams[marker.index].spawnPosition =
                        position;
                    continue;
                }

                if (captureTheFlag == null ||
                    captureTheFlag.teams == null ||
                    marker.teamIndex < 0 ||
                    marker.teamIndex >= captureTheFlag.teams.Count)
                {
                    continue;
                }

                var team = captureTheFlag.teams[marker.teamIndex];
                if (team == null)
                {
                    continue;
                }

                if (marker.pointType == MapModePointType.TeamSpawn)
                {
                    team.spawnPosition = position;
                }
                else if (marker.pointType == MapModePointType.TeamBase)
                {
                    team.basePosition = position;
                }
                else if (marker.pointType == MapModePointType.TeamFlag)
                {
                    team.flagPosition = position;
                }
            }

            EditorUtility.SetDirty(selectedConfiguration);
            AssetDatabase.SaveAssets();
        }

        private static bool SpawnCountNeedsUpdate(BattleRoyaleModeConfiguration configuration)
        {
            if (configuration == null)
            {
                return false;
            }

            var count = configuration.spawnPoints == null
                ? 0
                : configuration.spawnPoints.Count;
            return count != Mathf.Max(1, configuration.teamCount);
        }

        private static bool EnsureBattleRoyaleSpawnPoints(
            BattleRoyaleModeConfiguration configuration)
        {
            if (configuration == null)
            {
                return false;
            }

            configuration.spawnPoints ??= new List<MapModePoint>();
            var requiredCount = Mathf.Max(1, configuration.teamCount);
            var changed = false;
            var center = GetMapCenter(configuration.map);
            while (configuration.spawnPoints.Count < requiredCount)
            {
                var index = configuration.spawnPoints.Count;
                var angle = (index + 0.5f) / requiredCount * Mathf.PI * 2f;
                configuration.spawnPoints.Add(
                    new MapModePoint
                    {
                        id = $"Spawn {index + 1}",
                        position = center + new Vector3(
                            Mathf.Cos(angle) * NewSpawnRingRadius,
                            0f,
                            Mathf.Sin(angle) * NewSpawnRingRadius)
                    });
                changed = true;
            }

            while (configuration.spawnPoints.Count > requiredCount)
            {
                configuration.spawnPoints.RemoveAt(
                    configuration.spawnPoints.Count - 1);
                changed = true;
            }

            return changed;
        }

        private static void SyncBattleRoyaleMarkers(
            BattleRoyaleModeConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }

            var parent = FindConfigurationMarkerParent(configuration, true);
            if (parent == null)
            {
                return;
            }

            var points = configuration.spawnPoints;
            var existing = new Dictionary<int, MapModePointMarker>();
            for (var childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
            {
                var child = parent.GetChild(childIndex);
                var marker = child.GetComponent<MapModePointMarker>();
                if (marker == null ||
                    marker.pointType != MapModePointType.BattleRoyaleSpawn)
                {
                    continue;
                }

                if (points == null ||
                    marker.index < 0 ||
                    marker.index >= points.Count ||
                    existing.ContainsKey(marker.index))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    continue;
                }

                existing.Add(marker.index, marker);
            }

            if (points == null)
            {
                return;
            }

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == null || existing.ContainsKey(i))
                {
                    continue;
                }

                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.BattleRoyaleSpawn,
                    i,
                    -1,
                    point.id,
                    point.position);
            }
        }

        private static void SyncDeathmatchMarkers(
            DeathmatchModeConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }

            var parent = FindConfigurationMarkerParent(configuration, true);
            if (parent == null)
            {
                return;
            }

            var teams = configuration.teams;
            var existing = new Dictionary<int, MapModePointMarker>();
            for (var childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
            {
                var child = parent.GetChild(childIndex);
                var marker = child.GetComponent<MapModePointMarker>();
                if (marker == null ||
                    marker.pointType != MapModePointType.DeathmatchSpawn)
                {
                    continue;
                }

                if (teams == null ||
                    marker.index < 0 ||
                    marker.index >= teams.Count ||
                    existing.ContainsKey(marker.index))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    continue;
                }

                existing.Add(marker.index, marker);
            }

            if (teams == null)
            {
                return;
            }

            for (var i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                if (team == null || existing.ContainsKey(i))
                {
                    continue;
                }

                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.DeathmatchSpawn,
                    i,
                    i,
                    $"{team.teamId}_Spawn",
                    team.spawnPosition);
            }
        }

        private static void SyncEliminationMarkers(
            EliminationModeConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }

            var parent = FindConfigurationMarkerParent(configuration, true);
            if (parent == null)
            {
                return;
            }

            var teams = configuration.teams;
            var existing = new Dictionary<int, MapModePointMarker>();
            for (var childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
            {
                var child = parent.GetChild(childIndex);
                var marker = child.GetComponent<MapModePointMarker>();
                if (marker == null ||
                    marker.pointType != MapModePointType.EliminationSpawn)
                {
                    continue;
                }

                if (teams == null ||
                    marker.index < 0 ||
                    marker.index >= teams.Count ||
                    existing.ContainsKey(marker.index))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    continue;
                }

                existing.Add(marker.index, marker);
            }

            if (teams == null)
            {
                return;
            }

            for (var i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                if (team == null || existing.ContainsKey(i))
                {
                    continue;
                }

                AddMarker(
                    parent,
                    configuration,
                    MapModePointType.EliminationSpawn,
                    i,
                    i,
                    $"{team.teamId}_Spawn",
                    team.spawnPosition);
            }
        }

        private static Transform FindConfigurationMarkerParent(
            MapModeConfiguration configuration,
            bool createIfMissing)
        {
            var mapRoot = FindMapRoot(configuration.map);
            if (mapRoot == null)
            {
                return null;
            }

            var markerRoot = mapRoot.Find(MarkerRootName);
            if (markerRoot == null)
            {
                if (!createIfMissing)
                {
                    return null;
                }

                var markerRootObject = new GameObject(MarkerRootName);
                Undo.RegisterCreatedObjectUndo(markerRootObject, "Create Mode Markers");
                markerRootObject.transform.SetParent(mapRoot, false);
                markerRoot = markerRootObject.transform;
            }

            var configurationName =
                $"{configuration.mode}_{SanitizeName(configuration.variantName)}";
            var configurationRoot = markerRoot.Find(configurationName);
            if (configurationRoot == null && createIfMissing)
            {
                var configurationObject = new GameObject(configurationName);
                Undo.RegisterCreatedObjectUndo(
                    configurationObject,
                    "Create Mode Markers");
                configurationObject.transform.SetParent(markerRoot, false);
                configurationRoot = configurationObject.transform;
            }

            return configurationRoot;
        }

        private static void EnsureMarkerDiscs(MapModeConfiguration configuration)
        {
            foreach (var marker in Resources.FindObjectsOfTypeAll<MapModePointMarker>())
            {
                if (marker.configuration != configuration ||
                    marker.transform.Find(MarkerDiscName) != null ||
                    !marker.gameObject.scene.IsValid() ||
                    !marker.gameObject.scene.isLoaded)
                {
                    continue;
                }

                var radius = GetScatterRadius(
                    configuration,
                    marker.pointType,
                    marker.index);

                AddMarkerDisc(
                    marker.transform,
                    radius,
                    MapModePointMarker.GetColor(marker.pointType));
                Undo.RegisterCreatedObjectUndo(
                    marker.transform.Find(MarkerDiscName).gameObject,
                    "Show Spawn Marker");
            }
        }

        private static float GetScatterRadius(
            MapModeConfiguration configuration,
            MapModePointType pointType,
            int index)
        {
            if (index < 0)
            {
                return 0f;
            }

            if (pointType == MapModePointType.BattleRoyaleSpawn &&
                configuration is BattleRoyaleModeConfiguration battleRoyale &&
                battleRoyale.spawnPoints != null &&
                index < battleRoyale.spawnPoints.Count &&
                battleRoyale.spawnPoints[index] != null)
            {
                return battleRoyale.spawnPoints[index].radius;
            }

            if (pointType == MapModePointType.DeathmatchSpawn &&
                configuration is DeathmatchModeConfiguration deathmatch &&
                deathmatch.teams != null &&
                index < deathmatch.teams.Count &&
                deathmatch.teams[index] != null)
            {
                return deathmatch.teams[index].radius;
            }

            if (pointType == MapModePointType.EliminationSpawn &&
                configuration is EliminationModeConfiguration elimination &&
                elimination.teams != null &&
                index < elimination.teams.Count &&
                elimination.teams[index] != null)
            {
                return elimination.teams[index].radius;
            }

            return 0f;
        }

        private static void UpdateMarkerDiscDiameter(
            MapModeConfiguration configuration,
            MapModePointType pointType,
            int index,
            float scatterRadius)
        {
            var diameter = Mathf.Max(MinimumMarkerDiameter, scatterRadius * 2f);
            foreach (var marker in Resources.FindObjectsOfTypeAll<MapModePointMarker>())
            {
                if (marker.configuration != configuration ||
                    marker.pointType != pointType ||
                    marker.index != index ||
                    !marker.gameObject.scene.IsValid() ||
                    !marker.gameObject.scene.isLoaded)
                {
                    continue;
                }

                var disc = marker.transform.Find(MarkerDiscName);
                if (disc == null)
                {
                    continue;
                }

                disc.localScale = new Vector3(diameter, disc.localScale.y, diameter);
            }
        }

        private static Mesh discMesh;

        private static void AddMarkerDisc(
            Transform marker,
            float scatterRadius,
            Color color)
        {
            if (marker.Find(MarkerDiscName) != null)
            {
                return;
            }

            var disc = new GameObject(MarkerDiscName);
            disc.transform.SetParent(marker, false);
            disc.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var diameter = Mathf.Max(MinimumMarkerDiameter, scatterRadius * 2f);
            disc.transform.localScale = new Vector3(diameter, 0.04f, diameter);
            var meshFilter = disc.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetDiscMesh();
            var renderer = disc.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetMarkerMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private static Mesh GetDiscMesh()
        {
            if (discMesh != null)
            {
                return discMesh;
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            discMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(temp);
            return discMesh;
        }

        private static Material GetMarkerMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MarkerMaterialPath);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            return shader != null ? new Material(shader) : null;
        }

        private static void EnsureLists(MapModeConfiguration configuration)
        {
            switch (configuration)
            {
                case BattleRoyaleModeConfiguration battleRoyale:
                    battleRoyale.spawnPoints ??= new List<MapModePoint>();
                    break;
                case CaptureTheFlagModeConfiguration captureTheFlag:
                    captureTheFlag.teams ??= new List<MapModeTeamPoints>();
                    break;
                case DeathmatchModeConfiguration deathmatch:
                    deathmatch.teams ??= new List<MapTeamSpawn>();
                    break;
                case EliminationModeConfiguration elimination:
                    elimination.teams ??= new List<MapTeamSpawn>();
                    break;
            }
        }

        private static void EnsureCaptureTheFlagTeams(
            CaptureTheFlagModeConfiguration configuration)
        {
            configuration.teams ??= new List<MapModeTeamPoints>();
            var requiredCount = Mathf.Max(2, configuration.teamCount);
            while (configuration.teams.Count < requiredCount)
            {
                var index = configuration.teams.Count;
                configuration.teams.Add(
                    new MapModeTeamPoints
                    {
                        teamId = $"Team {index + 1}"
                    });
            }

            while (configuration.teams.Count > requiredCount)
            {
                configuration.teams.RemoveAt(
                    configuration.teams.Count - 1);
            }
        }

        private static int MinimumTeamCount(MapGameMode mode)
        {
            return mode == MapGameMode.BattleRoyale ? 1 : 2;
        }

        private static List<MapGameMode> GetSupportedModes(MapDefinition mapDefinition)
        {
            var modes = new List<MapGameMode>();
            foreach (MapGameMode mode in Enum.GetValues(typeof(MapGameMode)))
            {
                if (mapDefinition.SupportsMode(mode))
                {
                    modes.Add(mode);
                }
            }

            return modes;
        }

        private static MapModeConfiguration FindConfiguration(
            MapDefinition mapDefinition,
            MapGameMode mode)
        {
            if (mapDefinition.modeConfigurations == null)
            {
                return null;
            }

            foreach (var configuration in mapDefinition.modeConfigurations)
            {
                if (configuration != null &&
                    configuration.map == mapDefinition &&
                    configuration.mode == mode)
                {
                    return configuration;
                }
            }

            return null;
        }

        private static Vector3 GetMapCenter(MapDefinition mapDefinition)
        {
            return mapDefinition != null
                ? mapDefinition.WorldCenter
                : Vector3.zero;
        }

        private static string SanitizeName(string value)
        {
            var result = string.IsNullOrWhiteSpace(value)
                ? "Default"
                : value.Trim();
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalidCharacter, '_');
            }

            return result.Replace(' ', '_');
        }

        public static string GetDefinitionsFolder(TextAsset layoutJson)
        {
            var jsonPath = layoutJson != null
                ? AssetDatabase.GetAssetPath(layoutJson)
                : null;
            if (string.IsNullOrEmpty(jsonPath))
            {
                return null;
            }

            var mapFolder = Path.GetDirectoryName(jsonPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(mapFolder))
            {
                return null;
            }

            return $"{mapFolder}/Definitions";
        }

        public static string EnsureDefinitionsFolder(TextAsset layoutJson)
        {
            var definitionsFolder = GetDefinitionsFolder(layoutJson);
            if (string.IsNullOrEmpty(definitionsFolder) ||
                AssetDatabase.IsValidFolder(definitionsFolder))
            {
                return definitionsFolder;
            }

            var parent = Path.GetDirectoryName(definitionsFolder)?.Replace('\\', '/');
            var leaf = Path.GetFileName(definitionsFolder);
            if (!string.IsNullOrEmpty(parent) &&
                !string.IsNullOrEmpty(leaf) &&
                AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder(parent, leaf);
            }

            return definitionsFolder;
        }

        public static TextAsset FindSiblingMarkdown(TextAsset jsonAsset)
        {
            if (jsonAsset == null)
            {
                return null;
            }

            var jsonPath = AssetDatabase.GetAssetPath(jsonAsset);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(
                Path.ChangeExtension(jsonPath, ".md"));
        }

        public static Sprite SpriteForVisualMap(Texture2D visualMap)
        {
            if (visualMap == null)
            {
                return null;
            }

            var path = AssetDatabase.GetAssetPath(visualMap);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // visualMap is the whole PNG. Mode select needs a Sprite, and a
            // Multiple import stores one Sprite per separated mark. The first
            // of those is often a glyph. One sprite for the whole texture is
            // the picture already referenced by visualMap.
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null &&
                (importer.textureType != TextureImporterType.Sprite ||
                 importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
