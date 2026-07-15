#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ControlS.Editor
{
    [CustomEditor(typeof(PuzzleDefinitionSO))]
    public sealed class PuzzleDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("puzzleId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("completionFlag"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowRepeat"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requirements"), true);
            var matcher = serializedObject.FindProperty("matcher");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Answer Matcher", EditorStyles.boldLabel);
            if (matcher.managedReferenceValue != null) EditorGUILayout.PropertyField(matcher, true);
            if (GUILayout.Button(matcher.managedReferenceValue == null ? "Select Matcher" : "Change Matcher"))
            {
                var menu = new GenericMenu();
                foreach (var type in TypeCache.GetTypesDerivedFrom<AnswerMatcher>()
                             .Where(item => !item.IsAbstract).OrderBy(item => item.Name))
                {
                    var selected = type;
                    menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(selected.Name)), false, () =>
                    {
                        serializedObject.Update();
                        serializedObject.FindProperty("matcher").managedReferenceValue = Activator.CreateInstance(selected);
                        serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(PuzzleRunner), true)]
    public sealed class PuzzleRunnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("definition"));
            DrawOutcome("onSuccess", "Success");
            DrawOutcome("onIncorrect", "Incorrect");
            DrawOutcome("onConditionsNotMet", "Conditions Not Met");
            DrawOutcome("onAlreadySolved", "Already Solved");
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOutcome(string propertyName, string label)
        {
            var outcome = serializedObject.FindProperty(propertyName);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            InteractionRuleEditorGui.DrawManagedList(serializedObject,
                outcome.FindPropertyRelative("actions"), typeof(InteractionAction), "Add Action");
            EditorGUILayout.PropertyField(outcome.FindPropertyRelative("onCompleted"));
        }
    }
}
#endif
