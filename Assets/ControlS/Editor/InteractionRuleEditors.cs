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
        private bool showLegacy;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("kind"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prompt"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Conditional Rules", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("위에서부터 조건을 검사하고, 처음 매칭된 규칙의 액션을 순서대로 실행합니다.",
                MessageType.Info);
            InteractionRuleEditorGui.DrawRules(serializedObject, serializedObject.FindProperty("rules"));

            EditorGUILayout.Space(8f);
            showLegacy = EditorGUILayout.Foldout(showLegacy, "Legacy Payload / UnityEvents", true);
            if (showLegacy)
            {
                EditorGUILayout.HelpBox("Rules가 비어 있을 때만 실행되는 이전 방식의 호환 필드입니다.",
                    MessageType.None);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interactionNarration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interactionUI"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onInteract"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onInteractNarration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onInteractGameObject"));
            }
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
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
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
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Visibility Conditions (AND)", EditorStyles.boldLabel);
            InteractionRuleEditorGui.DrawManagedList(serializedObject,
                serializedObject.FindProperty("visibilityConditions"), typeof(InteractionCondition),
                "Add Visibility Condition");
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Click Rules", EditorStyles.boldLabel);
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
                EditorGUILayout.BeginHorizontal();
                rule.isExpanded = EditorGUILayout.Foldout(rule.isExpanded,
                    string.IsNullOrWhiteSpace(label.stringValue) ? $"Rule {i + 1}" : label.stringValue, true);
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(28f))) rules.MoveArrayElement(i, i - 1);
                GUI.enabled = i < rules.arraySize - 1;
                if (GUILayout.Button("▼", GUILayout.Width(28f))) rules.MoveArrayElement(i, i + 1);
                GUI.enabled = true;
                if (GUILayout.Button("×", GUILayout.Width(28f)))
                {
                    rules.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (rule.isExpanded)
                {
                    EditorGUILayout.PropertyField(label, new GUIContent("Label"));
                    EditorGUILayout.PropertyField(rule.FindPropertyRelative("stopAfterExecution"));
                    EditorGUILayout.PropertyField(rule.FindPropertyRelative("executeOnce"));
                    EditorGUILayout.Space(3f);
                    EditorGUILayout.LabelField("Conditions (AND)", EditorStyles.boldLabel);
                    DrawManagedList(owner, rule.FindPropertyRelative("conditions"),
                        typeof(InteractionCondition), "Add Condition");
                    EditorGUILayout.LabelField("Actions (Sequence)", EditorStyles.boldLabel);
                    DrawManagedList(owner, rule.FindPropertyRelative("actions"),
                        typeof(InteractionAction), "Add Action");
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Rule"))
            {
                var index = rules.arraySize;
                rules.InsertArrayElementAtIndex(index);
                var created = rules.GetArrayElementAtIndex(index);
                created.FindPropertyRelative("label").stringValue = $"Rule {index + 1}";
                created.FindPropertyRelative("conditions").ClearArray();
                created.FindPropertyRelative("actions").ClearArray();
                created.FindPropertyRelative("stopAfterExecution").boolValue = true;
                created.FindPropertyRelative("executeOnce").boolValue = false;
                created.isExpanded = true;
            }
        }

        public static void DrawManagedList(SerializedObject owner, SerializedProperty list,
            Type baseType, string addLabel)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element, new GUIContent(GetManagedTypeName(element)), true);
                if (GUILayout.Button("×", GUILayout.Width(26f)))
                {
                    list.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button(addLabel)) ShowTypeMenu(owner, list.propertyPath, baseType);
        }

        private static void ShowTypeMenu(SerializedObject owner, string propertyPath, Type baseType)
        {
            var menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .OrderBy(type => type.Name);
            foreach (var type in types)
            {
                var captured = type;
                menu.AddItem(new GUIContent(Nicify(captured.Name)), false, () =>
                {
                    owner.Update();
                    var list = owner.FindProperty(propertyPath);
                    var index = list.arraySize;
                    list.InsertArrayElementAtIndex(index);
                    list.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(captured);
                    owner.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        private static string GetManagedTypeName(SerializedProperty property)
        {
            var name = property.managedReferenceFullTypename;
            if (string.IsNullOrWhiteSpace(name)) return "Missing Type";
            var typeName = name.Split(' ').Last();
            return Nicify(typeName.Split('.').Last());
        }

        private static string Nicify(string value)
        {
            value = value.Replace("Condition", string.Empty).Replace("Action", string.Empty);
            return ObjectNames.NicifyVariableName(value);
        }
    }
}
#endif
