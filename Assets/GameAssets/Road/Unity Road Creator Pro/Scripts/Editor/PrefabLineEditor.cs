using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

namespace RoadCreatorPro
{
    [CustomEditor(typeof(PrefabLineCreator))]
    public class PrefabLineEditor : PointSystemEditor
    {

        public new void OnEnable()
        {
            base.OnEnable();

            for (int i = 0; i < targets.Length; i++)
            {
                PrefabLineCreator prefabLineCreator = (PrefabLineCreator)targets[i];
                prefabLineCreator.InitializeSystem();
                prefabLineCreator.settings = RoadCreatorSettings.GetSerializedSettings();
            }

            Tools.current = Tool.None;
            sDown = false;

            Undo.undoRedoPerformed += UndoPrefabLine;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoPrefabLine;
            Tools.current = Tool.Move;
        }

        private void UndoPrefabLine()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                ((PrefabLineCreator)targets[i]).Regenerate(false);
            }
        }

        public override void OnInspectorGUI()
        {
            if (PrefabStageUtility.GetPrefabStage(((PrefabLineCreator)target).gameObject) != null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            Utility.DisplayControls(ref ((PrefabLineCreator)target).controlsFolded, false);

            // Save and load
            EditorGUILayout.BeginHorizontal();
            serializedObject.FindProperty("prefabLinePreset").objectReferenceValue = EditorGUILayout.ObjectField("Prefab Line Preset", serializedObject.FindProperty("prefabLinePreset").objectReferenceValue, typeof(PrefabLinePreset), false);

            if (GUILayout.Button("Load"))
            {
                if (serializedObject.FindProperty("prefabLinePreset").objectReferenceValue != null)
                {
                    PrefabLinePreset prefabLinePreset = (PrefabLinePreset)serializedObject.FindProperty("prefabLinePreset").objectReferenceValue;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    Utility.CopyPrefabLinePresetToPrefabLineData(prefabLinePreset, serializedObject);
                    ((PrefabLineCreator)target).Regenerate(false);
                }
            }

            if (GUILayout.Button("Save"))
            {
                if (serializedObject.FindProperty("prefabLinePreset").objectReferenceValue != null)
                {
                    PrefabLinePreset prefabLinePreset = (PrefabLinePreset)serializedObject.FindProperty("prefabLinePreset").objectReferenceValue;
                    Utility.CopyPrefabLineDataToPrefabLinePreset(serializedObject, prefabLinePreset);
                    EditorUtility.SetDirty(serializedObject.FindProperty("prefabLinePreset").objectReferenceValue);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.FindProperty("detailLevel").floatValue = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Accuracy", "Determines how accurately the prefabs are spaced out."), serializedObject.FindProperty("detailLevel").floatValue), 0.01f, 20);
            serializedObject.FindProperty("fillGap").boolValue = EditorGUILayout.Toggle("Fill Gap", serializedObject.FindProperty("fillGap").boolValue);

            if (!serializedObject.FindProperty("fillGap").boolValue)
            {
                serializedObject.FindProperty("randomizeSpacing").boolValue = EditorGUILayout.Toggle("Randomize Spacing", serializedObject.FindProperty("randomizeSpacing").boolValue);

                if (serializedObject.FindProperty("randomizeSpacing").boolValue)
                {
                    serializedObject.FindProperty("spacing").floatValue = Mathf.Max(EditorGUILayout.FloatField("Min Spacing", serializedObject.FindProperty("spacing").floatValue), 0);
                    serializedObject.FindProperty("maxSpacing").floatValue = Mathf.Max(EditorGUILayout.FloatField("Max Spacing", serializedObject.FindProperty("maxSpacing").floatValue), serializedObject.FindProperty("spacing").floatValue);
                }
                else
                {
                    serializedObject.FindProperty("spacing").floatValue = Mathf.Max(EditorGUILayout.FloatField("Spacing", serializedObject.FindProperty("spacing").floatValue), 0);
                }

                if (GUILayout.Button("Calculate Spacing"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        ((PrefabLineCreator)targets[i]).spacing = ((PrefabLineCreator)targets[i]).prefabWidth;
                        ((PrefabLineCreator)targets[i]).maxSpacing = ((PrefabLineCreator)targets[i]).prefabWidth;
                    }
                }
            }

            serializedObject.FindProperty("deformPrefabsToCurve").boolValue = EditorGUILayout.Toggle("Deform Prefabs To Curve", serializedObject.FindProperty("deformPrefabsToCurve").boolValue);
            serializedObject.FindProperty("deformPrefabsToTerrain").boolValue = EditorGUILayout.Toggle("Deform Prefabs To Terrain", serializedObject.FindProperty("deformPrefabsToTerrain").boolValue);
            serializedObject.FindProperty("yOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Y Offset", serializedObject.FindProperty("yOffset").floatValue), -1, 1);
            serializedObject.FindProperty("rotationDirection").enumValueIndex = (int)(PrefabLineCreator.RotationDirection)EditorGUILayout.EnumPopup("Rotation Direction", (PrefabLineCreator.RotationDirection)serializedObject.FindProperty("rotationDirection").enumValueIndex);

            if (!serializedObject.FindProperty("fillGap").boolValue && !serializedObject.FindProperty("deformPrefabsToCurve").boolValue)
            {
                serializedObject.FindProperty("rotationRandomization").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Rotation Randomization", serializedObject.FindProperty("rotationRandomization").floatValue), 0, 180);
            }

            if (serializedObject.FindProperty("deformPrefabsToTerrain").boolValue && !serializedObject.FindProperty("fillGap").boolValue && !serializedObject.FindProperty("deformPrefabsToCurve").boolValue)
            {
                serializedObject.FindProperty("centralYModification").boolValue = EditorGUILayout.Toggle(new GUIContent("Central Y Modification", "Adapts all vertices to the terrain heigh at the center of the object's position. Used by signs for example."), serializedObject.FindProperty("centralYModification").boolValue);
            }

            if (serializedObject.FindProperty("controlled").boolValue && serializedObject.FindProperty("deformPrefabsToTerrain").boolValue && !serializedObject.FindProperty("centralYModification").boolValue)
            {
                serializedObject.FindProperty("bridgePillarMode").boolValue = EditorGUILayout.Toggle(new GUIContent("Bridge Pillar Mode", "Fills the gap from terrain to original position"), serializedObject.FindProperty("bridgePillarMode").boolValue);
                if (!serializedObject.FindProperty("bridgePillarMode").boolValue)
                {
                    serializedObject.FindProperty("onlyYModifyBottomVertices").boolValue = EditorGUILayout.Toggle(new GUIContent("Only Modify Bottom Vertices", "Used by under arch bridges to extend the in-built pillars to the ground"), serializedObject.FindProperty("onlyYModifyBottomVertices").boolValue);
                }
            }

            GUILayout.Space(20);
            GUILayout.Label("Prefabs", EditorStyles.boldLabel);
            serializedObject.FindProperty("startPrefab").objectReferenceValue = EditorGUILayout.ObjectField("Start Prefab", serializedObject.FindProperty("startPrefab").objectReferenceValue, typeof(GameObject), false);
            serializedObject.FindProperty("mainPrefab").objectReferenceValue = EditorGUILayout.ObjectField("Main Prefab", serializedObject.FindProperty("mainPrefab").objectReferenceValue, typeof(GameObject), false);
            serializedObject.FindProperty("endPrefab").objectReferenceValue = EditorGUILayout.ObjectField("End Prefab", serializedObject.FindProperty("endPrefab").objectReferenceValue, typeof(GameObject), false);
            serializedObject.FindProperty("xScale").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("X Scale", serializedObject.FindProperty("xScale").floatValue), 0.1f, 10);
            Utility.DisplayCurveEditor(serializedObject.FindProperty("yScale"), "Y Scale", ((PrefabLineCreator)target).settings);
            Utility.DisplayCurveEditor(serializedObject.FindProperty("zScale"), "Z Scale", ((PrefabLineCreator)target).settings);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                ((PrefabLineCreator)target).Regenerate(false);
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Snap Points To Terrain"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ((PrefabLineCreator)targets[i]).SnapPointsToTerrain();
                    ((PrefabLineCreator)targets[i]).Regenerate(false);
                }
            }

            if (GUILayout.Button("Reset Prefab Line"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Transform targetTransform = ((PrefabLineCreator)target).transform;
                    // Remove points
                    for (int j = targetTransform.GetChild(0).childCount - 1; j >= 0; j--)
                    {
                        DestroyImmediate(targetTransform.GetChild(0).GetChild(0).gameObject);
                    }

                    targetTransform.GetComponent<PrefabLineCreator>().Regenerate(false);
                }
            }

            if (GUILayout.Button("Update Prefab Line"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ((PrefabLineCreator)targets[i]).Regenerate(false);
                }
            }

            if (!serializedObject.FindProperty("controlled").boolValue)
            {
                if (GUILayout.Button("Convert To Static Meshes"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        ((PrefabLineCreator)targets[i]).ConvertToStatic();
                    }
                }
            }
        }
    }
}