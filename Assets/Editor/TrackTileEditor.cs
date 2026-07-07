using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrackTile))]
public class TrackTileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty trackType =
            serializedObject.FindProperty("trackType");

        SerializedProperty backAnchor =
            serializedObject.FindProperty("backAnchor");

        SerializedProperty frontAnchor =
            serializedObject.FindProperty("frontAnchor");

        SerializedProperty crosswalkSpawnPoint =
            serializedObject.FindProperty("crosswalkSpawnPoint");

        SerializedProperty finishTriggerPoint =
            serializedObject.FindProperty("finishTriggerPoint");

        // ===== Track =====

        EditorGUILayout.LabelField("Track", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(trackType);

        // ===== Anchors =====

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Anchors", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(backAnchor);

        EditorGUILayout.PropertyField(frontAnchor);

        // ===== Type Specific =====

        EditorGUILayout.Space();

        switch ((TrackTile.TrackType)trackType.enumValueIndex)
        {
            case TrackTile.TrackType.Crosswalk:

                EditorGUILayout.LabelField("Crosswalk", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(crosswalkSpawnPoint);

                break;

            case TrackTile.TrackType.Finish:

                EditorGUILayout.LabelField("Finish", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(finishTriggerPoint);

                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}