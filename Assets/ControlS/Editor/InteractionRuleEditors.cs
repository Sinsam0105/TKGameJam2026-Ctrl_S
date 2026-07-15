#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ControlS.Editor
{
    [CustomEditor(typeof(RoomInteractable))]
    public sealed class RoomInteractableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("kind"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prompt"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));
            EditorGUILayout.Space();
            InteractionRuleEditorGui.DrawRules(serializedObject, serializedObject.FindProperty("rules"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(InteractionSequence))]
    public sealed class InteractionSequenceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sequenceName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockReentry"));
            InteractionRuleEditorGui.DrawManagedList(serializedObject,
                serializedObject.FindProperty("actions"), typeof(InteractionAction), "Add Action");
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(DesktopShortcut))]
    public sealed class DesktopShortcutEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("button"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("label"));
            InteractionRuleEditorGui.DrawManagedList(serializedObject,
                serializedObject.FindProperty("visibilityConditions"), typeof(InteractionCondition),
                "Add Visibility Condition");
            InteractionRuleEditorGui.DrawRules(serializedObject, serializedObject.FindProperty("rules"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    internal static class InteractionRuleEditorGui
    {
        public static void DrawRules(SerializedObject owner, SerializedProperty rules)
        {
            for (var i = 0; i < rules.arraySize; i++)
            {
                var rule = rules.GetArrayElementAtIndex(i);
                var label = rule.FindPropertyRelative("label");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(label);
                EditorGUILayout.PropertyField(rule.FindPropertyRelative("stopAfterExecution"));
                EditorGUILayout.PropertyField(rule.FindPropertyRelative("executeOnce"));
                DrawManagedList(owner, rule.FindPropertyRelative("conditions"),
                    typeof(InteractionCondition), "Add Condition");
                DrawManagedList(owner, rule.FindPropertyRelative("actions"),
                    typeof(InteractionAction), "Add Action");
                if (GUILayout.Button("Remove Rule")) { rules.DeleteArrayElementAtIndex(i); i--; }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Rule")) rules.InsertArrayElementAtIndex(rules.arraySize);
        }

        public static void DrawManagedList(SerializedObject owner, SerializedProperty list,
            Type baseType, string addLabel)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), true);
                if (GUILayout.Button("Remove")) { list.DeleteArrayElementAtIndex(i); i--; }
            }
            if (!GUILayout.Button(addLabel)) return;
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom(baseType)
                         .Where(t => !t.IsAbstract && !t.IsGenericType && !t.IsDefined(typeof(ObsoleteAttribute), false))
                         .OrderBy(t => t.Name))
            {
                var selected = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(selected.Name)), false, () =>
                {
                    owner.Update();
                    var targetList = owner.FindProperty(list.propertyPath);
                    targetList.InsertArrayElementAtIndex(targetList.arraySize);
                    targetList.GetArrayElementAtIndex(targetList.arraySize - 1).managedReferenceValue =
                        Activator.CreateInstance(selected);
                    owner.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }
}
#endif
