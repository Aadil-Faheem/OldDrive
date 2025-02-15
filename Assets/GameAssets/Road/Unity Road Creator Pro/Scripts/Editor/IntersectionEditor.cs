using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

namespace RoadCreatorPro
{
    [CustomEditor(typeof(Intersection))]
    public class IntersectionEditor : Editor
    {
        private void OnEnable()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                Intersection intersection = (Intersection)targets[i];
                Transform transform = intersection.transform;
                intersection.InitializeIntersection();
                intersection.settings = RoadCreatorSettings.GetSerializedSettings();
                intersection.Regenerate(true, false);
            }

            Tools.current = Tool.None;
            Undo.undoRedoPerformed += UndoIntersection;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoIntersection;
            Tools.current = Tool.Move;
        }

        private void UndoIntersection()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                ((Intersection)targets[i]).Regenerate(true, false);
            }
        }

        private void OnSceneGUI()
        {
            if (PrefabStageUtility.GetPrefabStage(((Intersection)target).gameObject) != null)
            {
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(((Intersection)target).gameObject) != PrefabAssetType.NotAPrefab)
            {
                return;
            }

            Event currentEvent = Event.current;

            Draw(currentEvent, (Intersection)target);
        }

        private void Draw(Event currentEvent, Intersection intersection)
        {
            if (!intersection.automaticallyCalculateCurvePoints)
            {
                // Draw control points     
                for (int i = 0; i < intersection.connections.Count; i++)
                {
                    // Lines
                    Handles.color = Color.black;
                    Handles.DrawLine(intersection.connections[i].leftPoint, intersection.connections[i].leftPoint + intersection.connections[i].leftTangent);
                    Handles.DrawLine(intersection.connections[i].rightPoint, intersection.connections[i].rightPoint + intersection.connections[i].rightTangent);

                    // Points
                    Handles.color = intersection.settings.FindProperty("controlPointColour").colorValue;

                    Handles.CapFunction shape;
                    int shapeIndex = intersection.settings.FindProperty("pointShape").enumValueIndex;
                    if (shapeIndex == 0)
                    {
                        shape = Handles.CylinderHandleCap;
                    }
                    else if (shapeIndex == 1)
                    {
                        shape = Handles.SphereHandleCap;
                    }
                    else if (shapeIndex == 2)
                    {
                        shape = Handles.CubeHandleCap;
                    }
                    else
                    {
                        shape = Handles.ConeHandleCap;
                    }

                    shape(0, intersection.connections[i].leftPoint + intersection.connections[i].leftTangent, Quaternion.Euler(270, 0, 0), intersection.settings.FindProperty("intersectionCurvePointSize").floatValue, EventType.Repaint);
                    shape(0, intersection.connections[i].rightPoint + intersection.connections[i].rightTangent, Quaternion.Euler(270, 0, 0), intersection.settings.FindProperty("intersectionCurvePointSize").floatValue, EventType.Repaint);
                    EditorGUI.BeginChangeCheck();
                    intersection.connections[i].leftTangent = Utility.DrawPositionHandle(intersection.settings.FindProperty("scalePointsWhenZoomed").boolValue, intersection.settings.FindProperty("intersectionCurvePointSize").floatValue, intersection.connections[i].leftPoint + intersection.connections[i].leftTangent, Quaternion.identity) - intersection.connections[i].leftPoint;
                    intersection.connections[i].rightTangent = Utility.DrawPositionHandle(intersection.settings.FindProperty("scalePointsWhenZoomed").boolValue, intersection.settings.FindProperty("intersectionCurvePointSize").floatValue, intersection.connections[i].rightPoint + intersection.connections[i].rightTangent, Quaternion.identity) - intersection.connections[i].rightPoint;

                    if (EditorGUI.EndChangeCheck())
                    {
                        intersection.Regenerate(true, false);
                    }
                }

                SceneView.RepaintAll();
            }

            if (intersection.tab == 1)
            {
                // Draw selected connection
                Handles.color = intersection.settings.FindProperty("selectedObjectColour").colorValue;
                Handles.ArrowHandleCap(0, intersection.connections[intersection.connectionTab].roadPoint.transform.position + new Vector3(0, intersection.settings.FindProperty("selectedObjectArrowSize").floatValue * 1.15f, 0), Quaternion.Euler(90, 0, 0), intersection.settings.FindProperty("selectedObjectArrowSize").floatValue, EventType.Repaint);
            }
            else if (intersection.tab == 2)
            {
                if (intersection.mainRoads.Count > 0)
                {
                    // Prevent error
                    if (intersection.mainRoadTab > intersection.mainRoads.Count - 1)
                    {
                        intersection.mainRoadTab = 0;
                    }

                    // Draw selected main road
                    Handles.color = intersection.settings.FindProperty("selectedObjectColour").colorValue;
                    Handles.ArrowHandleCap(0, intersection.mainRoads[intersection.mainRoadTab].centerPoint + new Vector3(0, intersection.settings.FindProperty("selectedObjectArrowSize").floatValue * 1.15f, 0), Quaternion.Euler(90, 0, 0), intersection.settings.FindProperty("selectedObjectArrowSize").floatValue, EventType.Repaint);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (PrefabStageUtility.GetPrefabStage(((Intersection)target).gameObject) != null)
            {
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(((Intersection)target).gameObject) != PrefabAssetType.NotAPrefab)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            serializedObject.FindProperty("tab").intValue = GUILayout.Toolbar(serializedObject.FindProperty("tab").intValue, new string[] { "General", "Connections", "Terrain", "Main Roads" });

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorGUI.BeginChangeCheck();

            if (serializedObject.FindProperty("tab").intValue == 0)
            {
                InspectGeneral();
            }
            else if (serializedObject.FindProperty("tab").intValue == 1)
            {
                InspectConnections();
            }
            else if (serializedObject.FindProperty("tab").intValue == 2)
            {
                InspectTerrain();
            }
            else
            {
                InspectMainRoads();
            }

            GUILayout.Space(20);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                for (int i = 0; i < targets.Length; i++)
                {
                    ((Intersection)targets[i]).Regenerate(true, false);
                }
            }

            if (GUILayout.Button("Flatten Intersection"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ((Intersection)targets[i]).Flatten();
                }
            }

            if (GUILayout.Button("Update Intersection"))
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                for (int i = 0; i < targets.Length; i++)
                {
                    ((Intersection)targets[i]).Regenerate(false, false);
                }
            }
        }

        private void InspectGeneral()
        {
            serializedObject.FindProperty("detailLevel").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Detail Level", serializedObject.FindProperty("detailLevel").floatValue), 0.01f, 20);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mainMaterials"), true);
            serializedObject.FindProperty("mainPhysicMaterial").objectReferenceValue = EditorGUILayout.ObjectField("Physic Material", serializedObject.FindProperty("mainPhysicMaterial").objectReferenceValue, typeof(PhysicMaterial), false);
            serializedObject.FindProperty("automaticallyCalculateCurvePoints").boolValue = EditorGUILayout.Toggle("Automatically Calculate Curve Points", serializedObject.FindProperty("automaticallyCalculateCurvePoints").boolValue);

            if (GUILayout.Button("Recalculate Curve Points"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ((Intersection)targets[i]).RecalculateTangents();
                }
            }

            GUILayout.Space(20);
            serializedObject.FindProperty("flipUvs").boolValue = EditorGUILayout.Toggle("Flip Uvs", serializedObject.FindProperty("flipUvs").boolValue);
            serializedObject.FindProperty("uvXScale").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Uv X Scale", serializedObject.FindProperty("uvXScale").floatValue), 0.01f, 10f);
            serializedObject.FindProperty("generateColliders").boolValue = EditorGUILayout.Toggle("Generate Colliders", serializedObject.FindProperty("generateColliders").boolValue);

            GUILayout.Space(20);
            GUILayout.Label("LOD", EditorStyles.boldLabel);
            serializedObject.FindProperty("lodLevels").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Lod Levels", "Excludes original mesh"), serializedObject.FindProperty("lodLevels").intValue), 0, 3);

            if (serializedObject.FindProperty("lodLevels").intValue > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("(Distance/Vertex Divison)", "Distance: The distance in percent from the camera that the lod level stops showing, Vertex Division: Vertex Count/Vertex Division is the amount of vertices used for that lod level"));
                GUILayout.EndHorizontal();

                for (int i = 0; i < serializedObject.FindProperty("lodLevels").intValue; i++)
                {
                    if (i < serializedObject.FindProperty("lodDistances").arraySize)
                    {
                        float minDistance = 0;
                        float maxDistance = 1 - (serializedObject.FindProperty("lodLevels").intValue - i) * 0.01f;
                        if (i > 0)
                        {
                            minDistance = serializedObject.FindProperty("lodDistances").GetArrayElementAtIndex(i - 1).floatValue;
                        }

                        int minDivision = 2;
                        if (i > 0)
                        {
                            minDivision = serializedObject.FindProperty("lodVertexDivisions").GetArrayElementAtIndex(i - 1).intValue + 1;
                        }

                        GUILayout.BeginHorizontal();
                        serializedObject.FindProperty("lodDistances").GetArrayElementAtIndex(i).floatValue = Mathf.Clamp(EditorGUILayout.FloatField(serializedObject.FindProperty("lodDistances").GetArrayElementAtIndex(i).floatValue), minDistance + 0.01f, maxDistance);
                        serializedObject.FindProperty("lodVertexDivisions").GetArrayElementAtIndex(i).intValue = Mathf.Max(EditorGUILayout.IntField(serializedObject.FindProperty("lodVertexDivisions").GetArrayElementAtIndex(i).intValue), minDivision);
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

        private void InspectConnections()
        {
            // Generate options
            string[] options = new string[serializedObject.FindProperty("connections").arraySize];
            for (int i = 0; i < serializedObject.FindProperty("connections").arraySize; i++)
            {
                options[i] = (i + 1).ToString();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Connection");
            if (serializedObject.FindProperty("connectionTab").intValue > 0 && GUILayout.Button("←"))
            {
                serializedObject.FindProperty("connectionTab").intValue -= 1;
            }

            serializedObject.FindProperty("connectionTab").intValue = EditorGUILayout.Popup(serializedObject.FindProperty("connectionTab").intValue, options);

            if (serializedObject.FindProperty("connectionTab").intValue < serializedObject.FindProperty("connections").arraySize - 1 && GUILayout.Button("→"))
            {
                serializedObject.FindProperty("connectionTab").intValue += 1;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.Label("Lane Turn Markings", EditorStyles.boldLabel);
            serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsRepetitions").intValue = Mathf.Clamp(EditorGUILayout.IntField("Repetitions", serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsRepetitions").intValue), 0, 5);

            if (serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsRepetitions").intValue > 0)
            {
                serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsAmount").intValue = Mathf.Clamp(EditorGUILayout.IntField("Amount (Per Repetition)", serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsAmount").intValue), 1, 20);
                serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsStartOffset").floatValue = Mathf.Max(EditorGUILayout.FloatField("Start Offset", serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsStartOffset").floatValue), 0);
                serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsContiniusOffset").floatValue = Mathf.Max(EditorGUILayout.FloatField("Continius Offset", serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsContiniusOffset").floatValue), 1);
                serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsYOffset").floatValue = Mathf.Clamp01(EditorGUILayout.FloatField("Y Offset", serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsYOffset").floatValue));

                GUILayout.Space(20);
                GUILayout.Label("(Left/Forward/Right)");
                // Display checkboxes
                for (int i = 0; i < serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkings").arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("#" + (i + 1));
                    serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkings").GetArrayElementAtIndex(i).FindPropertyRelative("one").boolValue = EditorGUILayout.Toggle(serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkings").GetArrayElementAtIndex(i).FindPropertyRelative("one").boolValue);
                    serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkings").GetArrayElementAtIndex(i).FindPropertyRelative("two").boolValue = EditorGUILayout.Toggle(serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkings").GetArrayElementAtIndex(i).FindPropertyRelative("two").boolValue);
                    serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkings").GetArrayElementAtIndex(i).FindPropertyRelative("three").boolValue = EditorGUILayout.Toggle(serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkings").GetArrayElementAtIndex(i).FindPropertyRelative("three").boolValue);
                    EditorGUILayout.EndHorizontal();
                }

                // Display X-offsets
                GUILayout.Space(20);
                GUILayout.Label("X-Offets");
                serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("sameXOffsetsForAllRepetitions").boolValue = EditorGUILayout.Toggle("Same X-Offsets For All Repetitions", serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("sameXOffsetsForAllRepetitions").boolValue);

                if (!serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("sameXOffsetsForAllRepetitions").boolValue)
                {
                    GUILayout.Label("Vertical: Repeations, Horizontal: Amount");
                }
                else
                {
                    GUILayout.Label("Horizontal: Amount");
                }

                int max = serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsXOffsets").arraySize;
                if (serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("sameXOffsetsForAllRepetitions").boolValue)
                {
                    max = 1;
                }

                for (int i = 0; i < max; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsXOffsets").arraySize > i)
                    {
                        for (int j = 0; j < serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsXOffsets").GetArrayElementAtIndex(i).FindPropertyRelative("list").arraySize; j++)
                        {
                            serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsXOffsets").GetArrayElementAtIndex(i).FindPropertyRelative("list").GetArrayElementAtIndex(j).floatValue = EditorGUILayout.FloatField(serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkingsXOffsets").GetArrayElementAtIndex(i).FindPropertyRelative("list").GetArrayElementAtIndex(j).floatValue, GUILayout.MinWidth(15));
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                serializedObject.FindProperty("connections").GetArrayElementAtIndex(serializedObject.FindProperty("connectionTab").intValue).FindPropertyRelative("turnMarkings").ClearArray();
            }
        }

        private void InspectTerrain ()
        {
            GUILayout.Label("Terrain Deformation", EditorStyles.boldLabel);
            serializedObject.FindProperty("modifyTerrainHeight").boolValue = EditorGUILayout.Toggle("Modify Terrain Height", serializedObject.FindProperty("modifyTerrainHeight").boolValue);

            if (serializedObject.FindProperty("modifyTerrainHeight").boolValue)
            {
                serializedObject.FindProperty("terrainRadius").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Radius", serializedObject.FindProperty("terrainRadius").floatValue), 0, 100);
                serializedObject.FindProperty("terrainSmoothingRadius").intValue = Mathf.Clamp(EditorGUILayout.IntField("Smoothing Radius", serializedObject.FindProperty("terrainSmoothingRadius").intValue), 0, 7);

                if (serializedObject.FindProperty("terrainSmoothingRadius").intValue > 0)
                {
                    serializedObject.FindProperty("terrainSmoothingAmount").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Smoothing Amount", serializedObject.FindProperty("terrainSmoothingAmount").floatValue), 0, 1);
                }

                serializedObject.FindProperty("terrainAngle").floatValue = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Angle", "Angle in degrees"), serializedObject.FindProperty("terrainAngle").floatValue), 0, 89);
                serializedObject.FindProperty("terrainExtraMaxHeight").floatValue = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Extra Max Height", "The width of the section next to the intersection that has the same height"), serializedObject.FindProperty("terrainExtraMaxHeight").floatValue), 1, 10);
                serializedObject.FindProperty("terrainModificationYOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Y Offset", serializedObject.FindProperty("terrainModificationYOffset").floatValue), 0.01f, 2f);
                serializedObject.FindProperty("modifyTerrainOnUpdate").boolValue = EditorGUILayout.Toggle("Modify Terrain Height On Update", serializedObject.FindProperty("modifyTerrainOnUpdate").boolValue);

                GUILayout.Space(20);
                if (GUILayout.Button("Modify Terrain Height"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        ((Intersection)targets[i]).Regenerate(true, true);
                    }
                }
            }

            GUILayout.Space(20);
            GUILayout.Label("Detail Removal", EditorStyles.boldLabel);
            serializedObject.FindProperty("terrainRemoveDetails").boolValue = EditorGUILayout.Toggle("Remove Details", serializedObject.FindProperty("terrainRemoveDetails").boolValue);
            if (serializedObject.FindProperty("terrainRemoveDetails").boolValue)
            {
                serializedObject.FindProperty("terrainDetailsRadius").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Remove Detail Radius", serializedObject.FindProperty("terrainDetailsRadius").floatValue), 0, 100);
                serializedObject.FindProperty("terrainRemoveDetailsOnUpdate").boolValue = EditorGUILayout.Toggle("Remove Details On Update", serializedObject.FindProperty("terrainRemoveDetailsOnUpdate").boolValue);

                if (GUILayout.Button("Remove Details"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        ((Intersection)targets[i]).Regenerate(true, false, true);
                    }
                }
            }

            GUILayout.Space(20);
            GUILayout.Label("Tree Removal", EditorStyles.boldLabel);
            serializedObject.FindProperty("terrainRemoveTrees").boolValue = EditorGUILayout.Toggle("Remove Trees", serializedObject.FindProperty("terrainRemoveTrees").boolValue);
            if (serializedObject.FindProperty("terrainRemoveTrees").boolValue)
            {
                serializedObject.FindProperty("terrainTreesRadius").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Remove Trees Radius", serializedObject.FindProperty("terrainTreesRadius").floatValue), 0, 100);
                serializedObject.FindProperty("terrainRemoveTreesOnUpdate").boolValue = EditorGUILayout.Toggle("Remove Trees On Update", serializedObject.FindProperty("terrainRemoveTreesOnUpdate").boolValue);

                if (GUILayout.Button("Remove Trees"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        ((Intersection)targets[i]).Regenerate(true, false, false, true);
                    }
                }
            }

            if (serializedObject.FindProperty("modifyTerrainHeight").boolValue && serializedObject.FindProperty("terrainRemoveDetails").boolValue && serializedObject.FindProperty("terrainRemoveTrees").boolValue)
            {
                if (GUILayout.Button("Update Terrain And Remove Details/Trees"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        ((Intersection)targets[i]).Regenerate(true, true, true, true);
                    }
                }
            }
        }

        private void InspectMainRoads()
        {
            serializedObject.FindProperty("automaticallyGenerateMainRoads").boolValue = EditorGUILayout.Toggle(new GUIContent("Automatically Generate Main Roads", "Uses a algorithm to connect connected roads that have the same amount of lanes that arn't part of the main intersection. For example if you have two roads that both have a sidewalk consisting of 2 lanes, then the sidewalk will continue through the intersection."), serializedObject.FindProperty("automaticallyGenerateMainRoads").boolValue);

            if (serializedObject.FindProperty("mainRoads").arraySize > 0)
            {
                // Generate options
                string[] options = new string[serializedObject.FindProperty("mainRoads").arraySize];
                for (int i = 0; i < serializedObject.FindProperty("mainRoads").arraySize; i++)
                {
                    options[i] = (i + 1).ToString();
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Main Road");
                if (serializedObject.FindProperty("mainRoadTab").intValue > 0 && GUILayout.Button("←"))
                {
                    serializedObject.FindProperty("mainRoadTab").intValue -= 1;
                }

                serializedObject.FindProperty("mainRoadTab").intValue = EditorGUILayout.Popup(serializedObject.FindProperty("mainRoadTab").intValue, options);

                if (serializedObject.FindProperty("mainRoadTab").intValue < serializedObject.FindProperty("mainRoads").arraySize - 1 && GUILayout.Button("→"))
                {
                    serializedObject.FindProperty("mainRoadTab").intValue += 1;
                }
                EditorGUILayout.EndHorizontal();

                // Prevent selecting a main road that doesn't excist
                if (serializedObject.FindProperty("mainRoadTab").intValue > serializedObject.FindProperty("mainRoads").arraySize - 1)
                {
                    serializedObject.FindProperty("mainRoadTab").intValue = 0;
                }

                // Only show settings for non-generated main roads
                if (serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("generated").boolValue)
                {
                    GUILayout.Label("You cannot edit a generated main road");
                }
                else
                {
                    // Settings
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("material").objectReferenceValue = EditorGUILayout.ObjectField("Material", serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("material").objectReferenceValue, typeof(Material), false);
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("physicMaterial").objectReferenceValue = EditorGUILayout.ObjectField("Physic Material", serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("physicMaterial").objectReferenceValue, typeof(PhysicMaterial), false);
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndex").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Start Index", "The index of the connection where the main road starts"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndex").intValue), 0, serializedObject.FindProperty("connections").arraySize - 1);
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndex").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("End Index", "The index of the connection where the main road ends"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndex").intValue), 0, serializedObject.FindProperty("connections").arraySize - 1);

                    // Prevent the main road starting and ending at the same connection
                    if (serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndex").intValue == serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndex").intValue)
                    {
                        serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndex").intValue += 1;
                        if (serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndex").intValue > serializedObject.FindProperty("connections").arraySize - 1)
                        {
                            serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndex").intValue = 0;
                        }
                    }

                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("wholeLeftRoad").boolValue = EditorGUILayout.Toggle(new GUIContent("Whole Start Road", "Should the main road connect to the entire start road?"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("wholeLeftRoad").boolValue);
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("wholeRightRoad").boolValue = EditorGUILayout.Toggle(new GUIContent("Whole End Road", "Should the main road connect to the entire end road?"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("wholeRightRoad").boolValue);

                    if (!serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("wholeLeftRoad").boolValue)
                    {
                        int startIndex = serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndex").intValue;
                        serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndexLeftRoad").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Start Index Start Road", "The index of the lane that the main road should start the connection to the start road"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndexLeftRoad").intValue), 0, serializedObject.FindProperty("connections").GetArrayElementAtIndex(startIndex).FindPropertyRelative("connectedLanes").arraySize - 1);
                        serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndexLeftRoad").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("End Index Start Road", "The index of the lane that the main road should end the connection to the start road"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndexLeftRoad").intValue), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndexLeftRoad").intValue, serializedObject.FindProperty("connections").GetArrayElementAtIndex(startIndex).FindPropertyRelative("connectedLanes").arraySize - 1);
                    }

                    if (!serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("wholeRightRoad").boolValue)
                    {
                        int endIndex = serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndex").intValue;
                        serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndexRightRoad").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Start Index End Road", "The index of the lane that the main road should start the connection to the end road"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndexRightRoad").intValue), 0, serializedObject.FindProperty("connections").GetArrayElementAtIndex(endIndex).FindPropertyRelative("connectedLanes").arraySize - 1);
                        serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndexRightRoad").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("End Index End Road", "The index of the lane that the main road should end the connection to the end road"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("endIndexRightRoad").intValue), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("startIndexRightRoad").intValue, serializedObject.FindProperty("connections").GetArrayElementAtIndex(endIndex).FindPropertyRelative("connectedLanes").arraySize - 1);
                    }

                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("yOffset").floatValue = EditorGUILayout.FloatField("Uv Z Scale", serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("yOffset").floatValue);
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("flipUvs").boolValue = EditorGUILayout.Toggle("Flip Uvs", serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("flipUvs").boolValue);
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("uvZScale").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Uv Z Scale", serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("uvZScale").floatValue), 0.01f, 10f);
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("uvZOffset").floatValue = Mathf.Clamp01(EditorGUILayout.FloatField("Uv Z Offset", serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("uvZOffset").floatValue));

                    if (GUILayout.Button("Duplicate"))
                    {
                        serializedObject.FindProperty("mainRoads").InsertArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue + 1);
                        Utility.CopyMainRoadsData(serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue + 1));
                        serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue + 1).FindPropertyRelative("generated").boolValue = false;
                        serializedObject.FindProperty("mainRoadTab").intValue += 1;
                    }
                }
            }

            if ((serializedObject.FindProperty("mainRoads").arraySize == 0 || !serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("generated").boolValue) && GUILayout.Button("Add"))
            {
                if (serializedObject.FindProperty("mainRoads").arraySize == 0)
                {
                    serializedObject.FindProperty("mainRoads").InsertArrayElementAtIndex(0);
                    Utility.CopyMainRoadsData(serializedObject.FindProperty("defaultMainRoad"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(0));
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(0).FindPropertyRelative("generated").boolValue = false;
                }
                else
                {
                    serializedObject.FindProperty("mainRoads").InsertArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue + 1);
                    serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue + 1).FindPropertyRelative("generated").boolValue = false;
                    Utility.CopyMainRoadsData(serializedObject.FindProperty("defaultMainRoad"), serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue + 1));
                    serializedObject.FindProperty("mainRoadTab").intValue += 1;
                }
            }

            if ((serializedObject.FindProperty("mainRoads").arraySize == 0 || !serializedObject.FindProperty("mainRoads").GetArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue).FindPropertyRelative("generated").boolValue) && GUILayout.Button("Remove"))
            {
                serializedObject.FindProperty("mainRoads").DeleteArrayElementAtIndex(serializedObject.FindProperty("mainRoadTab").intValue);

                if (serializedObject.FindProperty("mainRoadTab").intValue > 0)
                {
                    serializedObject.FindProperty("mainRoadTab").intValue -= 1;
                }
            }
        }
    }
}