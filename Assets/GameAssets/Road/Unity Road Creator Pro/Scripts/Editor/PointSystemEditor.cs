using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

namespace RoadCreatorPro
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PointSystemCreator))]
    public class PointSystemEditor : Editor
    {
        protected int lastPointIndex = -1; // Including control points
        protected int currentMovingPointIndex = -1; // Not including control points
        protected bool sDown = false;
        private bool aDown = false;
        private bool pDown = false;
        private bool gDown = false;
        
        protected void OnEnable()
        {
            aDown = false;
            pDown = false;
            gDown = false;
        }

        public void OnSceneGUI()
        {
            Event currentEvent = Event.current;
            PointSystemCreator pointSystem = (PointSystemCreator)target;

            if (PrefabStageUtility.GetPrefabStage(pointSystem.gameObject) != null)
            {
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(pointSystem) != PrefabAssetType.NotAPrefab)
            {
                return;
            }

            RoadCreator road = pointSystem.GetComponent<RoadCreator>();
            PrefabLineCreator prefabLine = pointSystem.GetComponent<PrefabLineCreator>();

            if (prefabLine == null || !prefabLine.controlled)
            {
                GetCurrentPointIndex(pointSystem);

                if (currentEvent.type == EventType.Layout)
                {
                    if (currentEvent.shift)
                    {
                        HandleUtility.AddDefaultControl(0);
                    }

                    for (int i = 0; i < pointSystem.transform.GetChild(0).childCount; i++)
                    {
                        float distance = HandleUtility.DistanceToCircle(pointSystem.transform.GetChild(0).GetChild(i).position, pointSystem.settings.FindProperty("anchorPointSize").floatValue / 2);
                        HandleUtility.AddControl(GetIdForIndex(pointSystem, i, 0), distance);

                        if (i > 0)
                        {
                            distance = HandleUtility.DistanceToCircle(pointSystem.transform.GetChild(0).GetChild(i).position + pointSystem.transform.GetChild(0).GetChild(i).GetComponent<Point>().leftLocalControlPointPosition, pointSystem.settings.FindProperty("controlPointSize").floatValue / 2);
                            HandleUtility.AddControl(GetIdForIndex(pointSystem, i, 1), distance);
                        }

                        if (i < pointSystem.transform.GetChild(0).childCount - 1)
                        {
                            distance = HandleUtility.DistanceToCircle(pointSystem.transform.GetChild(0).GetChild(i).position + pointSystem.transform.GetChild(0).GetChild(i).GetComponent<Point>().rightLocalControlPointPosition, pointSystem.settings.FindProperty("controlPointSize").floatValue / 2);
                            HandleUtility.AddControl(GetIdForIndex(pointSystem, i, 2), distance);
                        }
                    }
                }
                else if (currentEvent.type == EventType.MouseDown)
                {
                    if (currentEvent.button == 0)
                    {
                        if (sDown == true)
                        {
                            if (pointSystem.handleIds.Contains(HandleUtility.nearestControl))
                            {
                                int id = pointSystem.handleIds.IndexOf(HandleUtility.nearestControl);
                                // Only main points
                                if (id % 3 == 0)
                                {
                                    pointSystem.SplitSegment(id / 3);
                                }
                            }
                        }
                        else
                        {
                            // Move Points
                            if (currentMovingPointIndex == -1 && pointSystem.handleIds.Contains(HandleUtility.nearestControl))
                            {
                                int id = pointSystem.handleIds.IndexOf(HandleUtility.nearestControl);
                                // Only main points
                                if (id % 3 == 0)
                                {
                                    currentMovingPointIndex = id / 3;
                                }
                            }
                            else if (currentEvent.shift)
                            {
                                if (currentEvent.alt)
                                {
                                    InsertPoint(currentEvent, pointSystem);
                                }
                                else
                                {
                                    // Create Points
                                    if (currentEvent.control)
                                    {
                                        CreatePoint(currentEvent, pointSystem, true);
                                    }
                                    else
                                    {
                                        CreatePoint(currentEvent, pointSystem, false);
                                    }
                                }
                            }
                        }
                    }
                    else if (currentEvent.button == 1 && currentEvent.shift)
                    {
                        RemovePoint(currentEvent, pointSystem);
                    }
                }
                else if (currentEvent.type == EventType.MouseDrag)
                {
                    MovePoint(currentEvent, pointSystem);
                }
                else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
                {
                    if (road != null)
                    {
                        // Test for intersections
                        road.CheckForIntersections(currentMovingPointIndex);
                    }

                    currentMovingPointIndex = -1;
                    pointSystem.Regenerate(false);
                }
                else if (currentEvent.type == EventType.KeyDown)
                {
                    if (currentEvent.keyCode == KeyCode.S)
                    {
                        sDown = true;
                    }
                    else if (currentEvent.keyCode == KeyCode.A)
                    {
                        aDown = true;
                    }
                    else if (currentEvent.keyCode == KeyCode.P)
                    {
                        pDown = true;
                    }
                    else if (currentEvent.keyCode == KeyCode.G)
                    {
                        gDown = true;
                    }

                    // Fixes input sometimes not being recognized
                    SceneView.RepaintAll();
                }
                else if (currentEvent.type == EventType.KeyUp)
                {
                    if (currentEvent.keyCode == KeyCode.S)
                    {
                        sDown = false;
                    }
                    else if (currentEvent.keyCode == KeyCode.A)
                    {
                        aDown = false;
                    }
                    else if (currentEvent.keyCode == KeyCode.P)
                    {
                        pDown = false;
                    }
                    else if (currentEvent.keyCode == KeyCode.G)
                    {
                        gDown = false;
                    }

                    // Fixes input sometimes not being recognized
                    SceneView.RepaintAll();
                }

                // Needs both mouse events, repaint event etc
                Draw(currentEvent, pointSystem);

                // Prevent scaling and rotation
                if (pointSystem.transform.hasChanged)
                {
                    pointSystem.transform.hasChanged = false;
                    pointSystem.transform.localRotation = Quaternion.identity;
                    pointSystem.transform.localScale = Vector3.one;
                }
            }
        }

        protected void GetCurrentPointIndex(PointSystemCreator pointSystem)
        {
            // Prevent focus switching to over points when moving
            if (currentMovingPointIndex == -1 && GUIUtility.hotControl == 0 && pointSystem.handleIds.Contains(HandleUtility.nearestControl))
            {
                lastPointIndex = pointSystem.handleIds.IndexOf(HandleUtility.nearestControl);

                if (EditPointWindow.instance != null)
                {
                    EditPointWindow editPointWindow = (EditPointWindow)EditorWindow.GetWindow(typeof(EditPointWindow));
                    editPointWindow.lastPoint = pointSystem.transform.GetChild(0).GetChild(lastPointIndex / 3).GetComponent<Point>();
                    editPointWindow.lastPointIndex = lastPointIndex % 3;
                }
            }
        }

        protected void Draw(Event currentEvent, PointSystemCreator pointSystem)
        {
            Vector3 screenMousePosition = currentEvent.mousePosition;
            screenMousePosition.z = 0;

            RoadCreator road = pointSystem.GetComponent<RoadCreator>();
            PrefabLineCreator prefabLine = pointSystem.GetComponent<PrefabLineCreator>();

            for (int i = 0; i < pointSystem.transform.GetChild(0).childCount; i++)
            {
                #region Draw Points

                // Main points
                Vector3 screenPosition = HandleUtility.WorldToGUIPoint(pointSystem.transform.GetChild(0).GetChild(i).position);
                screenPosition.z = 0;

                if (HandleUtility.nearestControl == GetIdForIndex(pointSystem, i, 0))
                {
                    Handles.color = pointSystem.settings.FindProperty("selectedAnchorPointColour").colorValue;
                }
                else
                {
                    Handles.color = pointSystem.settings.FindProperty("anchorPointColour").colorValue;
                }

                Transform point = pointSystem.transform.GetChild(0).GetChild(i);
                Point localControlPoint = pointSystem.transform.GetChild(0).GetChild(i).GetComponent<Point>();

                Handles.CapFunction shape;
                int shapeIndex = pointSystem.settings.FindProperty("pointShape").enumValueIndex;
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

                float handleSize = pointSystem.settings.FindProperty("anchorPointSize").floatValue;
                handleSize = Mathf.Min(handleSize, HandleUtility.GetHandleSize(point.position) * handleSize);
                if (road == null || !road.cyclic || pointSystem.transform.GetChild(0).childCount <= 3 || i > 0)
                {
                    shape(GetIdForIndex(pointSystem, i, 0), point.position, Quaternion.Euler(270, 0, 0), handleSize, EventType.Repaint);
                }

                // Calculate handle rotation
                Vector3 lookDirection = localControlPoint.leftLocalControlPointPosition.normalized;
                lookDirection.y = 0;
                Quaternion handleRotation = Quaternion.LookRotation(lookDirection);

                if (Tools.pivotRotation == PivotRotation.Global)
                {
                    handleRotation = Quaternion.identity;
                }

                // Don't draw first point for cyclic road
                if (lastPointIndex == i * 3 && currentMovingPointIndex == -1 && (road == null || !road.cyclic || pointSystem.transform.GetChild(0).childCount <= 3 || i > 0))
                {
                    Undo.RecordObject(point, "Move Point");
                    EditorGUI.BeginChangeCheck();
                    point.position = Utility.DrawPositionHandle(pointSystem.settings.FindProperty("scalePointsWhenZoomed").boolValue, pointSystem.settings.FindProperty("anchorPointSize").floatValue, point.position + Vector3.up * pointSystem.settings.FindProperty("anchorPointSize").floatValue, handleRotation) - Vector3.up * pointSystem.settings.FindProperty("anchorPointSize").floatValue;

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (currentEvent.control && road != null)
                        {
                            Utility.SnapPoint(road.roadPoints, point.GetComponent<Point>(), pointSystem.settings, pointSystem.transform.GetChild(0).GetChild(lastPointIndex / 3).GetComponent<Point>());
                        }

                        pointSystem.Regenerate(false);
                    }
                }

                // Control points
                Handles.color = pointSystem.settings.FindProperty("controlPointColour").colorValue;

                if (i > 0)
                {
                    handleSize = pointSystem.settings.FindProperty("controlPointSize").floatValue;
                    handleSize = Mathf.Min(handleSize, HandleUtility.GetHandleSize(point.position + localControlPoint.leftLocalControlPointPosition) * handleSize);
                    shape(GetIdForIndex(pointSystem, i, 1), point.position + localControlPoint.leftLocalControlPointPosition, Quaternion.Euler(270, 0, 0), handleSize, EventType.Repaint);

                    if (lastPointIndex == i * 3 + 1 && currentMovingPointIndex == -1)
                    {
                        Undo.RecordObject(localControlPoint, "Move Point");
                        EditorGUI.BeginChangeCheck();
                        localControlPoint.leftLocalControlPointPosition = Utility.DrawPositionHandle(pointSystem.settings.FindProperty("scalePointsWhenZoomed").boolValue, pointSystem.settings.FindProperty("controlPointSize").floatValue, point.position + localControlPoint.leftLocalControlPointPosition + Vector3.up * pointSystem.settings.FindProperty("anchorPointSize").floatValue, handleRotation) - point.position - Vector3.up * pointSystem.settings.FindProperty("anchorPointSize").floatValue;

                        if (EditorGUI.EndChangeCheck())
                        {
                            // P = lock y-position
                            if (pDown)
                            {
                                localControlPoint.leftLocalControlPointPosition.y = 0;
                            }

                            // Change corresponding control point
                            if (!aDown)
                            {
                                float distance = localControlPoint.rightLocalControlPointPosition.magnitude;
                                localControlPoint.rightLocalControlPointPosition = (-localControlPoint.leftLocalControlPointPosition).normalized * distance;

                                // Change next point in cyclic road
                                RoadCreator roadCreator = road;
                                if (roadCreator != null && roadCreator.cyclic && i == roadCreator.transform.GetChild(0).childCount - 1)
                                {
                                    distance = roadCreator.transform.GetChild(0).GetChild(0).GetComponent<Point>().rightLocalControlPointPosition.magnitude;
                                    roadCreator.transform.GetChild(0).GetChild(0).GetComponent<Point>().rightLocalControlPointPosition = (-localControlPoint.leftLocalControlPointPosition).normalized * distance;
                                }
                            }

                            pointSystem.Regenerate(false);
                        }
                    }
                }

                if (i < pointSystem.transform.GetChild(0).childCount - 1)
                {
                    handleSize = pointSystem.settings.FindProperty("controlPointSize").floatValue;
                    handleSize = Mathf.Min(handleSize, HandleUtility.GetHandleSize(point.position + localControlPoint.rightLocalControlPointPosition) * handleSize);
                    shape(GetIdForIndex(pointSystem, i, 2), point.position + localControlPoint.rightLocalControlPointPosition, Quaternion.Euler(270, 0, 0), handleSize, EventType.Repaint);

                    if (lastPointIndex == i * 3 + 2 && currentMovingPointIndex == -1)
                    {
                        Undo.RecordObject(localControlPoint, "Move Point");
                        EditorGUI.BeginChangeCheck();
                        localControlPoint.rightLocalControlPointPosition = Utility.DrawPositionHandle(pointSystem.settings.FindProperty("scalePointsWhenZoomed").boolValue, pointSystem.settings.FindProperty("controlPointSize").floatValue, point.position + localControlPoint.rightLocalControlPointPosition + Vector3.up * pointSystem.settings.FindProperty("anchorPointSize").floatValue, handleRotation) - point.position - Vector3.up * pointSystem.settings.FindProperty("anchorPointSize").floatValue;

                        if (EditorGUI.EndChangeCheck())
                        {
                            // P = lock y-position
                            if (pDown)
                            {
                                localControlPoint.rightLocalControlPointPosition.y = 0;
                            }

                            // Change corresponding control point
                            if (!aDown)
                            {
                                float distance = localControlPoint.leftLocalControlPointPosition.magnitude;
                                localControlPoint.leftLocalControlPointPosition = (-localControlPoint.rightLocalControlPointPosition).normalized * distance;

                                // Change next point in cyclic road
                                RoadCreator roadCreator = road;
                                if (roadCreator != null && roadCreator.cyclic && i == 0)
                                {
                                    distance = roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetComponent<Point>().leftLocalControlPointPosition.magnitude;
                                    roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetComponent<Point>().leftLocalControlPointPosition = (-localControlPoint.rightLocalControlPointPosition).normalized * distance;
                                }
                            }

                            pointSystem.Regenerate(false);
                        }
                    }
                }

                #endregion

                #region Draw Lines
                Handles.color = Color.white;

                if (i > 0)
                {
                    Handles.DrawLine(point.position, point.position + localControlPoint.leftLocalControlPointPosition);
                }

                if (i < pointSystem.transform.GetChild(0).childCount - 1)
                {
                    Handles.DrawLine(point.position, point.position + localControlPoint.rightLocalControlPointPosition);
                }

                if (i < pointSystem.transform.GetChild(0).childCount - 1)
                {
                    Handles.DrawBezier(point.transform.position, pointSystem.transform.GetChild(0).GetChild(i + 1).position, point.transform.position + localControlPoint.rightLocalControlPointPosition, pointSystem.transform.GetChild(0).GetChild(i + 1).position + pointSystem.transform.GetChild(0).GetChild(i + 1).GetComponent<Point>().leftLocalControlPointPosition, Color.black, null, 3f);
                }

                #endregion
            }

            #region Draw Guidelines
            if (road != null)
            {
                for (int i = 0; i < road.roadPoints.Length; i++)
                {
                    if (Vector3.Distance(road.roadPoints[i].transform.position, Utility.GetMousePosition(false, true)) < pointSystem.settings.FindProperty("roadGuidelinesRenderDistance").floatValue)
                    {
                        if (pointSystem.settings.FindProperty("roadGuidelinesLength").floatValue > 0 && (lastPointIndex % 3 != 0 || (pointSystem.transform.GetChild(0).childCount > 1 && road.roadPoints[i] != pointSystem.transform.GetChild(0).GetChild(lastPointIndex / 3).GetComponent<Point>())))
                        {
                            Handles.color = pointSystem.settings.FindProperty("roadGuidelinesColour").colorValue;
                            Handles.DrawLine(road.roadPoints[i].transform.position + road.roadPoints[i].startGuidelinePoint, road.roadPoints[i].transform.position + road.roadPoints[i].endGuidelinePoint);
                            Vector3 forward = road.roadPoints[i].endGuidelinePoint - road.roadPoints[i].startGuidelinePoint;
                            Vector3 left = new Vector3(-forward.z, 0, forward.x).normalized;
                            Handles.DrawLine(road.roadPoints[i].transform.position + road.roadPoints[i].startGuidelinePoint - left, road.roadPoints[i].transform.position + road.roadPoints[i].startGuidelinePoint + left);
                            Handles.DrawLine(road.roadPoints[i].transform.position + road.roadPoints[i].endGuidelinePoint - left, road.roadPoints[i].transform.position + road.roadPoints[i].endGuidelinePoint + left);
                        }
                    }
                }
            }

            #endregion

            SceneView.RepaintAll();
        }

        private void MovePoint(Event currentEvent, PointSystemCreator pointSystem)
        {
            RoadCreator road = pointSystem.GetComponent<RoadCreator>();

            if (currentMovingPointIndex != -1)
            {
                Undo.RecordObject(pointSystem.transform.GetChild(0).GetChild(currentMovingPointIndex), "Move Point");
                pointSystem.transform.GetChild(0).GetChild(currentMovingPointIndex).position = Utility.GetMousePosition(false, true);

                if (currentEvent.control && road != null)
                {
                    Utility.SnapPoint(road.roadPoints, pointSystem.transform.GetChild(0).GetChild(currentMovingPointIndex).GetComponent<Point>(), pointSystem.settings, pointSystem.transform.GetChild(0).GetChild(currentMovingPointIndex).GetComponent<Point>());
                }
            }
        }

        private void CreatePoint(Event currentEvent, PointSystemCreator pointSystem, bool start)
        {
            // Prevent adding more points to connected road
            if (pointSystem.GetComponent<RoadCreator>() != null && ((start && pointSystem.GetComponent<RoadCreator>().startIntersection != null) || (!start && pointSystem.GetComponent<RoadCreator>().endIntersection != null)))
            {
                Debug.Log("Can not continue a road in the direction that it is attached to an intersection");
                return;
            }

            // Prevent adding points to cyclic road
            if (pointSystem.GetComponent<RoadCreator>() != null && pointSystem.GetComponent<RoadCreator>().cyclic)
            {
                Debug.Log("Can not continue a cyclic road");
                return;
            }

            GameObject point = new GameObject("Point");

            if (pointSystem.GetComponent<RoadCreator>() != null)
            {
                point.transform.position = Utility.GetMousePosition(false, false);
            }
            else
            {
                point.transform.position = Utility.GetMousePosition(false, true);
            }

            point.AddComponent<Point>();
            Undo.RegisterCreatedObjectUndo(point, "Create Point");
            point.transform.SetParent(pointSystem.transform.GetChild(0));
            point.hideFlags = HideFlags.NotEditable;

            if (start)
            {
                point.transform.SetAsFirstSibling();

                // Set control points
                if (pointSystem.transform.GetChild(0).childCount > 1)
                {
                    Point nextPoint = pointSystem.transform.GetChild(0).GetChild(1).GetComponent<Point>();

                    // G = Uncurved/straight segment
                    if (gDown)
                    {
                        point.GetComponent<Point>().rightLocalControlPointPosition = Vector3.Lerp(point.transform.position, nextPoint.transform.position, 0.25f) - point.transform.position;
                        nextPoint.leftLocalControlPointPosition = (point.transform.position - nextPoint.transform.position).normalized * 3;
                    }
                    else
                    {
                        Vector3 direction;
                        Vector3 offset;
                        float distance;

                        if (pointSystem.transform.GetChild(0).childCount == 2)
                        {
                            direction = Vector3.zero;
                            offset = point.transform.position - nextPoint.transform.position;
                            direction += offset.normalized;
                            distance = offset.magnitude;
                            direction.Normalize();

                            nextPoint.leftLocalControlPointPosition = direction * distance * 0.45f;
                        }
                        else
                        {
                            direction = Vector3.zero;
                            offset = nextPoint.rightLocalControlPointPosition;
                            direction -= offset.normalized;
                            distance = (point.transform.position - nextPoint.transform.position).magnitude;
                            direction.Normalize();

                            nextPoint.leftLocalControlPointPosition = direction * distance * 0.45f;
                        }

                        direction = Vector3.zero;
                        offset = nextPoint.transform.position - point.transform.position + nextPoint.leftLocalControlPointPosition;
                        direction += offset.normalized;
                        distance = (nextPoint.transform.position - point.transform.position).magnitude;
                        direction.Normalize();

                        point.GetComponent<Point>().rightLocalControlPointPosition = direction * distance * 0.45f;
                    }

                    point.GetComponent<Point>().leftLocalControlPointPosition = -point.GetComponent<Point>().rightLocalControlPointPosition;
                }
                else
                {
                    point.GetComponent<Point>().rightLocalControlPointPosition = Vector3.left;
                    point.GetComponent<Point>().leftLocalControlPointPosition = -point.GetComponent<Point>().rightLocalControlPointPosition;
                }

                if (pointSystem.GetComponent<RoadCreator>() != null)
                {
                    // Change lane positions so that they are in the same place as before
                    for (int i = 0; i < pointSystem.GetComponent<RoadCreator>().lanes.Count; i++)
                    {
                        if (pointSystem.GetComponent<RoadCreator>().lanes[i].startIndex == 0)
                        {
                            Undo.RecordObject(pointSystem.GetComponent<RoadCreator>(), "Create Point");
                            pointSystem.GetComponent<RoadCreator>().lanes[i].startIndex += 1;
                            pointSystem.GetComponent<RoadCreator>().lanes[i].endIndex += 1;
                        }
                    }

                    // Change prefab lines so that they are in the same place as before
                    for (int i = 0; i < pointSystem.GetComponent<RoadCreator>().prefabLines.Count; i++)
                    {
                        if (pointSystem.GetComponent<RoadCreator>().prefabLines[i].startIndex == 0)
                        {
                            Undo.RecordObject(pointSystem.GetComponent<RoadCreator>().prefabLines[i], "Create Point");
                            pointSystem.GetComponent<RoadCreator>().prefabLines[i].startIndex += 1;
                            pointSystem.GetComponent<RoadCreator>().prefabLines[i].endIndex += 1;
                        }
                    }
                }
            }
            else
            {
                // Set control points
                if (pointSystem.transform.GetChild(0).childCount > 1)
                {
                    Point lastPoint = pointSystem.transform.GetChild(0).GetChild(pointSystem.transform.GetChild(0).childCount - 2).GetComponent<Point>();

                    // G = Uncurved/straight segment
                    if (gDown)
                    {
                        point.GetComponent<Point>().leftLocalControlPointPosition = Vector3.Lerp(point.transform.position, lastPoint.transform.position, 0.25f) - point.transform.position;
                        lastPoint.rightLocalControlPointPosition = (point.transform.position - lastPoint.transform.position).normalized * 3;
                    }
                    else
                    {
                        Vector3 direction;
                        Vector3 offset;
                        float distance;

                        if (pointSystem.transform.GetChild(0).childCount == 2)
                        {
                            direction = Vector3.zero;
                            offset = point.transform.position - lastPoint.transform.position;
                            direction += offset.normalized;
                            distance = offset.magnitude;
                            direction.Normalize();

                            lastPoint.rightLocalControlPointPosition = direction * distance * 0.45f;
                        }
                        else
                        {
                            direction = Vector3.zero;
                            offset = lastPoint.leftLocalControlPointPosition;
                            direction -= offset.normalized;
                            distance = (point.transform.position - lastPoint.transform.position).magnitude;
                            direction.Normalize();

                            lastPoint.rightLocalControlPointPosition = direction * distance * 0.45f;
                        }

                        direction = Vector3.zero;
                        offset = lastPoint.transform.position - point.transform.position + lastPoint.rightLocalControlPointPosition;
                        direction += offset.normalized;
                        distance = (lastPoint.transform.position - point.transform.position).magnitude;
                        direction.Normalize();

                        point.GetComponent<Point>().leftLocalControlPointPosition = direction * distance * 0.45f;
                    }

                    point.GetComponent<Point>().rightLocalControlPointPosition = -point.GetComponent<Point>().leftLocalControlPointPosition;
                }
                else
                {
                    point.GetComponent<Point>().rightLocalControlPointPosition = Vector3.left;
                    point.GetComponent<Point>().leftLocalControlPointPosition = -point.GetComponent<Point>().rightLocalControlPointPosition;
                }
            }

            if (pointSystem.GetComponent<RoadCreator>() != null)
            {
                if (currentEvent.control)
                {
                    Utility.SnapPoint(pointSystem.GetComponent<RoadCreator>().roadPoints, point.GetComponent<Point>(), pointSystem.settings, null);
                }

                // Make sure intersections are attached to correct points
                pointSystem.GetComponent<RoadCreator>().UpdateConnectedLanes();
            }

            pointSystem.Regenerate(false);

            if (pointSystem.GetComponent<RoadCreator>() != null)
            {
                pointSystem.GetComponent<RoadCreator>().UpdatePointList();
            }
        }

        private void InsertPoint(Event currentEvent, PointSystemCreator pointSystem)
        {
            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            Vector3 mousePosition = Utility.GetMousePosition(false, true);

            for (int i = 0; i < pointSystem.transform.GetChild(0).childCount - 1; i++)
            {
                float distance = HandleUtility.DistancePointBezier(mousePosition, pointSystem.transform.GetChild(0).GetChild(i).position, pointSystem.transform.GetChild(0).GetChild(i + 1).position, pointSystem.transform.GetChild(0).GetChild(i).position + pointSystem.transform.GetChild(0).GetChild(i).GetComponent<Point>().rightLocalControlPointPosition, pointSystem.transform.GetChild(0).GetChild(i + 1).position + pointSystem.transform.GetChild(0).GetChild(i + 1).GetComponent<Point>().leftLocalControlPointPosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            Vector3[] positions = Utility.ClosestPointOnLineSegment(mousePosition, pointSystem.transform.GetChild(0).GetChild(closestIndex).position, pointSystem.transform.GetChild(0).GetChild(closestIndex + 1).position, pointSystem.transform.GetChild(0).GetChild(closestIndex).position + pointSystem.transform.GetChild(0).GetChild(closestIndex).GetComponent<Point>().rightLocalControlPointPosition, pointSystem.transform.GetChild(0).GetChild(closestIndex + 1).position + pointSystem.transform.GetChild(0).GetChild(closestIndex + 1).GetComponent<Point>().leftLocalControlPointPosition);
            if (Vector3.Distance(positions[0], pointSystem.transform.GetChild(0).GetChild(closestIndex).position) > 1 && Vector3.Distance(positions[0], pointSystem.transform.GetChild(0).GetChild(closestIndex + 1).position) > 1)
            {
                int newIndex = closestIndex + 1;
                GameObject point = new GameObject("Point");
                Undo.RegisterCreatedObjectUndo(point, "Insert Point");
                point.transform.SetParent(pointSystem.transform.GetChild(0));
                point.transform.SetSiblingIndex(newIndex);
                point.transform.position = positions[0];
                point.AddComponent<Point>();
                point.hideFlags = HideFlags.NotEditable;

                Vector3 previousPoint = point.transform.parent.GetChild(newIndex - 1).transform.position;
                Vector3 previousControlPoint = point.transform.parent.GetChild(newIndex - 1).GetComponent<Point>().rightLocalControlPointPosition;

                Vector3 nextPoint = point.transform.parent.GetChild(newIndex + 1).transform.position;
                Vector3 nextControlPoint = point.transform.parent.GetChild(newIndex + 1).GetComponent<Point>().leftLocalControlPointPosition;

                float previousDistance = Vector3.Distance(previousPoint, nextPoint + nextControlPoint);
                float nextDistance = Vector3.Distance(previousPoint + previousControlPoint, nextPoint);

                Vector3 leftIntersection = Utility.GetLineIntersection(previousPoint, previousControlPoint.normalized, point.transform.position, (positions[0] - positions[1]).normalized, previousDistance);
                Vector3 rightIntersection = Utility.GetLineIntersection(nextPoint, nextControlPoint.normalized, point.transform.position, (positions[1] - positions[0]).normalized, nextDistance);

                Vector3 direction;
                Vector3 offset;
                float distance;

                // Set control points
                // Set them to be same as derivate of that position
                if (rightIntersection != Utility.MaxVector3)
                {
                    direction = Vector3.zero;
                    offset = nextPoint - point.transform.position + nextControlPoint;
                    direction += offset.normalized;
                    distance = (nextPoint - point.transform.position).magnitude;
                    direction.Normalize();

                    point.GetComponent<Point>().rightLocalControlPointPosition = direction * distance * 0.3f;
                }
                else
                {
                    direction = Vector3.zero;
                    offset = nextPoint - point.transform.position;
                    direction += offset.normalized;
                    distance = offset.magnitude;
                    direction.Normalize();

                    point.GetComponent<Point>().rightLocalControlPointPosition = direction * distance * 0.3f;
                }

                if (leftIntersection != Utility.MaxVector3)
                {
                    direction = Vector3.zero;
                    offset = point.GetComponent<Point>().rightLocalControlPointPosition;
                    direction -= offset.normalized;
                    distance = (previousPoint - point.transform.position).magnitude;
                    direction.Normalize();

                    point.GetComponent<Point>().leftLocalControlPointPosition = direction * distance * 0.3f;
                }
                else
                {
                    direction = Vector3.zero;
                    offset = previousPoint - point.transform.position + previousControlPoint;
                    direction += offset.normalized;
                    distance = (previousPoint - point.transform.position).magnitude;
                    direction.Normalize();

                    point.GetComponent<Point>().leftLocalControlPointPosition = direction * distance * 0.3f;
                }

                // Adapt to new point, control point half way to new control point
                Undo.RecordObject(point.transform.parent.GetChild(newIndex - 1).GetComponent<Point>(), "Insert Point");

                direction = Vector3.zero;
                offset = previousControlPoint;
                direction += offset.normalized;
                distance = (point.transform.position - previousPoint).magnitude;
                direction.Normalize();

                point.transform.parent.GetChild(newIndex - 1).GetComponent<Point>().rightLocalControlPointPosition = direction * distance * 0.3f;

                Undo.RecordObject(point.transform.parent.GetChild(newIndex + 1).GetComponent<Point>(), "Insert Point");

                direction = Vector3.zero;
                offset = point.transform.parent.GetChild(newIndex + 1).GetComponent<Point>().rightLocalControlPointPosition;
                direction -= offset.normalized;
                distance = (point.transform.position - nextPoint).magnitude;
                direction.Normalize();

                point.transform.parent.GetChild(newIndex + 1).GetComponent<Point>().leftLocalControlPointPosition = direction * distance * 0.3f;

                // Update lanes
                if (pointSystem.GetComponent<RoadCreator>() != null)
                {
                    for (int i = 0; i < pointSystem.GetComponent<RoadCreator>().lanes.Count; i++)
                    {
                        if (pointSystem.GetComponent<RoadCreator>().lanes[i].startIndex >= newIndex)
                        {
                            Undo.RecordObject(pointSystem, "Insert Point");
                            pointSystem.GetComponent<RoadCreator>().lanes[i].startIndex += 1;
                        }

                        if (pointSystem.GetComponent<RoadCreator>().lanes[i].endIndex >= newIndex)
                        {
                            Undo.RecordObject(pointSystem, "Insert Point");
                            pointSystem.GetComponent<RoadCreator>().lanes[i].endIndex += 1;
                        }
                    }
                }

                pointSystem.Regenerate(false);

                if (pointSystem.GetComponent<RoadCreator>() != null)
                {
                    pointSystem.GetComponent<RoadCreator>().UpdatePointList();
                }
            }
        }

        private void RemovePoint(Event currentEvent, PointSystemCreator pointSystem)
        {
            RoadCreator road = pointSystem.GetComponent<RoadCreator>();

            if (lastPointIndex != -1 && lastPointIndex % 3 == 0)
            {
                Vector3 screenMousePosition = currentEvent.mousePosition;
                screenMousePosition.z = 0;

                Vector3 screenPosition = HandleUtility.WorldToGUIPoint(pointSystem.transform.GetChild(0).GetChild(lastPointIndex / 3).position);
                screenPosition.z = 0;

                if (HandleUtility.nearestControl == GetIdForIndex(pointSystem, lastPointIndex / 3, 0))
                {
                    if (road != null)
                    {
                        // Update connected intersections
                        if (lastPointIndex / 3 == 0 || lastPointIndex / 3 == pointSystem.transform.GetChild(0).childCount - 1)
                        {
                            road.RemoveConnectedPoint(lastPointIndex / 3);
                        }
                    }

                    Undo.DestroyObjectImmediate(pointSystem.transform.GetChild(0).GetChild(lastPointIndex / 3).gameObject);

                    if (road != null)
                    {
                        // Change lane indexes to try and keep them at the same positions as before
                        List<Lane> lanes = road.lanes;
                        for (int i = 0; i < lanes.Count; i++)
                        {
                            Undo.RecordObject(pointSystem, "Remove Point");
                            if (lanes[i].startIndex > lastPointIndex / 3)
                            {
                                lanes[i].startIndex -= 1;
                                lanes[i].endIndex -= 1;
                            }
                            else if (lanes[i].endIndex >= lastPointIndex / 3 - 1 && lanes[i].endIndex > lanes[i].startIndex)
                            {
                                lanes[i].endIndex -= 1;
                            }
                        }

                        // Change prefab line indexes to try and keep them at the same positions as before
                        List<PrefabLineCreator> prefabLines = road.prefabLines;
                        for (int i = 0; i < prefabLines.Count; i++)
                        {
                            Undo.RecordObject(prefabLines[i], "Remove Point");
                            if (prefabLines[i].startIndex > lastPointIndex / 3)
                            {
                                prefabLines[i].startIndex -= 1;
                                prefabLines[i].endIndex -= 1;
                            }
                            else if (prefabLines[i].endIndex >= lastPointIndex / 3 - 1 && prefabLines[i].endIndex > prefabLines[i].startIndex)
                            {
                                prefabLines[i].endIndex -= 1;
                            }
                        }

                        road.UpdatePointList();
                    }

                    RemoveIdForIndex(pointSystem, lastPointIndex / 3);

                    lastPointIndex = -1;
                    pointSystem.Regenerate(false);
                    pointSystem.Regenerate(false);

                    if (road != null)
                    {
                        road.UpdatePointList();
                    }
                }
            }
        }

        #region Handle Ids

        private void RemoveIdForIndex(PointSystemCreator pointSystem, int index)
        {
            pointSystem.handleHashes.RemoveAt(index * 3);
            pointSystem.handleIds.RemoveAt(index * 3);
            pointSystem.handleHashes.RemoveAt(index * 3);
            pointSystem.handleIds.RemoveAt(index * 3);
            pointSystem.handleHashes.RemoveAt(index * 3);
            pointSystem.handleIds.RemoveAt(index * 3);
        }

        // Points 0:Main point 1:Left control point 2:Right control point
        private int GetIdForIndex(PointSystemCreator pointSystem, int index, int point)
        {
            if (point == 1)
            {
                return pointSystem.handleIds[index * 3 + 1];
            }
            else if (point == 2)
            {
                return pointSystem.handleIds[index * 3 + 2];
            }
            else
            {
                if (pointSystem.handleIds.Count <= index * 3)
                {
                    AddId(pointSystem);
                }

                return pointSystem.handleIds[index * 3];
            }
        }

        private void AddId(PointSystemCreator pointSystem)
        {
            // Main points
            int hash = ("PointHandle" + pointSystem.lastHashIndex).GetHashCode();
            pointSystem.handleHashes.Add(hash);
            int id = GUIUtility.GetControlID(hash, FocusType.Passive);
            pointSystem.handleIds.Add(id);

            // Left control point
            hash = ("PointHandle" + pointSystem.lastHashIndex + 1).GetHashCode();
            pointSystem.handleHashes.Add(hash);
            id = GUIUtility.GetControlID(hash, FocusType.Passive);
            pointSystem.handleIds.Add(id);

            // Right control point
            hash = ("PointHandle" + pointSystem.lastHashIndex + 2).GetHashCode();
            pointSystem.handleHashes.Add(hash);
            id = GUIUtility.GetControlID(hash, FocusType.Passive);
            pointSystem.handleIds.Add(id);

            pointSystem.lastHashIndex += 3;
        }

        #endregion
    }
}