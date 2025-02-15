using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace RoadCreatorPro
{
    [HelpURL("https://mcrafterzz.itch.io/road-creator-pro")]
    [SelectionBase]
    public class PointSystemCreator : MonoBehaviour
    {
        public float detailLevel = 10;

        // Internal
        public List<int> handleHashes = new List<int>();
        public List<int> handleIds = new List<int>();
        public int lastHashIndex = 0;
        public SerializedObject settings;

        public virtual void Regenerate(bool updateTerrain = false, bool updateDetails = false, bool updateTrees = false) { }

        public virtual void InitializeSystem() { }

        public float GetCurveLenth(int i, bool xz)
        {
            Vector3[] points = Handles.MakeBezierPoints(transform.GetChild(0).GetChild(i).position, transform.GetChild(0).GetChild(i + 1).position, transform.GetChild(0).GetChild(i).position + transform.GetChild(0).GetChild(i).GetComponent<Point>().rightLocalControlPointPosition, transform.GetChild(0).GetChild(i + 1).position + transform.GetChild(0).GetChild(i + 1).GetComponent<Point>().leftLocalControlPointPosition, (int)(Vector3.Distance(transform.GetChild(0).GetChild(i).position, transform.GetChild(0).GetChild(i + 1).position) * 1.5f));

            // Calculate distance between points
            float distance = 0;
            for (int j = 0; j < points.Length - 1; j++)
            {
                if (xz)
                {
                    distance += Vector2.Distance(new Vector2(points[j].x, points[j].z), new Vector2(points[j + 1].x, points[j + 1].z));
                }
                else
                {
                    distance += Vector3.Distance(points[j], points[j + 1]);
                }
            }

            return distance;
        }

        // Returns newly created point
        public Point SplitSegment(int segmentId)
        {
            if (segmentId > 0 && segmentId < transform.GetChild(0).childCount - 1)
            {
                GameObject newPointSystem;
                if (GetComponent<RoadCreator>() != null)
                {
                    newPointSystem = new GameObject("Road");
                    newPointSystem.AddComponent<RoadCreator>();
                }
                else
                {
                    newPointSystem = new GameObject("Prefab Line");
                    newPointSystem.AddComponent<PrefabLineCreator>();
                }

                Undo.RegisterCreatedObjectUndo(newPointSystem, "Split Segment");
                newPointSystem.transform.SetParent(newPointSystem.transform.parent, false);
                newPointSystem.transform.hideFlags = HideFlags.NotEditable;
                newPointSystem.GetComponent<PointSystemCreator>().InitializeSystem();

                // Split points
                int segments = transform.GetChild(0).childCount;

                // Copy this point
                GameObject copiedPoint = Instantiate(transform.GetChild(0).GetChild(segmentId).gameObject);
                Undo.RegisterCreatedObjectUndo(copiedPoint, "Split Segment");
                copiedPoint.transform.SetParent(newPointSystem.transform.GetChild(0));
                copiedPoint.transform.position = transform.GetChild(0).GetChild(segmentId).position;

                // Move points after this one to next segment
                for (int i = segments - 1; i >= segmentId + 1; i--)
                {
                    // Always move the same index as the next point will takes the previous space
                    Undo.SetTransformParent(transform.GetChild(0).GetChild(segmentId + 1), newPointSystem.transform.GetChild(0), "Split Segment");
                }

                if (GetComponent<RoadCreator>() != null)
                {
                    RoadCreator road = GetComponent<RoadCreator>();
                    RoadCreator newRoad = newPointSystem.GetComponent<RoadCreator>();
                    Undo.RegisterCompleteObjectUndo(road, "Split Segment");
                    Undo.RegisterCompleteObjectUndo(newRoad, "Split Segment");

                    // Move intersection connection
                    if (road.endIntersection != null)
                    {
                        newRoad.endIntersection = road.endIntersection;
                        newRoad.endIntersectionConnection = road.endIntersectionConnection;
                        road.endIntersection = null;
                        road.endIntersectionConnection = null;
                    }

                    // Move lanes
                    for (int i = road.lanes.Count - 1; i >= 0; i--)
                    {
                        if (road.lanes[i].wholeRoad)
                        {
                            Lane newLane = new Lane(road.lanes[i]);
                            newRoad.lanes.Insert(0, newLane);
                        }
                        else if (road.lanes[i].endIndex >= segmentId)
                        {
                            Lane newLane = new Lane(road.lanes[i]);
                            newLane.startIndex -= segmentId;
                            newLane.endIndex -= segmentId;
                            newRoad.lanes.Insert(0, newLane);

                            if (road.lanes[i].startIndex >= segmentId)
                            {
                                road.lanes.RemoveAt(i);
                            }
                            else
                            {
                                road.lanes[i].endIndex = Mathf.Min(segmentId - 1, road.lanes[i].endIndex);
                                newRoad.lanes[0].startIndex = 0;
                            }
                        }
                        else
                        {
                            road.lanes[i].endIndex = Mathf.Min(segmentId - 1, road.lanes[i].endIndex);
                        }
                    }

                    // Move prefab lines
                    for (int i = road.prefabLines.Count - 1; i >= 0; i--)
                    {
                        if (road.prefabLines[i].wholeRoad)
                        {
                            PrefabLineCreator newPrefabLine = Instantiate(road.transform.GetChild(2).GetChild(i).gameObject).GetComponent<PrefabLineCreator>();
                            newPrefabLine.transform.SetParent(newRoad.transform.GetChild(2));
                            newRoad.prefabLines.Insert(0, newPrefabLine);
                        }
                        else if (road.prefabLines[i].endIndex >= segmentId)
                        {
                            PrefabLineCreator prefabLine = Instantiate(road.transform.GetChild(2).GetChild(i).GetComponent<PrefabLineCreator>());
                            prefabLine.startIndex -= segmentId;
                            prefabLine.endIndex -= segmentId;
                            prefabLine.transform.SetParent(newRoad.transform.GetChild(2));
                            newRoad.prefabLines.Insert(0, prefabLine);

                            if (road.prefabLines[i].startIndex >= segmentId)
                            {
                                road.prefabLines.RemoveAt(i);
                                Undo.DestroyObjectImmediate(road.transform.GetChild(2).GetChild(i).gameObject);
                            }
                            else
                            {
                                road.prefabLines[i].endIndex = Mathf.Min(segmentId - 1, road.prefabLines[i].endIndex);
                                newRoad.prefabLines[0].startIndex = 0;
                            }
                        }
                        else
                        {
                            road.prefabLines[i].endIndex = Mathf.Min(segmentId - 1, road.prefabLines[i].endIndex);
                        }
                    }
                }

                Regenerate(false);
                newPointSystem.GetComponent<PointSystemCreator>().Regenerate(false);

                if (GetComponent<RoadCreator>() != null)
                {
                    GetComponent<RoadCreator>().UpdatePointList();
                }

                return copiedPoint.GetComponent<Point>();
            }
            return null;
        }

        public void SnapPointsToTerrain ()
        {
            for (int i = 0; i < transform.GetChild(0).childCount; i++)
            {
                // Snap anchor point
                RaycastHit raycastHit;
                if (Physics.Raycast(new Ray(transform.GetChild(0).GetChild(i).position + Vector3.up * 50, Vector3.down), out raycastHit, 100, ~(1 << LayerMask.NameToLayer("Road") | 1 << LayerMask.NameToLayer("Intersection") | 1 << LayerMask.NameToLayer("Prefab Line"))))
                {
                    transform.GetChild(0).GetChild(i).position = raycastHit.point;
                }
            }
        }
    }
}
#endif