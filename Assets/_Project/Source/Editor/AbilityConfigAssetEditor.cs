using System;
using System.Linq;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EditorTools
{
    [CustomEditor(typeof(AbilityConfigAsset))]
    public sealed class AbilityConfigAssetEditor : Editor
    {
        private SerializedProperty _abilityId;
        private SerializedProperty _displayName;
        private SerializedProperty _hotkeySlot;
        private SerializedProperty _hotkeyKey;
        private SerializedProperty _targetType;
        private SerializedProperty _rangeType;
        private SerializedProperty _range;
        private SerializedProperty _components;

        private ReorderableList _componentList;
        private Type[] _componentTypes;
        private string[] _componentTypeNames;
        private int _selectedTypeIndex;

        private void OnEnable()
        {
            _abilityId = serializedObject.FindProperty("_abilityId");
            _displayName = serializedObject.FindProperty("_displayName");
            _hotkeySlot = serializedObject.FindProperty("_hotkeySlot");
            _hotkeyKey = serializedObject.FindProperty("_hotkeyKey");
            _targetType = serializedObject.FindProperty("_targetType");
            _rangeType = serializedObject.FindProperty("_rangeType");
            _range = serializedObject.FindProperty("_range");
            _components = serializedObject.FindProperty("_components");

            _componentTypes = typeof(LockInputComponentData).Assembly
                .GetTypes()
                .Where(t => typeof(IAbilityComponentData).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToArray();
            _componentTypeNames = _componentTypes.Select(t => t.Name).ToArray();

            _componentList = new ReorderableList(serializedObject, _components, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Components"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    SerializedProperty element = _components.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, new GUIContent(GetComponentDisplayName(element)), true);
                },
                elementHeightCallback = index =>
                    EditorGUI.GetPropertyHeight(_components.GetArrayElementAtIndex(index), true) + 4f
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_abilityId);
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_hotkeySlot);
            EditorGUILayout.PropertyField(_hotkeyKey);
            EditorGUILayout.PropertyField(_targetType);
            EditorGUILayout.PropertyField(_rangeType);
            EditorGUILayout.PropertyField(_range);

            DrawValidationWarnings();

            EditorGUILayout.Space();
            _componentList.DoLayoutList();

            EditorGUILayout.Space();
            if (_componentTypeNames.Length > 0)
            {
                _selectedTypeIndex = EditorGUILayout.Popup("Add Component", _selectedTypeIndex, _componentTypeNames);
                if (GUILayout.Button("Add Component"))
                {
                    object instance = Activator.CreateInstance(_componentTypes[_selectedTypeIndex]);
                    _components.InsertArrayElementAtIndex(_components.arraySize);
                    _components.GetArrayElementAtIndex(_components.arraySize - 1).managedReferenceValue = instance;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No IAbilityComponentData types found.", MessageType.Warning);
            }

            DrawPlayModePanel();

            serializedObject.ApplyModifiedProperties();
        }

        private static string GetComponentDisplayName(SerializedProperty element)
        {
            object value = element.managedReferenceValue;
            if (value != null)
                return value.GetType().Name;

            string fullName = element.managedReferenceFullTypename;
            if (!string.IsNullOrEmpty(fullName))
            {
                string[] parts = fullName.Split(' ');
                if (parts.Length >= 2)
                {
                    string typeName = parts[1];
                    int dot = typeName.LastIndexOf('.');
                    return dot >= 0 ? typeName.Substring(dot + 1) : typeName;
                }
            }

            return "Missing Component";
        }

        private void DrawValidationWarnings()
        {
            var asset = (AbilityConfigAsset)target;
            bool hasAnimation = false;
            bool hasSound = false;

            foreach (IAbilityComponentData component in asset.Components)
            {
                if (component is AnimationComponentData animation)
                {
                    hasAnimation = AddressableAssetRefUtility.IsAssigned(animation.CastClip);
                }

                if (component is SoundComponentData sound)
                {
                    hasSound = AddressableAssetRefUtility.IsAssigned(sound.SoundClip);
                }
            }

            if (!hasAnimation || !hasSound)
            {
                EditorGUILayout.HelpBox(
                    "Recommended: include AnimationComponentData and SoundComponentData for demo abilities.",
                    MessageType.Warning);
            }

            if (string.IsNullOrWhiteSpace(asset.AbilityId))
            {
                EditorGUILayout.HelpBox("Ability Id is required.", MessageType.Error);
            }
        }

        private Vector2 _logScroll;

        private void DrawPlayModePanel()
        {
            if (!EditorApplication.isPlaying)
                return;

            EditorGUILayout.Space();
            var asset = (AbilityConfigAsset)target;
            AbilityId replayAbilityId = string.IsNullOrWhiteSpace(asset.AbilityId)
                ? default
                : new AbilityId(asset.AbilityId);

            AbilityActivationLogDrawer.Draw(AbilityEditorPlayAccess.Log, ref _logScroll, replayAbilityId);
        }
    }
}
