using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

namespace RoadCreatorPro
{
    [CustomEditor(typeof(RoadCreator))]
    public class RoadEditor : PointSystemEditor
    {

        public new void OnEnable()
        {
            base.OnEnable();

            for (int i = 0; i < targets.Length; i++)
            {
                RoadCreator road = (RoadCreator)targets[i];
                road.InitializeSystem();
                road.settings = RoadCreatorSettings.GetSerializedSettings();
                road.defaultLane = new Lane();
                road.defaultTerrainInterval = new TerrainModificationInterval();
                road.UpdatePointList();
                road.iDown = false;

                if (road.startIntersection != null)
                {
                    road.startIntersection.Regenerate(true, false);
                }

                if (road.endIntersection != null)
                {
                    road.endIntersection.Regenerate(true, false);
                }
            }

            Tools.current = Tool.None;
            sDown = false;

            Undo.undoRedoPerformed += UndoRoad;
        }

        private new void OnSceneGUI()
        {
            base.OnSceneGUI();

            Event currentEvent = Event.current;
            RoadCreator roadCreator = (RoadCreator)target;

            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.I)
                {
                    roadCreator.iDown = true;
                }
            }
            else if (currentEvent.type == EventType.KeyUp && currentEvent.keyCode == KeyCode.I)
            {
                if (currentEvent.keyCode == KeyCode.I)
                {
                    roadCreator.iDown = false;
                }
            }

            // Draw selected lane
            if (roadCreator.tab == 1 && roadCreator.transform.GetChild(0).childCount > 1 && roadCreator.lanes.Count > 0)
            {
                Handles.color = roadCreator.settings.FindProperty("selectedObjectColour").colorValue;
                Handles.ArrowHandleCap(0, roadCreator.lanes[roadCreator.lanesTab].centerPoint + new Vector3(0, roadCreator.settings.FindProperty("selectedObjectArrowSize").floatValue * 1.15f, 0), Quaternion.Euler(90, 0, 0), roadCreator.settings.FindProperty("selectedObjectArrowSize").floatValue, EventType.Repaint);
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRoad;
            Tools.current = Tool.Move;
        }

        private void UndoRoad()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                RoadCreator road = ((RoadCreator)targets[i]);
                road.UpdatePointList();
                lastPointIndex = -1;
                currentMovingPointIndex = -1;
                road.Regenerate(false);
            }

            // Update all roads
            HashSet<RoadCreator> updatedRoads = new HashSet<RoadCreator>();

            // Update at start point
            Transform points = ((RoadCreator)target).transform.GetChild(0);

            if (points.childCount > 0)
            {
                Collider[] colliders = Physics.OverlapBox(points.GetChild(0).transform.position, new Vector3(10, 5, 10), Quaternion.identity, 1 << LayerMask.NameToLayer("Road"));
                for (int i = 0; i < colliders.Length; i++)
                {
                    RoadCreator road = colliders[i].transform.parent.parent.GetComponent<RoadCreator>();
                    if (!updatedRoads.Contains(road))
                    {
                        road.Regenerate(false);
                    }
                    else
                    {
                        updatedRoads.Add(road);
                    }
                }

                // Update at end point
                colliders = Physics.OverlapBox(points.GetChild(points.childCount - 1).transform.position, new Vector3(10, 5, 10), Quaternion.identity, 1 << LayerMask.NameToLayer("Road"));
                for (int i = 0; i < colliders.Length; i++)
                {
                    RoadCreator road = colliders[i].transform.parent.parent.GetComponent<RoadCreator>();
                    if (!updatedRoads.Contains(road))
                    {
                        road.Regenerate(false);
                    }
                    else
                    {
                        updatedRoads.Add(road);
                    }
                }
            }
        }

        #region Inspector

        public override void OnInspectorGUI()
        {
            if (PrefabStageUtility.GetPrefabStage(((RoadCreator)target).gameObject) != null)
            {
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(((RoadCreator)target).gameObject) != PrefabAssetType.NotAPrefab)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            Utility.DisplayControls(ref ((RoadCreator)target).controlsFolded, true);
            serializedObject.FindProperty("tab").intValue = GUILayout.Toolbar(serializedObject.FindProperty("tab").intValue, new string[] { "General", "Lanes", "Terrain", "Prefabs" });

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
                InspectLanes();
            }
            else if (serializedObject.FindProperty("tab").intValue == 2)
            {
                InspectTerrain();
            }
            else
            {
                InspectPrefabs();
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Snap Points To Terrain"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ((RoadCreator)targets[i]).SnapPointsToTerrain();
                    ((RoadCreator)targets[i]).Regenerate(false);
                }
            }

            if (GUILayout.Button("Update Road"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ((RoadCreator)targets[i]).UpdateConnectedLanes();
                    ((RoadCreator)targets[i]).Regenerate(false);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                for (int i = 0; i < targets.Length; i++)
                {
                    ((RoadCreator)targets[i]).UpdateConnectedLanes();
                    ((RoadCreator)targets[i]).Regenerate(false);
                }
            }
        }

        private void InspectGeneral()
        {
            // Save and load
            EditorGUILayout.BeginHorizontal();
            serializedObject.FindProperty("roadPreset").objectReferenceValue = EditorGUILayout.ObjectField("Road Preset", serializedObject.FindProperty("roadPreset").objectReferenceValue, typeof(RoadPreset), false);

            if (GUILayout.Button("Load"))
            {
                if (serializedObject.FindProperty("roadPreset").objectReferenceValue != null)
                {
                    RoadPreset roadPreset = (RoadPreset)serializedObject.FindProperty("roadPreset").objectReferenceValue;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    Utility.CopyRoadPresetToRoadData(roadPreset, serializedObject);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    for (int i = 0; i < targets.Length; i++)
                    {
                        Utility.CopyRoadPresetToRoadPrefabData(roadPreset, (RoadCreator)targets[i]);
                    }

                    ((RoadCreator)target).Regenerate(false);
                }
            }

            if (GUILayout.Button("Save"))
            {
                if (serializedObject.FindProperty("roadPreset").objectReferenceValue != null)
                {
                    RoadPreset roadPreset = (RoadPreset)serializedObject.FindProperty("roadPreset").objectReferenceValue;
                    Utility.CopyRoadDataToRoadPreset(serializedObject, roadPreset);
                    EditorUtility.SetDirty(serializedObject.FindProperty("roadPreset").objectReferenceValue);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.FindProperty("baseYOffset").floatValue = EditorGUILayout.FloatField("Base Y Offset", serializedObject.FindProperty("baseYOffset").floatValue);
            serializedObject.FindProperty("detailLevel").floatValue = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Detail Level", "Determines how accurately the road follows the curve by changing the amount of vertices generated."), serializedObject.FindProperty("detailLevel").floatValue), 0.01f, 200);
            serializedObject.FindProperty("generateColliders").boolValue = EditorGUILayout.Toggle("Generate Colliders", serializedObject.FindProperty("generateColliders").boolValue);

            RoadCreator roadCreator = (RoadCreator)target;
            if (roadCreator.transform.GetChild(0).childCount > 3 && roadCreator.startIntersection == null && roadCreator.endIntersection == null)
            {
                serializedObject.FindProperty("cyclic").boolValue = EditorGUILayout.Toggle(new GUIContent("Cyclic", "Determines if the roads connects with itself"), serializedObject.FindProperty("cyclic").boolValue);
            }

            if (!serializedObject.FindProperty("cyclic").boolValue)
            {
                serializedObject.FindProperty("connectToIntersections").boolValue = EditorGUILayout.Toggle("Connect To Intersections", serializedObject.FindProperty("connectToIntersections").boolValue);
            }

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

            GUILayout.Space(20);
            if (GUILayout.Button("Reset Road"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Transform targetTransform = ((RoadCreator)targets[i]).transform;
                    // Remove points
                    for (int j = targetTransform.GetChild(0).childCount - 1; j >= 0; j--)
                    {
                        DestroyImmediate(targetTransform.GetChild(0).GetChild(0).gameObject);
                    }

                    // Remove prefab lines
                    for (int j = targetTransform.GetChild(2).childCount - 1; j >= 0; j--)
                    {
                        DestroyImmediate(targetTransform.GetChild(2).GetChild(0).gameObject);
                    }

                    targetTransform.GetComponent<RoadCreator>().lanes.Clear();
                    targetTransform.GetComponent<RoadCreator>().prefabLines.Clear();
                    targetTransform.GetComponent<RoadCreator>().startIntersection = null;
                    targetTransform.GetComponent<RoadCreator>().startIntersectionConnection = null;
                    targetTransform.GetComponent<RoadCreator>().endIntersection = null;
                    targetTransform.GetComponent<RoadCreator>().endIntersectionConnection = null;
                    targetTransform.GetComponent<RoadCreator>().Regenerate(false);
                    targetTransform.GetComponent<RoadCreator>().UpdatePointList();
                }

                lastPointIndex = -1;
                currentMovingPointIndex = -1;
            }
        }

        private void InspectLanes()
        {
            if (serializedObject.FindProperty("lanes").arraySize > 0 && ((RoadCreator)target).transform.GetChild(0).childCount > 1)
            {
                // Generate options
                string[] options = new string[serializedObject.FindProperty("lanes").arraySize];
                for (int i = 0; i < serializedObject.FindProperty("lanes").arraySize; i++)
                {
                    options[i] = (i + 1).ToString();
                }

                int tab = serializedObject.FindProperty("lanesTab").intValue;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Lane");
                if (tab > 0 && GUILayout.Button("←"))
                {
                    serializedObject.FindProperty("lanesTab").intValue -= 1;
                    // Update value as it otherwise isn't saved
                    tab = serializedObject.FindProperty("lanesTab").intValue;
                }

                serializedObject.FindProperty("lanesTab").intValue = EditorGUILayout.Popup(tab, options);

                if (tab < serializedObject.FindProperty("lanes").arraySize - 1 && GUILayout.Button("→"))
                {
                    serializedObject.FindProperty("lanesTab").intValue += 1;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (tab > 0 && GUILayout.Button("Decrease Index"))
                {
                    serializedObject.FindProperty("lanes").MoveArrayElement(tab, tab - 1);

                    if (!serializedObject.FindProperty("lanes").GetArrayElementAtIndex(tab).FindPropertyRelative("ignoreForWidthCalculation").boolValue)
                    {
                        serializedObject.FindProperty("lanes").GetArrayElementAtIndex(tab - 1).FindPropertyRelative("ignoreForWidthCalculation").boolValue = false;
                    }

                    serializedObject.FindProperty("lanesTab").intValue -= 1;
                }

                if (tab < serializedObject.FindProperty("lanes").arraySize - 1 && GUILayout.Button("Increase Index"))
                {
                    serializedObject.FindProperty("lanes").MoveArrayElement(tab, tab + 1);
                    serializedObject.FindProperty("lanesTab").intValue += 1;
                }
                EditorGUILayout.EndHorizontal();

                SerializedProperty lane = serializedObject.FindProperty("lanes").GetArrayElementAtIndex(tab);
                lane.FindPropertyRelative("wholeRoad").boolValue = EditorGUILayout.Toggle("Whole Road", lane.FindPropertyRelative("wholeRoad").boolValue);

                if (!lane.FindPropertyRelative("wholeRoad").boolValue)
                {
                    lane.FindPropertyRelative("startIndex").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Start Point Index", "Index of the point where the lane should start"), lane.FindPropertyRelative("startIndex").intValue), 0, ((RoadCreator)target).transform.GetChild(0).childCount - 2);
                    lane.FindPropertyRelative("endIndex").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("End Point Index", "Index of the point where the lane should end"), lane.FindPropertyRelative("endIndex").intValue), lane.FindPropertyRelative("startIndex").intValue, ((RoadCreator)target).transform.GetChild(0).childCount - 2);
                }

                lane.FindPropertyRelative("startPercentageOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Start Point Offset", "Amount of point offset from the start point"), lane.FindPropertyRelative("startPercentageOffset").floatValue), 0, 1);
                lane.FindPropertyRelative("endPercentageOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("End Point Offset", "Amount of point offset from the end point"), lane.FindPropertyRelative("endPercentageOffset").floatValue), 0, 1);

                // Prevent start point being after end point
                if (lane.FindPropertyRelative("startIndex").intValue == lane.FindPropertyRelative("endIndex").intValue && (!lane.FindPropertyRelative("wholeRoad").boolValue || ((RoadCreator)target).transform.GetChild(0).childCount <= 2))
                {
                    if (lane.FindPropertyRelative("startPercentageOffset").floatValue > lane.FindPropertyRelative("endPercentageOffset").floatValue)
                    {
                        lane.FindPropertyRelative("startPercentageOffset").floatValue = lane.FindPropertyRelative("endPercentageOffset").floatValue;
                    }
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("lanes").GetArrayElementAtIndex(tab).FindPropertyRelative("materials"), true);
                serializedObject.FindProperty("lanes").GetArrayElementAtIndex(tab).FindPropertyRelative("physicMaterial").objectReferenceValue = EditorGUILayout.ObjectField("Physic Material", serializedObject.FindProperty("lanes").GetArrayElementAtIndex(tab).FindPropertyRelative("physicMaterial").objectReferenceValue, typeof(PhysicMaterial), false);
                lane.FindPropertyRelative("textureTilingMultiplier").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Texture Tiling Multiplier", lane.FindPropertyRelative("textureTilingMultiplier").floatValue), 0.01f, 10);
                lane.FindPropertyRelative("constantUvWidth").boolValue = EditorGUILayout.Toggle(new GUIContent("Constant Uv Width", "If true then texture will scale to match current width, if false then the texture will match the max width and parts couold therefore be cut off"), lane.FindPropertyRelative("constantUvWidth").boolValue);
                lane.FindPropertyRelative("flipUvs").boolValue = EditorGUILayout.Toggle(new GUIContent("Flip Uvs", "If true then the texture is flipped left to right"), lane.FindPropertyRelative("flipUvs").boolValue);

                if (!lane.FindPropertyRelative("constantUvWidth").boolValue)
                {
                    lane.FindPropertyRelative("uvXMin").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Uv X Min", lane.FindPropertyRelative("uvXMin").floatValue), 0, 1);
                    lane.FindPropertyRelative("uvXMax").floatValue = Mathf.Clamp(EditorGUILayout.FloatField("Uv X Max", lane.FindPropertyRelative("uvXMax").floatValue), lane.FindPropertyRelative("uvXMin").floatValue, 1);
                }

                lane.FindPropertyRelative("mainRoadPart").boolValue = EditorGUILayout.Toggle(new GUIContent("Main Road Part", "Determines if this lane should connect to the main part of an intersection"), lane.FindPropertyRelative("mainRoadPart").boolValue);
                Utility.DisplayCurveEditor(lane.FindPropertyRelative("width"), "Width", ((RoadCreator)target).settings);
                Utility.DisplayCurveEditor(lane.FindPropertyRelative("yOffset"), "Y Offset", ((RoadCreator)target).settings);

                if (tab == serializedObject.FindProperty("lanes").arraySize - 1 || (tab < serializedObject.FindProperty("lanes").arraySize - 1 && serializedObject.FindProperty("lanes").GetArrayElementAtIndex(tab + 1).FindPropertyRelative("ignoreForWidthCalculation").boolValue))
                {
                    lane.FindPropertyRelative("ignoreForWidthCalculation").boolValue = EditorGUILayout.Toggle(new GUIContent("Ignore For Width Calculation", "Enable if you want this lane to avoid moving the rest of the lanes"), lane.FindPropertyRelative("ignoreForWidthCalculation").boolValue);
                }

                GUILayout.Space(20);
                if (GUILayout.Button("Duplicate"))
                {
                    serializedObject.FindProperty("lanes").InsertArrayElementAtIndex(serializedObject.FindProperty("lanesTab").intValue + 1);
                    Utility.CopyLaneData(serializedObject.FindProperty("lanes").GetArrayElementAtIndex(serializedObject.FindProperty("lanesTab").intValue), serializedObject.FindProperty("lanes").GetArrayElementAtIndex(serializedObject.FindProperty("lanesTab").intValue + 1));
                    serializedObject.FindProperty("lanesTab").intValue += 1;
                }

                if (GUILayout.Button("Add"))
                {
                    serializedObject.FindProperty("lanes").GetArrayElementAtIndex(tab).FindPropertyRelative("ignoreForWidthCalculation").boolValue = false;
                    serializedObject.FindProperty("lanes").InsertArrayElementAtIndex(serializedObject.FindProperty("lanesTab").intValue + 1);
                    Utility.CopyLaneData(serializedObject.FindProperty("defaultLane"), serializedObject.FindProperty("lanes").GetArrayElementAtIndex(serializedObject.FindProperty("lanesTab").intValue + 1));
                    serializedObject.FindProperty("lanesTab").intValue += 1;
                }

                if (serializedObject.FindProperty("lanes").arraySize > 1 && GUILayout.Button("Remove"))
                {
                    if (tab == serializedObject.FindProperty("lanes").arraySize - 1 && tab > 0)
                    {
                        serializedObject.FindProperty("lanesTab").intValue -= 1;
                    }

                    serializedObject.FindProperty("lanes").DeleteArrayElementAtIndex(tab);
                }
            }

            // Add lanes from curve
            GUILayout.Space(20);
            GUILayout.Label("Add Lanes From Curve", EditorStyles.boldLabel);
            serializedObject.FindProperty("laneCurve").animationCurveValue = EditorGUILayout.CurveField(new GUIContent("Lane Curve", "The shape of the added lanes from the side"), serializedObject.FindProperty("laneCurve").animationCurveValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("laneMaterials"), true);

            if (serializedObject.FindProperty("laneMaterials").arraySize > 1)
            {
                serializedObject.FindProperty("oneMaterialPerLane").boolValue = EditorGUILayout.Toggle(new GUIContent("One Material Per Lane", "If true each material is only applied to one lane instead of all"), serializedObject.FindProperty("oneMaterialPerLane").boolValue);
            }

            if (GUILayout.Button("Add Lanes"))
            {
                AddLanesFromCurve();
            }

            if (GUILayout.Button("Set Lanes"))
            {
                serializedObject.FindProperty("lanes").ClearArray();
                serializedObject.FindProperty("lanesTab").intValue = 0;
                serializedObject.ApplyModifiedProperties();
                AddLanesFromCurve();
            }
        }

        private void AddLanesFromCurve()
        {
            // Add a lane that represents each segment between two points in the curve
            AnimationCurve curve = serializedObject.FindProperty("laneCurve").animationCurveValue;

            // Calculate total distance
            float totalDistance = 0;
            float[] distances = new float[curve.keys.Length];

            for (int i = 1; i < curve.keys.Length; i++)
            {
                totalDistance += Mathf.Abs(curve.keys[i].time - curve.keys[i - 1].time);
                totalDistance += Mathf.Abs(curve.keys[i].value - curve.keys[i - 1].value);
                distances[i] = totalDistance;
            }

            int materialIndex = 0;
            for (int i = 1; i < curve.keys.Length; i++)
            {
                float width = curve.keys[i].time - curve.keys[i - 1].time;
                float height = curve.keys[i].value - curve.keys[i - 1].value;

                serializedObject.FindProperty("lanes").InsertArrayElementAtIndex(serializedObject.FindProperty("lanes").arraySize);
                Utility.CopyLaneData(serializedObject.FindProperty("defaultLane"), serializedObject.FindProperty("lanes").GetArrayElementAtIndex(serializedObject.FindProperty("lanes").arraySize - 1));
                SerializedProperty lane = serializedObject.FindProperty("lanes").GetArrayElementAtIndex(serializedObject.FindProperty("lanes").arraySize - 1);
                lane.FindPropertyRelative("width").animationCurveValue = AnimationCurve.Linear(0, width, 1, width);
                lane.FindPropertyRelative("yOffset").animationCurveValue = AnimationCurve.Linear(0, height, 1, height);

                lane.FindPropertyRelative("materials").ClearArray();

                // Add all material to all lanes
                if (serializedObject.FindProperty("laneMaterials").arraySize == 1 || !serializedObject.FindProperty("oneMaterialPerLane").boolValue)
                {
                    for (int j = 0; j < serializedObject.FindProperty("laneMaterials").arraySize; j++)
                    {
                        lane.FindPropertyRelative("materials").InsertArrayElementAtIndex(j);
                        lane.FindPropertyRelative("materials").GetArrayElementAtIndex(j).objectReferenceValue = serializedObject.FindProperty("laneMaterials").GetArrayElementAtIndex(j).objectReferenceValue;
                    }
                }
                // Add one material to each lane
                else
                {
                    lane.FindPropertyRelative("materials").InsertArrayElementAtIndex(0);
                    lane.FindPropertyRelative("materials").GetArrayElementAtIndex(0).objectReferenceValue = serializedObject.FindProperty("laneMaterials").GetArrayElementAtIndex(materialIndex).objectReferenceValue;
                }

                // Only modify UV if material is spread out across multiple lanes
                if (!serializedObject.FindProperty("oneMaterialPerLane").boolValue)
                {
                    lane.FindPropertyRelative("uvXMin").floatValue = distances[i - 1] / totalDistance;
                    lane.FindPropertyRelative("uvXMax").floatValue = distances[i] / totalDistance;
                }

                if (materialIndex < serializedObject.FindProperty("laneMaterials").arraySize - 1)
                {
                    materialIndex++;
                }
            }
        }

        private void InspectTerrain()
        {
            serializedObject.FindProperty("deformMeshToTerrain").boolValue = EditorGUILayout.Toggle("Deform Mesh To Terrain", serializedObject.FindProperty("deformMeshToTerrain").boolValue);

            if (!serializedObject.FindProperty("deformMeshToTerrain").boolValue)
            {
                GUILayout.Space(20);
                GUILayout.Label("Terrain Deformation", EditorStyles.boldLabel);
                serializedObject.FindProperty("modifyTerrainHeight").boolValue = EditorGUILayout.Toggle("Modify Terrain Height", serializedObject.FindProperty("modifyTerrainHeight").boolValue);

                if (serializedObject.FindProperty("modifyTerrainHeight").boolValue)
                {
                    serializedObject.FindProperty("terrain").objectReferenceValue = EditorGUILayout.ObjectField("Terrain", serializedObject.FindProperty("terrain").objectReferenceValue, typeof(GameObject), true);
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

                    if (serializedObject.FindProperty("terrainModificationIntervals").arraySize > 0)
                    {
                        GUILayout.Space(20);

                        // Interval selection
                        // Generate options
                        string[] options = new string[serializedObject.FindProperty("terrainModificationIntervals").arraySize];
                        for (int i = 0; i < serializedObject.FindProperty("terrainModificationIntervals").arraySize; i++)
                        {
                            options[i] = (i + 1).ToString();
                        }

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Interval");
                        if (serializedObject.FindProperty("terrainTab").intValue > 0 && GUILayout.Button("←"))
                        {
                            serializedObject.FindProperty("terrainTab").intValue -= 1;
                        }

                        serializedObject.FindProperty("terrainTab").intValue = EditorGUILayout.Popup(serializedObject.FindProperty("terrainTab").intValue, options);

                        if (serializedObject.FindProperty("terrainTab").intValue < serializedObject.FindProperty("terrainModificationIntervals").arraySize - 1 && GUILayout.Button("→"))
                        {
                            serializedObject.FindProperty("terrainTab").intValue += 1;
                        }
                        EditorGUILayout.EndHorizontal();

                        SerializedProperty interval = serializedObject.FindProperty("terrainModificationIntervals").GetArrayElementAtIndex(serializedObject.FindProperty("terrainTab").intValue);

                        interval.FindPropertyRelative("wholeRoad").boolValue = EditorGUILayout.Toggle("Whole Road", interval.FindPropertyRelative("wholeRoad").boolValue);
                        if (!serializedObject.FindProperty("terrainModificationIntervals").GetArrayElementAtIndex(serializedObject.FindProperty("terrainTab").intValue).FindPropertyRelative("wholeRoad").boolValue)
                        {
                            interval.FindPropertyRelative("startIndex").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Start Point Index", "Index of the point where the lane should start"), interval.FindPropertyRelative("startIndex").intValue), 0, ((RoadCreator)target).transform.GetChild(0).childCount - 2);
                            interval.FindPropertyRelative("endIndex").intValue = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("End Point Index", "Index of the point where the lane should end"), interval.FindPropertyRelative("endIndex").intValue), interval.FindPropertyRelative("startIndex").intValue, ((RoadCreator)target).transform.GetChild(0).childCount - 2);
                        }

                        interval.FindPropertyRelative("startPercentageOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Start Point Offset", "Amount of point offset from the start point"), interval.FindPropertyRelative("startPercentageOffset").floatValue), 0, 1);
                        float minEndOffset = 0;
                        if ((!interval.FindPropertyRelative("wholeRoad").boolValue || ((RoadCreator)target).transform.GetChild(0).childCount <= 2) && interval.FindPropertyRelative("startIndex").intValue == interval.FindPropertyRelative("endIndex").intValue)
                        {
                            minEndOffset = interval.FindPropertyRelative("startPercentageOffset").floatValue;
                        }

                        interval.FindPropertyRelative("endPercentageOffset").floatValue = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("End Point Offset", "Amount of point offset from the end point"), interval.FindPropertyRelative("endPercentageOffset").floatValue), minEndOffset, 1);


                        if (GUILayout.Button("Duplicate"))
                        {
                            serializedObject.FindProperty("terrainModificationIntervals").InsertArrayElementAtIndex(serializedObject.FindProperty("terrainTab").intValue + 1);
                            Utility.CopyTerrainIntervalData(serializedObject.FindProperty("terrainModificationIntervals").GetArrayElementAtIndex(serializedObject.FindProperty("terrainTab").intValue), serializedObject.FindProperty("terrainModificationIntervals").GetArrayElementAtIndex(serializedObject.FindProperty("terrainTab").intValue + 1));
                            serializedObject.FindProperty("terrainTab").intValue += 1;
                        }

                        if (GUILayout.Button("Add"))
                        {
                            serializedObject.FindProperty("terrainModificationIntervals").InsertArrayElementAtIndex(serializedObject.FindProperty("terrainTab").intValue + 1);
                            Utility.CopyTerrainIntervalData(serializedObject.FindProperty("defaultTerrainInterval"), serializedObject.FindProperty("terrainModificationIntervals").GetArrayElementAtIndex(serializedObject.FindProperty("terrainTab").intValue + 1));
                            serializedObject.FindProperty("terrainTab").intValue += 1;
                        }

                        if (serializedObject.FindProperty("terrainModificationIntervals").arraySize > 1 && GUILayout.Button("Remove"))
                        {
                            if (serializedObject.FindProperty("terrainTab").intValue == serializedObject.FindProperty("terrainModificationIntervals").arraySize - 1 && serializedObject.FindProperty("terrainTab").intValue > 0)
                            {
                                serializedObject.FindProperty("terrainTab").intValue -= 1;
                            }

                            serializedObject.FindProperty("terrainModificationIntervals").DeleteArrayElementAtIndex(serializedObject.FindProperty("terrainTab").intValue);
                        }

                        GUILayout.Space(20);
                        if (GUILayout.Button("Modify Terrain Height"))
                        {
                            for (int i = 0; i < targets.Length; i++)
                            {
                                ((RoadCreator)targets[i]).Regenerate(true);
                            }
                        }
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
                        ((RoadCreator)targets[i]).Regenerate(false, true);
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
                        ((RoadCreator)targets[i]).Regenerate(false, false, true);
                    }
                }
            }

            if (serializedObject.FindProperty("modifyTerrainHeight").boolValue && serializedObject.FindProperty("terrainRemoveDetails").boolValue && serializedObject.FindProperty("terrainRemoveTrees").boolValue && !serializedObject.FindProperty("deformMeshToTerrain").boolValue)
            {
                if (GUILayout.Button("Update Terrain And Remove Details/Trees"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        ((RoadCreator)targets[i]).Regenerate(true, true, true);
                    }
                }
            }
        }

        private void InspectPrefabs()
        {
            RoadCreator road = (RoadCreator)target;
            // Generate options
            string[] options = new string[road.prefabLines.Count];
            for (int i = 0; i < road.prefabLines.Count; i++)
            {
                options[i] = (i + 1).ToString();
            }

            if (road.transform.GetChild(0).childCount > 1)
            {
                if (road.prefabLines.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Prefab Line");
                    if (road.prefabsTab > 0 && GUILayout.Button("←"))
                    {
                        road.prefabsTab -= 1;
                    }

                    road.prefabsTab = EditorGUILayout.Popup(road.prefabsTab, options);

                    if (road.prefabsTab < road.prefabLines.Count - 1 && GUILayout.Button("→"))
                    {
                        road.prefabsTab += 1;
                    }
                    EditorGUILayout.EndHorizontal();

                    PrefabLineCreator prefabLine = ((RoadCreator)target).transform.GetChild(2).GetChild(road.prefabsTab).GetComponent<PrefabLineCreator>();
                    DisplayCurveEditor(prefabLine, "Offset", ((RoadCreator)target).settings);

                    prefabLine.wholeRoad = EditorGUILayout.Toggle("Whole Road", prefabLine.wholeRoad);

                    if (!prefabLine.wholeRoad)
                    {
                        prefabLine.startIndex = Mathf.Clamp(EditorGUILayout.IntField("Start Index", prefabLine.startIndex), 0, ((RoadCreator)target).transform.GetChild(0).childCount - 2);
                        prefabLine.endIndex = Mathf.Clamp(EditorGUILayout.IntField("End Index", prefabLine.endIndex), prefabLine.startIndex, ((RoadCreator)target).transform.GetChild(0).childCount - 2);
                    }

                    prefabLine.startOffsetPercentage = Mathf.Clamp(EditorGUILayout.FloatField("Start Percentage Offset", prefabLine.startOffsetPercentage), 0, 1);

                    float minEndOffset = 0;
                    if (prefabLine.startIndex == prefabLine.endIndex && (prefabLine.wholeRoad || prefabLine.transform.GetChild(0).childCount <= 2))
                    {
                        minEndOffset = prefabLine.startOffsetPercentage;
                    }

                    prefabLine.endOffsetPercentage = Mathf.Clamp(EditorGUILayout.FloatField("End Percentage Offset", prefabLine.endOffsetPercentage), minEndOffset, 1);

                    if (GUILayout.Button("Configure"))
                    {
                        Selection.activeGameObject = road.transform.GetChild(2).GetChild(road.prefabsTab).gameObject;
                    }
                }

                if (road.prefabLines.Count > 0 && GUILayout.Button("Duplicate"))
                {
                    GameObject prefabLine = new GameObject("Prefab Line");
                    prefabLine.transform.SetParent(road.transform.GetChild(2), false);
                    int originalIndex = road.prefabsTab;

                    if (road.prefabLines.Count > 0)
                    {
                        prefabLine.transform.SetSiblingIndex(road.prefabsTab + 1);
                        road.prefabsTab += 1;
                    }

                    prefabLine.AddComponent<PrefabLineCreator>();
                    prefabLine.GetComponent<PrefabLineCreator>().settings = road.settings;
                    prefabLine.GetComponent<PrefabLineCreator>().InitializeSystem();
                    prefabLine.transform.hideFlags = HideFlags.NotEditable;
                    prefabLine.GetComponent<PrefabLineCreator>().controlled = true;
                    road.prefabLines.Add(prefabLine.GetComponent<PrefabLineCreator>());
                    Utility.CopyPrefabData(road.prefabLines[originalIndex], prefabLine.GetComponent<PrefabLineCreator>());
                }

                if (GUILayout.Button("Add"))
                {
                    GameObject prefabLine = new GameObject("Prefab Line");
                    prefabLine.transform.SetParent(road.transform.GetChild(2), false);

                    if (road.prefabLines.Count > 0)
                    {
                        prefabLine.transform.SetSiblingIndex(road.prefabsTab + 1);
                        road.prefabsTab += 1;
                    }

                    prefabLine.AddComponent<PrefabLineCreator>();
                    prefabLine.GetComponent<PrefabLineCreator>().settings = road.settings;
                    prefabLine.GetComponent<PrefabLineCreator>().InitializeSystem();
                    prefabLine.transform.hideFlags = HideFlags.NotEditable;
                    prefabLine.GetComponent<PrefabLineCreator>().controlled = true;
                    road.prefabLines.Add(prefabLine.GetComponent<PrefabLineCreator>());
                }

                if (road.prefabLines.Count > 0 && GUILayout.Button("Remove"))
                {
                    road.prefabLines.RemoveAt(road.prefabsTab);
                    DestroyImmediate(road.transform.GetChild(2).GetChild(road.prefabsTab).gameObject);

                    if (road.prefabsTab > 0)
                    {
                        road.prefabsTab -= 1;
                    }
                }
            }
        }

        public static void DisplayCurveEditor(PrefabLineCreator prefabLine, string name, SerializedObject settings)
        {
            prefabLine.offsetCurve = EditorGUILayout.CurveField(name, prefabLine.offsetCurve);

            if (settings.FindProperty("exposeCurveKeysToEditor").boolValue)
            {
                Keyframe[] keyFrames = prefabLine.offsetCurve.keys;
                for (int i = 0; i < prefabLine.offsetCurve.length; i++)
                {
                    keyFrames[i].value = EditorGUILayout.FloatField("\tKey #" + (i + 1) + " Value", keyFrames[i].value);
                }

                prefabLine.offsetCurve = new AnimationCurve(keyFrames);
            }
        }

        #endregion
    }
}