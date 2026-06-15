using System;
using System.Collections.Generic;
using System.Linq;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.AbilitiesDemo.Application;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public sealed class AbilityAuthoringWindow : EditorWindow
    {
        private const string ConfigFolder = "Assets/_Project/Configs/Abilities";

        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private Vector2 _logScroll;
        private int _selectedIndex = -1;
        private AbilityConfigAsset[] _assets = Array.Empty<AbilityConfigAsset>();
        private Editor _assetEditor;
        private const string NewAbilityId = "new_ability";
        private KeyCode _playModeBindKey = KeyCode.None;
        private bool _captureNextKey;

        [MenuItem("AvantajPrim/Abilities/Authoring Window")]
        public static void Open()
        {
            AbilityAuthoringWindow window = GetWindow<AbilityAuthoringWindow>("Ability Authoring");
            window.minSize = new Vector2(720f, 420f);
            window.RefreshAssetList();
        }

        private void OnEnable() => RefreshAssetList();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawAssetListPanel();
            DrawDetailPanel();
            EditorGUILayout.EndHorizontal();

            if (EditorApplication.isPlaying)
                DrawPlayModePanel();
        }

        private void DrawAssetListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240f));
            EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
                RefreshAssetList();
            if (GUILayout.Button("Create"))
                CreateNewAbility();
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            for (int i = 0; i < _assets.Length; i++)
            {
                AbilityConfigAsset asset = _assets[i];
                if (asset == null)
                    continue;

                string label = $"{asset.HotkeySlot}: {asset.DisplayName} ({asset.AbilityId})";
                if (GUILayout.Toggle(_selectedIndex == i, label, "Button"))
                {
                    if (_selectedIndex != i)
                        SelectAsset(i);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical();
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            if (_selectedIndex < 0 || _selectedIndex >= _assets.Length || _assets[_selectedIndex] == null)
            {
                EditorGUILayout.HelpBox("Select or create an ability config.", MessageType.Info);
            }
            else
            {
                AbilityConfigAsset asset = _assets[_selectedIndex];
                EditorGUILayout.LabelField(asset.name, EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Duplicate"))
                    DuplicateSelected();
                if (GUILayout.Button("Validate"))
                    ShowValidation(asset);
                if (GUILayout.Button("Register Addressable"))
                    AvantajPrimProjectTools.RegisterAbilityAddressable(asset);
                EditorGUILayout.EndHorizontal();

                if (_assetEditor == null || _assetEditor.target != asset)
                {
                    DestroyImmediate(_assetEditor);
                    _assetEditor = Editor.CreateEditor(asset);
                }

                _assetEditor.OnInspectorGUI();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPlayModePanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play Mode — Hotkey Mapping", EditorStyles.boldLabel);

            var hotkeys = AbilityEditorPlayAccess.HotkeyBindings as AbilityHotkeyBindingService;
            var catalog = AbilityEditorPlayAccess.AbilityCatalog as AbilityCatalog;
            if (hotkeys == null || catalog == null)
            {
                EditorGUILayout.HelpBox("Play Mode services are not wired yet. Start the demo scene.", MessageType.Warning);
                return;
            }

            foreach (AbilityDefinition definition in catalog.Enumerate().OrderBy(d => d.HotkeySlot))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(definition.Id.Value, GUILayout.Width(140f));
                var effective = (KeyCode)hotkeys.GetEffectiveKeyCode(definition);
                EditorGUILayout.LabelField(effective == KeyCode.None ? "(none)" : effective.ToString(), GUILayout.Width(80f));

                if (GUILayout.Button("Bind", GUILayout.Width(60f)))
                {
                    _captureNextKey = true;
                    _selectedIndex = Array.FindIndex(_assets, a => a != null && a.AbilityId == definition.Id.Value);
                }

                if (GUILayout.Button("Clear", GUILayout.Width(60f)))
                    hotkeys.ClearRuntimeOverride(definition.Id.Value);

                EditorGUILayout.EndHorizontal();
            }

            if (_captureNextKey)
            {
                EditorGUILayout.HelpBox("Press a key to bind to the selected ability…", MessageType.Info);
                Event e = Event.current;
                if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
                {
                    if (_selectedIndex >= 0 && _selectedIndex < _assets.Length && _assets[_selectedIndex] != null)
                    {
                        hotkeys.SetRuntimeOverride(_assets[_selectedIndex].AbilityId, (int)e.keyCode);
                        _playModeBindKey = e.keyCode;
                    }

                    _captureNextKey = false;
                    e.Use();
                    Repaint();
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Runtime Overrides"))
                hotkeys.ClearAllRuntimeOverrides();

            if (_selectedIndex >= 0 && _selectedIndex < _assets.Length && _assets[_selectedIndex] != null &&
                GUILayout.Button("Save Hotkey To Asset"))
            {
                SaveHotkeyToAsset(_assets[_selectedIndex], hotkeys);
            }

            EditorGUILayout.EndHorizontal();

            DrawActivationLogPanel();
        }

        private void DrawActivationLogPanel()
        {
            AbilityActivationLog log = AbilityEditorPlayAccess.Log;
            AbilityId replayAbilityId = _selectedIndex >= 0 && _selectedIndex < _assets.Length && _assets[_selectedIndex] != null
                ? new AbilityId(_assets[_selectedIndex].AbilityId)
                : default;

            AbilityActivationLogDrawer.Draw(log, ref _logScroll, replayAbilityId);
        }

        private void SaveHotkeyToAsset(AbilityConfigAsset asset, AbilityHotkeyBindingService hotkeys)
        {
            if (!hotkeys.TryGetRuntimeOverride(asset.AbilityId, out int keyCode) || keyCode == 0)
            {
                EditorUtility.DisplayDialog("Ability Authoring", "No runtime override to save for this ability.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Save Hotkey",
                    $"Write KeyCode.{(KeyCode)keyCode} to '{asset.name}'?",
                    "Save",
                    "Cancel"))
            {
                return;
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_hotkeyKey").enumValueIndex = keyCode;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            RefreshAssetList();
        }

        private void RefreshAssetList()
        {
            _assets = AssetDatabase.FindAssets("t:AbilityConfigAsset", new[] { ConfigFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AbilityConfigAsset>)
                .Where(a => a != null)
                .OrderBy(a => a.HotkeySlot)
                .ThenBy(a => a.AbilityId)
                .ToArray();

            if (_selectedIndex >= _assets.Length)
                _selectedIndex = _assets.Length - 1;

            Repaint();
        }

        private void SelectAsset(int index)
        {
            _selectedIndex = index;
            DestroyImmediate(_assetEditor);
            _assetEditor = null;
        }

        private void CreateNewAbility()
        {
            EnsureFolder(ConfigFolder);
            string safeId = string.IsNullOrWhiteSpace(NewAbilityId) ? "new_ability" : NewAbilityId.Trim().ToLowerInvariant();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{ConfigFolder}/{safeId}.asset");
            AbilityConfigAsset asset = ScriptableObject.CreateInstance<AbilityConfigAsset>();
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_abilityId").stringValue = safeId;
            serialized.FindProperty("_displayName").stringValue = safeId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            RefreshAssetList();
            _selectedIndex = Array.FindIndex(_assets, a => a == asset);
        }

        private void DuplicateSelected()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _assets.Length)
                return;

            AbilityConfigAsset source = _assets[_selectedIndex];
            string path = AssetDatabase.GenerateUniqueAssetPath($"{ConfigFolder}/{source.name}_Copy.asset");
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path))
                return;

            AssetDatabase.SaveAssets();
            RefreshAssetList();
            AbilityConfigAsset duplicated = AssetDatabase.LoadAssetAtPath<AbilityConfigAsset>(path);
            _selectedIndex = Array.FindIndex(_assets, a => a == duplicated);
        }

        private static void ShowValidation(AbilityConfigAsset asset)
        {
            List<string> issues = AbilityAuthoringValidation.CollectIssues(new[] { asset });
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation", "No issues found.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Validation", string.Join("\n", issues), "OK");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            const string root = "Assets/_Project";
            if (!AssetDatabase.IsValidFolder(root))
                AssetDatabase.CreateFolder("Assets", "_Project");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Configs"))
                AssetDatabase.CreateFolder(root, "Configs");
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets/_Project/Configs", "Abilities");
        }

        private void OnDestroy()
        {
            DestroyImmediate(_assetEditor);
        }
    }

    internal static class AbilityAuthoringValidation
    {
        public static List<string> CollectIssues(IEnumerable<AbilityConfigAsset> assets)
        {
            var list = assets.Where(a => a != null).ToList();
            var issues = new List<string>();
            var ids = new HashSet<string>();
            var slots = new HashSet<int>();

            foreach (AbilityConfigAsset asset in list)
            {
                if (string.IsNullOrWhiteSpace(asset.AbilityId))
                    issues.Add($"{asset.name}: missing Ability Id");
                else if (!ids.Add(asset.AbilityId))
                    issues.Add($"Duplicate Ability Id '{asset.AbilityId}'");

                if (asset.HotkeySlot > 0 && !slots.Add(asset.HotkeySlot))
                    issues.Add($"Duplicate Hotkey Slot {asset.HotkeySlot}");

                bool hasAnimation = false;
                bool hasSound = false;
                foreach (IAbilityComponentData component in asset.Components)
                {
                    if (component is AnimationComponentData animation)
                        hasAnimation = AddressableAssetRefUtility.IsAssigned(animation.CastClip);
                    if (component is SoundComponentData sound)
                        hasSound = AddressableAssetRefUtility.IsAssigned(sound.SoundClip);
                }

                if (!hasAnimation || !hasSound)
                    issues.Add($"{asset.name}: recommended Animation + Sound components");
            }

            return issues;
        }
    }
}
