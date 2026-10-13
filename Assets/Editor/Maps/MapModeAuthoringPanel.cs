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

            EditorGUILayout.LabelField("Game Modes", EditorStyles.boldLabel);
            DrawSupportedModes(mapDefinition);

            var supportedModes = GetSupportedModes(mapDefinition);
            if (supportedModes.Count == 0)
            {
                StopSceneGui();
                EditorGUILayout.HelpBox(
                    "Select at least one game mode that this map supports.",
                    MessageType.None);
                EditorGUILayout.Space(8f);
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
                EditorGUILayout.Space(8f);
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
                EditorGUILayout.Space(8f);
                return;
            }

            DrawConfigurationSettings(mapDefinition, definitionsFolder);
            DrawConfigurationPoints(mapDefinition);
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
            EditorGUILayout.Space(8f);
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

                if (configuration.IsBattleRoyale &&
                    (configuration.battleRoyaleSpawnPoints == null ||
                     configuration.battleRoyaleSpawnPoints.Count == 0))
                {
                    errorMessage =
                        $"Battle Royale configuration '{configuration.name}' needs at least one starting spawn point.";
                    return false;
                }

                if (configuration.IsCaptureTheFlag &&
                    (configuration.captureTheFlagTeams == null ||
                     configuration.captureTheFlagTeams.Count <
                     Mathf.Max(2, configuration.teamCount)))
                {
                    errorMessage =
                        $"Capture the Flag configuration '{configuration.name}' needs spawn, base, and flag points for every team.";
                    return false;
                }

                if (configuration.IsDeathmatch &&
                    (configuration.deathmatchTeams == null ||
                     configuration.deathmatchTeams.Count <
                     DeathmatchRules.RequiredTeamCount(configuration.teamCount)))
                {
                    errorMessage =
                        $"Deathmatch configuration '{configuration.name}' needs a spawn point for every team.";
                    return false;
                }

                if (configuration.IsElimination &&
                    (configuration.eliminationTeams == null ||
                     configuration.eliminationTeams.Count <
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

                if (configuration.IsBattleRoyale)
                {
                    AddBattleRoyaleMarkers(configurationRoot.transform, configuration);
                }
                else if (configuration.IsCaptureTheFlag)
                {
                    AddCaptureTheFlagMarkers(configurationRoot.transform, configuration);
                }
                else if (configuration.IsDeathmatch)
                {
                    AddDeathmatchMarkers(configurationRoot.transform, configuration);
                }
                else if (configuration.IsElimination)
                {
                    AddEliminationMarkers(configurationRoot.transform, configuration);
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
            EditorGUILayout.LabelField(
                $"{ObjectNames.NicifyVariableName(selectedMode.ToString())} Settings",
                EditorStyles.boldLabel);

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
                return;
            }

            if (selectedConfiguration.IsBattleRoyale)
            {
                DrawBattleRoyalePoints(mapDefinition);
            }
            else if (selectedConfiguration.IsCaptureTheFlag)
            {
                DrawCaptureTheFlagPoints(mapDefinition);
            }
            else if (selectedConfiguration.IsDeathmatch)
            {
                DrawDeathmatchPoints(mapDefinition);
            }
            else if (selectedConfiguration.IsElimination)
            {
                DrawEliminationPoints(mapDefinition);
            }
        }

        private void DrawBattleRoyalePoints(MapDefinition mapDefinition)
        {
            selectedConfiguration.battleRoyaleSpawnPoints ??= new List<MapModePoint>();
            EditorGUILayout.LabelField("Starting Spawn Points", EditorStyles.boldLabel);

            for (var i = 0; i < selectedConfiguration.battleRoyaleSpawnPoints.Count; i++)
            {
                var point = selectedConfiguration.battleRoyaleSpawnPoints[i];
                if (point == null)
                {
                    point = new MapModePoint();
                    selectedConfiguration.battleRoyaleSpawnPoints[i] = point;
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
                        Undo.RecordObject(selectedConfiguration, "Remove Battle Royale Spawn Point");
                        selectedConfiguration.teamCount = teamCount;
                        EnsureBattleRoyaleSpawnPoints(selectedConfiguration);
                        EditorUtility.SetDirty(selectedConfiguration);
                        SyncBattleRoyaleMarkers(selectedConfiguration);
                        i--;
                        continue;
                    }

                    if (pointIdChanged)
                    {
                        Undo.RecordObject(
                            selectedConfiguration,
                            "Rename Battle Royale Spawn Point");
                        point.id = pointId;
                        EditorUtility.SetDirty(selectedConfiguration);
                    }
                }

                DrawPointPosition(selectedConfiguration, point, "Position");
                EditorGUI.BeginChangeCheck();
                var radius = Mathf.Max(
                    0f,
                    EditorGUILayout.FloatField("Scatter Radius", point.radius));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(
                        selectedConfiguration,
                        "Edit Battle Royale Spawn Radius");
                    point.radius = radius;
                    EditorUtility.SetDirty(selectedConfiguration);
                    UpdateMarkerDiscDiameter(
                        selectedConfiguration,
                        MapModePointType.BattleRoyaleSpawn,
                        i,
                        radius);
                }
            }

            if (GUILayout.Button("Add Battle Royale Spawn Point"))
            {
                teamCount++;
                Undo.RecordObject(selectedConfiguration, "Add Battle Royale Spawn Point");
                selectedConfiguration.teamCount = teamCount;
                EnsureBattleRoyaleSpawnPoints(selectedConfiguration);
                EditorUtility.SetDirty(selectedConfiguration);
                SyncBattleRoyaleMarkers(selectedConfiguration);
            }
        }

        private void DrawCaptureTheFlagPoints(MapDefinition mapDefinition)
        {
            selectedConfiguration.captureTheFlagTeams ??= new List<MapModeTeamPoints>();
            EnsureCaptureTheFlagTeams(selectedConfiguration);
            EditorGUILayout.LabelField("Team Points", EditorStyles.boldLabel);

            for (var i = 0; i < selectedConfiguration.captureTheFlagTeams.Count; i++)
            {
                var team = selectedConfiguration.captureTheFlagTeams[i];
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
                        selectedConfiguration,
                        "Edit Capture The Flag Points");
                    team.teamId = teamId;
                    team.spawnPosition = spawnPosition;
                    team.basePosition = basePosition;
                    team.flagPosition = flagPosition;
                    EditorUtility.SetDirty(selectedConfiguration);
                    UpdateMarkerTransform(
                        selectedConfiguration,
                        MapModePointType.TeamSpawn,
                        i,
                        i,
                        spawnPosition);
                    UpdateMarkerTransform(
                        selectedConfiguration,
                        MapModePointType.TeamBase,
                        i,
                        i,
                        basePosition);
                    UpdateMarkerTransform(
                        selectedConfiguration,
                        MapModePointType.TeamFlag,
                        i,
                        i,
                        flagPosition);
                }
                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("Reset Team Points To Map Center"))
            {
                Undo.RecordObject(selectedConfiguration, "Reset Capture The Flag Points");
                for (var i = 0; i < selectedConfiguration.captureTheFlagTeams.Count; i++)
                {
                    var center = GetMapCenter(mapDefinition);
                    selectedConfiguration.captureTheFlagTeams[i].spawnPosition = center;
                    selectedConfiguration.captureTheFlagTeams[i].basePosition = center;
                    selectedConfiguration.captureTheFlagTeams[i].flagPosition = center;
                }

                EditorUtility.SetDirty(selectedConfiguration);
            }
        }

        private void DrawDeathmatchPoints(MapDefinition mapDefinition)
        {
            selectedConfiguration.deathmatchTeams ??= new List<MapModeTeamSpawn>();
            DeathmatchRules.EnsureTeamSpawns(selectedConfiguration);
            EditorGUILayout.LabelField("Team Spawns", EditorStyles.boldLabel);

            for (var i = 0; i < selectedConfiguration.deathmatchTeams.Count; i++)
            {
                var team = selectedConfiguration.deathmatchTeams[i];
                if (team == null)
                {
                    team = new MapModeTeamSpawn();
                    selectedConfiguration.deathmatchTeams[i] = team;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(team.teamId, EditorStyles.miniBoldLabel);
                    if (selectedConfiguration.deathmatchTeams.Count >
                            DeathmatchRules.MinimumTeamCount &&
                        GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        teamCount = Mathf.Max(
                            DeathmatchRules.MinimumTeamCount,
                            teamCount - 1);
                        Undo.RecordObject(
                            selectedConfiguration,
                            "Remove Deathmatch Spawn");
                        selectedConfiguration.teamCount = teamCount;
                        DeathmatchRules.EnsureTeamSpawns(selectedConfiguration);
                        EditorUtility.SetDirty(selectedConfiguration);
                        SyncDeathmatchMarkers(selectedConfiguration);
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
                        selectedConfiguration,
                        "Edit Deathmatch Spawn");
                    team.teamId = teamId;
                    team.spawnPosition = spawnPosition;
                    team.radius = radius;
                    EditorUtility.SetDirty(selectedConfiguration);
                    UpdateMarkerTransform(
                        selectedConfiguration,
                        MapModePointType.DeathmatchSpawn,
                        i,
                        i,
                        spawnPosition);
                    UpdateMarkerDiscDiameter(
                        selectedConfiguration,
                        MapModePointType.DeathmatchSpawn,
                        i,
                        radius);
                }

                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("Add Team Spawn"))
            {
                teamCount++;
                Undo.RecordObject(selectedConfiguration, "Add Deathmatch Spawn");
                selectedConfiguration.teamCount = teamCount;
                DeathmatchRules.EnsureTeamSpawns(selectedConfiguration);
                EditorUtility.SetDirty(selectedConfiguration);
                SyncDeathmatchMarkers(selectedConfiguration);
            }

            if (GUILayout.Button("Reset Team Spawns Around Map Center"))
            {
                Undo.RecordObject(
                    selectedConfiguration,
                    "Reset Deathmatch Spawns");
                var center = GetMapCenter(mapDefinition);
                var count = selectedConfiguration.deathmatchTeams.Count;
                for (var i = 0; i < count; i++)
                {
                    var team = selectedConfiguration.deathmatchTeams[i];
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
                        selectedConfiguration,
                        MapModePointType.DeathmatchSpawn,
                        i,
                        i,
                        team.spawnPosition);
                }

                EditorUtility.SetDirty(selectedConfiguration);
            }
        }

        private void DrawEliminationPoints(MapDefinition mapDefinition)
        {
            selectedConfiguration.eliminationTeams ??= new List<MapModeTeamSpawn>();
            EliminationRules.EnsureTeamSpawns(selectedConfiguration);
            EditorGUILayout.LabelField("Team Spawns", EditorStyles.boldLabel);

            for (var i = 0; i < selectedConfiguration.eliminationTeams.Count; i++)
            {
                var team = selectedConfiguration.eliminationTeams[i];
                if (team == null)
                {
                    team = new MapModeTeamSpawn();
                    selectedConfiguration.eliminationTeams[i] = team;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(team.teamId, EditorStyles.miniBoldLabel);
                    if (selectedConfiguration.eliminationTeams.Count >
                            EliminationRules.MinimumTeamCount &&
                        GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        teamCount = Mathf.Max(
                            EliminationRules.MinimumTeamCount,
                            teamCount - 1);
                        Undo.RecordObject(
                            selectedConfiguration,
                            "Remove Elimination Spawn");
                        selectedConfiguration.teamCount = teamCount;
                        EliminationRules.EnsureTeamSpawns(selectedConfiguration);
                        EditorUtility.SetDirty(selectedConfiguration);
                        SyncEliminationMarkers(selectedConfiguration);
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
                        selectedConfiguration,
                        "Edit Elimination Spawn");
                    team.teamId = teamId;
                    team.spawnPosition = spawnPosition;
                    team.radius = radius;
                    EditorUtility.SetDirty(selectedConfiguration);
                    UpdateMarkerTransform(
                        selectedConfiguration,
                        MapModePointType.EliminationSpawn,
                        i,
                        i,
                        spawnPosition);
                    UpdateMarkerDiscDiameter(
                        selectedConfiguration,
                        MapModePointType.EliminationSpawn,
                        i,
                        radius);
                }

                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("Add Team Spawn"))
            {
                teamCount++;
                Undo.RecordObject(selectedConfiguration, "Add Elimination Spawn");
                selectedConfiguration.teamCount = teamCount;
                EliminationRules.EnsureTeamSpawns(selectedConfiguration);
                EditorUtility.SetDirty(selectedConfiguration);
                SyncEliminationMarkers(selectedConfiguration);
            }

            if (GUILayout.Button("Reset Team Spawns Around Map Center"))
            {
                Undo.RecordObject(
                    selectedConfiguration,
                    "Reset Elimination Spawns");
                var center = GetMapCenter(mapDefinition);
                var count = selectedConfiguration.eliminationTeams.Count;
                for (var i = 0; i < count; i++)
                {
                    var team = selectedConfiguration.eliminationTeams[i];
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
                        selectedConfiguration,
                        MapModePointType.EliminationSpawn,
                        i,
                        i,
                        team.spawnPosition);
                }

                EditorUtility.SetDirty(selectedConfiguration);
            }
        }

        private void DrawPointPosition(
            MapModeConfiguration configuration,
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
                configuration.battleRoyaleSpawnPoints.IndexOf(point),
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
                    selectedConfiguration =
                        ScriptableObject.CreateInstance<MapModeConfiguration>();
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

        private void ApplySettingsToConfiguration()
        {
            if (selectedConfiguration == null)
            {
                return;
            }

            EnsureLists(selectedConfiguration);
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
            if (selectedConfiguration.IsDeathmatch)
            {
                settingsMatch = settingsMatch &&
                    selectedConfiguration.scoreLimit == nextScoreLimit &&
                    Mathf.Approximately(
                        selectedConfiguration.respawnSeconds,
                        nextRespawnSeconds);
            }
            else if (selectedConfiguration.IsElimination)
            {
                settingsMatch = settingsMatch &&
                    selectedConfiguration.roundWinLimit == nextRoundWinLimit &&
                    Mathf.Approximately(
                        selectedConfiguration.roundSeconds,
                        nextRoundSeconds) &&
                    Mathf.Approximately(
                        selectedConfiguration.intermissionSeconds,
                        nextIntermissionSeconds);
            }

            if (settingsMatch)
            {
                if (selectedConfiguration.IsCaptureTheFlag)
                {
                    EnsureCaptureTheFlagTeams(selectedConfiguration);
                }
                else if (selectedConfiguration.IsDeathmatch)
                {
                    spawnCountChanged = DeathmatchRules.EnsureTeamSpawns(
                        selectedConfiguration);
                }
                else if (selectedConfiguration.IsElimination)
                {
                    spawnCountChanged = EliminationRules.EnsureTeamSpawns(
                        selectedConfiguration);
                }
                else if (selectedConfiguration.IsBattleRoyale &&
                         SpawnCountNeedsUpdate(selectedConfiguration))
                {
                    Undo.RecordObject(
                        selectedConfiguration,
                        "Update Battle Royale Spawn Points");
                    spawnCountChanged = EnsureBattleRoyaleSpawnPoints(
                        selectedConfiguration);
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
            if (selectedConfiguration.IsDeathmatch)
            {
                selectedConfiguration.scoreLimit = nextScoreLimit;
                selectedConfiguration.respawnSeconds = nextRespawnSeconds;
                spawnCountChanged = DeathmatchRules.EnsureTeamSpawns(
                    selectedConfiguration);
            }
            else if (selectedConfiguration.IsElimination)
            {
                selectedConfiguration.roundWinLimit = nextRoundWinLimit;
                selectedConfiguration.roundSeconds = nextRoundSeconds;
                selectedConfiguration.intermissionSeconds = nextIntermissionSeconds;
                spawnCountChanged = EliminationRules.EnsureTeamSpawns(
                    selectedConfiguration);
            }
            else if (selectedConfiguration.IsCaptureTheFlag)
            {
                EnsureCaptureTheFlagTeams(selectedConfiguration);
            }
            else if (selectedConfiguration.IsBattleRoyale)
            {
                spawnCountChanged = EnsureBattleRoyaleSpawnPoints(
                    selectedConfiguration);
            }

            EditorUtility.SetDirty(selectedConfiguration);
            if (spawnCountChanged)
            {
                SyncModeMarkers(selectedConfiguration);
            }
        }

        private static void SyncModeMarkers(MapModeConfiguration configuration)
        {
            if (configuration.IsBattleRoyale)
            {
                SyncBattleRoyaleMarkers(configuration);
            }
            else if (configuration.IsDeathmatch)
            {
                SyncDeathmatchMarkers(configuration);
            }
            else if (configuration.IsElimination)
            {
                SyncEliminationMarkers(configuration);
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
            scoreLimit = Mathf.Max(1, selectedConfiguration.scoreLimit);
            respawnSeconds = Mathf.Max(0f, selectedConfiguration.respawnSeconds);
            roundWinLimit = Mathf.Max(1, selectedConfiguration.roundWinLimit);
            roundSeconds = Mathf.Max(1f, selectedConfiguration.roundSeconds);
            intermissionSeconds = Mathf.Max(
                0f,
                selectedConfiguration.intermissionSeconds);
            EnsureLists(selectedConfiguration);
            if (selectedConfiguration.IsCaptureTheFlag)
            {
                EnsureCaptureTheFlagTeams(selectedConfiguration);
            }
            else if (selectedConfiguration.IsDeathmatch)
            {
                DeathmatchRules.EnsureTeamSpawns(selectedConfiguration);
            }
            else if (selectedConfiguration.IsElimination)
            {
                EliminationRules.EnsureTeamSpawns(selectedConfiguration);
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

            if (selectedConfiguration.IsBattleRoyale)
            {
                DrawBattleRoyaleHandles(mapRoot);
            }
            else if (selectedConfiguration.IsCaptureTheFlag)
            {
                DrawCaptureTheFlagHandles(mapRoot);
            }
            else if (selectedConfiguration.IsDeathmatch)
            {
                DrawDeathmatchHandles(mapRoot);
            }
            else if (selectedConfiguration.IsElimination)
            {
                DrawEliminationHandles(mapRoot);
            }
        }

        private void DrawBattleRoyaleHandles(Transform mapRoot)
        {
            if (selectedConfiguration.battleRoyaleSpawnPoints == null)
            {
                return;
            }

            for (var i = 0; i < selectedConfiguration.battleRoyaleSpawnPoints.Count; i++)
            {
                var point = selectedConfiguration.battleRoyaleSpawnPoints[i];
                if (point == null)
                {
                    continue;
                }

                DrawPositionHandle(
                    mapRoot,
                    selectedConfiguration,
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
            if (selectedConfiguration.captureTheFlagTeams == null)
            {
                return;
            }

            for (var i = 0; i < selectedConfiguration.captureTheFlagTeams.Count; i++)
            {
                var team = selectedConfiguration.captureTheFlagTeams[i];
                if (team == null)
                {
                    continue;
                }

                DrawPositionHandle(
                    mapRoot,
                    selectedConfiguration,
                    MapModePointType.TeamSpawn,
                    i,
                    i,
                    $"{team.teamId} Spawn",
                    Color.green,
                    ref team.spawnPosition);
                DrawPositionHandle(
                    mapRoot,
                    selectedConfiguration,
                    MapModePointType.TeamBase,
                    i,
                    i,
                    $"{team.teamId} Base",
                    Color.yellow,
                    ref team.basePosition);
                DrawPositionHandle(
                    mapRoot,
                    selectedConfiguration,
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
            if (selectedConfiguration.deathmatchTeams == null)
            {
                return;
            }

            for (var i = 0; i < selectedConfiguration.deathmatchTeams.Count; i++)
            {
                var team = selectedConfiguration.deathmatchTeams[i];
                if (team == null)
                {
                    continue;
                }

                DrawPositionHandle(
                    mapRoot,
                    selectedConfiguration,
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
            if (selectedConfiguration.eliminationTeams == null)
            {
                return;
            }

            for (var i = 0; i < selectedConfiguration.eliminationTeams.Count; i++)
            {
                var team = selectedConfiguration.eliminationTeams[i];
                if (team == null)
                {
                    continue;
                }

                DrawPositionHandle(
                    mapRoot,
                    selectedConfiguration,
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
            MapModeConfiguration configuration)
        {
            if (configuration.battleRoyaleSpawnPoints == null)
            {
                return;
            }

            for (var i = 0; i < configuration.battleRoyaleSpawnPoints.Count; i++)
            {
                var point = configuration.battleRoyaleSpawnPoints[i];
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
            MapModeConfiguration configuration)
        {
            if (configuration.captureTheFlagTeams == null)
            {
                return;
            }

            for (var i = 0; i < configuration.captureTheFlagTeams.Count; i++)
            {
                var team = configuration.captureTheFlagTeams[i];
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
            MapModeConfiguration configuration)
        {
            if (configuration.deathmatchTeams == null)
            {
                return;
            }

            for (var i = 0; i < configuration.deathmatchTeams.Count; i++)
            {
                var team = configuration.deathmatchTeams[i];
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
            MapModeConfiguration configuration)
        {
            if (configuration.eliminationTeams == null)
            {
                return;
            }

            for (var i = 0; i < configuration.eliminationTeams.Count; i++)
            {
                var team = configuration.eliminationTeams[i];
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
            var scatterRadius = 0f;
            if (pointType == MapModePointType.BattleRoyaleSpawn &&
                configuration.battleRoyaleSpawnPoints != null &&
                index >= 0 &&
                index < configuration.battleRoyaleSpawnPoints.Count &&
                configuration.battleRoyaleSpawnPoints[index] != null)
            {
                scatterRadius = configuration.battleRoyaleSpawnPoints[index].radius;
            }
            else if (pointType == MapModePointType.DeathmatchSpawn &&
                     configuration.deathmatchTeams != null &&
                     index >= 0 &&
                     index < configuration.deathmatchTeams.Count &&
                     configuration.deathmatchTeams[index] != null)
            {
                scatterRadius = configuration.deathmatchTeams[index].radius;
            }
            else if (pointType == MapModePointType.EliminationSpawn &&
                     configuration.eliminationTeams != null &&
                     index >= 0 &&
                     index < configuration.eliminationTeams.Count &&
                     configuration.eliminationTeams[index] != null)
            {
                scatterRadius = configuration.eliminationTeams[index].radius;
            }
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
                    selectedConfiguration.battleRoyaleSpawnPoints != null &&
                    marker.index >= 0 &&
                    marker.index < selectedConfiguration.battleRoyaleSpawnPoints.Count &&
                    selectedConfiguration.battleRoyaleSpawnPoints[marker.index] != null)
                {
                    selectedConfiguration.battleRoyaleSpawnPoints[marker.index].position =
                        position;
                    continue;
                }

                if (marker.pointType == MapModePointType.DeathmatchSpawn &&
                    selectedConfiguration.deathmatchTeams != null &&
                    marker.index >= 0 &&
                    marker.index < selectedConfiguration.deathmatchTeams.Count &&
                    selectedConfiguration.deathmatchTeams[marker.index] != null)
                {
                    selectedConfiguration.deathmatchTeams[marker.index].spawnPosition =
                        position;
                    continue;
                }

                if (marker.pointType == MapModePointType.EliminationSpawn &&
                    selectedConfiguration.eliminationTeams != null &&
                    marker.index >= 0 &&
                    marker.index < selectedConfiguration.eliminationTeams.Count &&
                    selectedConfiguration.eliminationTeams[marker.index] != null)
                {
                    selectedConfiguration.eliminationTeams[marker.index].spawnPosition =
                        position;
                    continue;
                }

                if (selectedConfiguration.captureTheFlagTeams == null ||
                    marker.teamIndex < 0 ||
                    marker.teamIndex >= selectedConfiguration.captureTheFlagTeams.Count)
                {
                    continue;
                }

                var team = selectedConfiguration.captureTheFlagTeams[marker.teamIndex];
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

        private static bool SpawnCountNeedsUpdate(MapModeConfiguration configuration)
        {
            if (configuration == null || !configuration.IsBattleRoyale)
            {
                return false;
            }

            var count = configuration.battleRoyaleSpawnPoints == null
                ? 0
                : configuration.battleRoyaleSpawnPoints.Count;
            return count != Mathf.Max(1, configuration.teamCount);
        }

        private static bool EnsureBattleRoyaleSpawnPoints(
            MapModeConfiguration configuration)
        {
            if (configuration == null || !configuration.IsBattleRoyale)
            {
                return false;
            }

            EnsureLists(configuration);
            var requiredCount = Mathf.Max(1, configuration.teamCount);
            var changed = false;
            var center = GetMapCenter(configuration.map);
            while (configuration.battleRoyaleSpawnPoints.Count < requiredCount)
            {
                var index = configuration.battleRoyaleSpawnPoints.Count;
                var angle = (index + 0.5f) / requiredCount * Mathf.PI * 2f;
                configuration.battleRoyaleSpawnPoints.Add(
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

            while (configuration.battleRoyaleSpawnPoints.Count > requiredCount)
            {
                configuration.battleRoyaleSpawnPoints.RemoveAt(
                    configuration.battleRoyaleSpawnPoints.Count - 1);
                changed = true;
            }

            return changed;
        }

        private static void SyncBattleRoyaleMarkers(
            MapModeConfiguration configuration)
        {
            if (configuration == null || !configuration.IsBattleRoyale)
            {
                return;
            }

            var parent = FindConfigurationMarkerParent(configuration, true);
            if (parent == null)
            {
                return;
            }

            var points = configuration.battleRoyaleSpawnPoints;
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
            MapModeConfiguration configuration)
        {
            if (configuration == null || !configuration.IsDeathmatch)
            {
                return;
            }

            var parent = FindConfigurationMarkerParent(configuration, true);
            if (parent == null)
            {
                return;
            }

            var teams = configuration.deathmatchTeams;
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
            MapModeConfiguration configuration)
        {
            if (configuration == null || !configuration.IsElimination)
            {
                return;
            }

            var parent = FindConfigurationMarkerParent(configuration, true);
            if (parent == null)
            {
                return;
            }

            var teams = configuration.eliminationTeams;
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

                var radius = 0f;
                if (marker.pointType == MapModePointType.BattleRoyaleSpawn &&
                    configuration.battleRoyaleSpawnPoints != null &&
                    marker.index >= 0 &&
                    marker.index < configuration.battleRoyaleSpawnPoints.Count &&
                    configuration.battleRoyaleSpawnPoints[marker.index] != null)
                {
                    radius = configuration.battleRoyaleSpawnPoints[marker.index].radius;
                }
                else if (marker.pointType == MapModePointType.DeathmatchSpawn &&
                         configuration.deathmatchTeams != null &&
                         marker.index >= 0 &&
                         marker.index < configuration.deathmatchTeams.Count &&
                         configuration.deathmatchTeams[marker.index] != null)
                {
                    radius = configuration.deathmatchTeams[marker.index].radius;
                }
                else if (marker.pointType == MapModePointType.EliminationSpawn &&
                         configuration.eliminationTeams != null &&
                         marker.index >= 0 &&
                         marker.index < configuration.eliminationTeams.Count &&
                         configuration.eliminationTeams[marker.index] != null)
                {
                    radius = configuration.eliminationTeams[marker.index].radius;
                }

                AddMarkerDisc(
                    marker.transform,
                    radius,
                    MapModePointMarker.GetColor(marker.pointType));
                Undo.RegisterCreatedObjectUndo(
                    marker.transform.Find(MarkerDiscName).gameObject,
                    "Show Spawn Marker");
            }
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
            configuration.battleRoyaleSpawnPoints ??= new List<MapModePoint>();
            configuration.captureTheFlagTeams ??= new List<MapModeTeamPoints>();
            configuration.deathmatchTeams ??= new List<MapModeTeamSpawn>();
            configuration.eliminationTeams ??= new List<MapModeTeamSpawn>();
        }

        private static void EnsureCaptureTheFlagTeams(
            MapModeConfiguration configuration)
        {
            EnsureLists(configuration);
            var requiredCount = Mathf.Max(2, configuration.teamCount);
            while (configuration.captureTheFlagTeams.Count < requiredCount)
            {
                var index = configuration.captureTheFlagTeams.Count;
                configuration.captureTheFlagTeams.Add(
                    new MapModeTeamPoints
                    {
                        teamId = $"Team {index + 1}"
                    });
            }

            while (configuration.captureTheFlagTeams.Count > requiredCount)
            {
                configuration.captureTheFlagTeams.RemoveAt(
                    configuration.captureTheFlagTeams.Count - 1);
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
    }
}
#endif
