﻿#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoadCreatorPro
{
    public class PrefabLineCreator : PointSystemCreator
    {
        // General
        public bool randomizeSpacing = false;
        public float spacing = 1;
        public float maxSpacing = 1;
        public bool fillGap = true;
        public bool deformPrefabsToCurve = true;
        public bool deformPrefabsToTerrain = true;
        public float yOffset = 0;
        public enum RotationDirection { Left, Right, Forwards, Backwards };
        public RotationDirection rotationDirection = RotationDirection.Left;
        public float rotationRandomization = 0;
        public bool centralYModification = false;
        public bool bridgePillarMode = false;
        public bool onlyYModifyBottomVertices = false;

        // Prefabs
        public GameObject startPrefab;
        public GameObject mainPrefab;
        public GameObject endPrefab;
        public float xScale = 1;
        public AnimationCurve yScale = AnimationCurve.Constant(0, 1, 1);
        public AnimationCurve zScale = AnimationCurve.Constant(0, 1, 1);

        // Internal
        public float prefabWidth = 0;
        public ComputeShader computeShader;
        public PrefabLinePreset prefabLinePreset;
        public bool controlsFolded = false;

        // Road prefab lines
        public bool controlled = false;
        public AnimationCurve offsetCurve = AnimationCurve.Constant(0, 1, 0);
        public bool wholeRoad = true;
        public int startIndex = 0;
        public float startOffsetPercentage = 0;
        public int endIndex = 0;
        public float endOffsetPercentage = 1;

        public override void Regenerate(bool updateTerrain = false, bool updateDetails = false, bool updateTrees = false)
        {
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
            {
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab)
            {
                return;
            }

            if (settings == null)
            {
                settings = RoadCreatorSettings.GetSerializedSettings();
            }

            RemoveOldPrefabs();

            if (transform.GetChild(0).childCount > 1)
            {
                CheckVariables();
                RecalculatePrefabWidth();

                // Has to be done after the width has been recalculated
                if (fillGap == true)
                {
                    spacing = prefabWidth;
                }

                VertexShaderStruct[] lastRightVertices = new VertexShaderStruct[] { };
                VertexShaderStruct[] currentRightVertices = new VertexShaderStruct[] { };

                PrefabData points = CalculatePoints();
                // Interval has size of cero
                if (points == null)
                {
                    return;
                }

                for (int i = 0; i < points.mainPoints.Count; i++)
                {
                    // Get right prefab
                    GameObject prefab;
                    if (i == 0 && startPrefab != null)
                    {
                        prefab = startPrefab;
                    }
                    else if (i == points.mainPoints.Count - 1 && endPrefab != null)
                    {
                        prefab = endPrefab;
                    }
                    else
                    {
                        prefab = mainPrefab;
                    }

                    // Prevent crash
                    if (prefab.GetComponentInChildren<MeshFilter>() == null)
                    {
                        Debug.Log("Prefab must have a mesh filter attached");
                        return;
                    }

                    // Place prefab
                    GameObject placedPrefab = Instantiate(prefab);
                    placedPrefab.name = "Prefab";
                    placedPrefab.transform.SetParent(transform.GetChild(1));
                    placedPrefab.layer = LayerMask.NameToLayer("Prefab Line");
                    placedPrefab.transform.position = Utility.Center(points.startPoints[i], points.endPoints[i]) + new Vector3(0, yOffset, 0);
                    placedPrefab.hideFlags = HideFlags.NotEditable;
                    placedPrefab.AddComponent<SelectParent>();
                    ScaleAndRotate(placedPrefab, points, i);
                    placedPrefab = placedPrefab.GetComponentInChildren<MeshFilter>().transform.gameObject; // Modify object with mesh, not empty root      
                    placedPrefab.layer = LayerMask.NameToLayer("Prefab Line");

                    // Bend
                    Mesh sharedMesh = placedPrefab.GetComponent<MeshFilter>().sharedMesh;
                    Vector3[] meshVertices = sharedMesh.vertices;
                    VertexShaderStruct[] vertices = new VertexShaderStruct[sharedMesh.vertices.Length];

                    if (deformPrefabsToCurve || deformPrefabsToTerrain || fillGap)
                    {
                        for (int j = 0; j < vertices.Length; j++)
                        {
                            // One will be kept as original, other will be modified
                            vertices[j] = new VertexShaderStruct(meshVertices[j], meshVertices[j]);
                        }
                    }

                    if (deformPrefabsToCurve || fillGap)
                    {
                        if (i == 0)
                        {
                            // Don't know how many of the vertices are on the outer right
                            currentRightVertices = new VertexShaderStruct[vertices.Length / 2];
                        }

                        BendPrefabsToCurve(placedPrefab, points, ref vertices, lastRightVertices, ref currentRightVertices, i, sharedMesh);
                        lastRightVertices = currentRightVertices;
                    }

                    if (deformPrefabsToTerrain)
                    {
                        if (centralYModification && !fillGap && !deformPrefabsToCurve)
                        {
                            BendPrefabsToTerrainCentral(placedPrefab, ref vertices);
                        }
                        else
                        {
                            BendPrefabsToTerrain(placedPrefab, ref vertices, sharedMesh.bounds.min.y, sharedMesh.bounds.max.y);
                        }
                    }

                    if (deformPrefabsToCurve || deformPrefabsToTerrain || fillGap)
                    {
                        // Assign new vertices
                        Mesh mesh = Instantiate(placedPrefab.GetComponent<MeshFilter>().sharedMesh);
                        Vector3[] vertexPositions = new Vector3[vertices.Length];

                        for (int j = 0; j < vertexPositions.Length; j++)
                        {
                            vertexPositions[j] = vertices[j].position;
                        }

                        mesh.vertices = vertexPositions;
                        mesh.RecalculateNormals();
                        mesh.RecalculateBounds();

                        placedPrefab.GetComponent<MeshFilter>().sharedMesh = mesh;
                        if (placedPrefab.GetComponent<MeshCollider>() != null)
                        {
                            placedPrefab.GetComponent<MeshCollider>().sharedMesh = mesh;
                        }

                        // Update collider
                        UpdateCollider(placedPrefab);
                    }
                }

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            else
            {
                // Remove prefabs
                for (int j = transform.GetChild(1).childCount - 1; j >= 0; j--)
                {
                    DestroyImmediate(transform.GetChild(1).GetChild(0).gameObject);
                }
            }
        }

        private void UpdateCollider(GameObject placedPrefab)
        {
            Collider collider = placedPrefab.GetComponent<Collider>();
            if (collider != null)
            {
                System.Type type = collider.GetType();

                if (type != null)
                {
                    DestroyImmediate(collider);
                    placedPrefab.AddComponent(type);
                }
            }
        }

        private void ScaleAndRotate(GameObject placedPrefab, PrefabData points, int i)
        {
            // Scale prefab
            if (rotationDirection == RotationDirection.Left || rotationDirection == RotationDirection.Right)
            {
                placedPrefab.transform.localScale = new Vector3(xScale, yScale.Evaluate(points.percentages[i]), zScale.Evaluate(points.percentages[i]));
            }
            else
            {
                placedPrefab.transform.localScale = new Vector3(zScale.Evaluate(points.percentages[i]), yScale.Evaluate(points.percentages[i]), xScale);
            }

            // Set prefab rotation
            Vector3 forward = points.endPoints[i] - points.startPoints[i];
            Vector3 left = (new Vector3(-forward.z, 0, forward.x)).normalized;

            if (rotationDirection == RotationDirection.Left)
            {
                placedPrefab.transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(left, Vector3.up).eulerAngles.y + Random.Range(-rotationRandomization, rotationRandomization), 0);
            }
            else if (rotationDirection == RotationDirection.Right)
            {
                placedPrefab.transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(-left, Vector3.up).eulerAngles.y + Random.Range(-rotationRandomization, rotationRandomization), 0);
            }
            else if (rotationDirection == RotationDirection.Forwards)
            {
                placedPrefab.transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y + Random.Range(-rotationRandomization, rotationRandomization), 0);
            }
            else if (rotationDirection == RotationDirection.Backwards)
            {
                placedPrefab.transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(-forward, Vector3.up).eulerAngles.y + Random.Range(-rotationRandomization, rotationRandomization), 0);
            }
        }

        private void BendPrefabsToCurve(GameObject placedPrefab, PrefabData points, ref VertexShaderStruct[] vertices, VertexShaderStruct[] lastRightVertices, ref VertexShaderStruct[] currentRightVertices, int i, Mesh sharedMesh)
        {
            int bufferLength = vertices.Length;

            // Pass data to shader
            // Vertices
            ComputeBuffer verticesBuffer = new ComputeBuffer(bufferLength, 24);
            verticesBuffer.SetData(vertices);
            computeShader.SetBuffer(0, "vertices", verticesBuffer);

            ComputeBuffer lastRightVerticesBuffer = new ComputeBuffer(bufferLength, 24);
            lastRightVerticesBuffer.SetData(lastRightVertices);
            lastRightVerticesBuffer.SetCounterValue(0);
            computeShader.SetBuffer(0, "lastRightVertices", lastRightVerticesBuffer);

            ComputeBuffer currentRightVerticesBuffer = new ComputeBuffer(bufferLength, 24, ComputeBufferType.Append);
            currentRightVerticesBuffer.SetData(currentRightVertices);
            currentRightVerticesBuffer.SetCounterValue(0);
            computeShader.SetBuffer(0, "currentRightVertices", currentRightVerticesBuffer);

            // Min and max values
            if (rotationDirection == RotationDirection.Left || rotationDirection == RotationDirection.Right)
            {
                computeShader.SetFloat("minX", sharedMesh.bounds.min.x);
                computeShader.SetFloat("maxX", sharedMesh.bounds.max.x);
            }
            else
            {
                computeShader.SetFloat("minX", sharedMesh.bounds.min.z);
                computeShader.SetFloat("maxX", sharedMesh.bounds.max.z);
            }

            // Curve data
            computeShader.SetFloat("startTime", points.startTimes[i]);
            computeShader.SetFloat("endTime", points.endTimes[i]);
            computeShader.SetVector("objectPosition", placedPrefab.transform.position - new Vector3(0, yOffset, 0));
            computeShader.SetVector("objectScale", placedPrefab.transform.lossyScale);
            computeShader.SetInt("rotationIndex", (int)rotationDirection);
            computeShader.SetBool("fillGap", fillGap);
            computeShader.SetBool("bendToCurve", deformPrefabsToCurve);
            computeShader.SetFloat("startOffset", points.startOffsets[i]);
            computeShader.SetFloat("endOffset", points.endOffsets[i]);

            if (i == 0)
            {
                computeShader.SetBool("first", true);
            }
            else
            {
                computeShader.SetBool("first", false);
            }

            // Minus one as last one doesn't have a segnent
            int children = transform.GetChild(0).childCount - 1;

            // Points
            Vector3[] startPoints = new Vector3[children];
            Vector3[] endPoints = new Vector3[children];
            Vector3[] startTangents = new Vector3[children];
            Vector3[] endTangents = new Vector3[children];

            for (int j = 0; j < children; j++)
            {
                Vector3 startPoint = transform.GetChild(0).GetChild(j).transform.position;
                Vector3 endPoint = transform.GetChild(0).GetChild(j + 1).transform.position;
                startPoints[j] = startPoint;
                endPoints[j] = endPoint;
                startTangents[j] = startPoint + transform.GetChild(0).GetChild(j).GetComponent<Point>().rightLocalControlPointPosition;
                endTangents[j] = endPoint + transform.GetChild(0).GetChild(j + 1).GetComponent<Point>().leftLocalControlPointPosition;
            }

            bufferLength = transform.GetChild(0).childCount - 1;
            ComputeBuffer startPointsBuffer = new ComputeBuffer(bufferLength, 12);
            startPointsBuffer.SetData(startPoints);
            computeShader.SetBuffer(0, "startPoints", startPointsBuffer);

            ComputeBuffer endPointsBuffer = new ComputeBuffer(bufferLength, 12);
            endPointsBuffer.SetData(endPoints);
            computeShader.SetBuffer(0, "endPoints", endPointsBuffer);

            ComputeBuffer startTangentsBuffer = new ComputeBuffer(bufferLength, 12);
            startTangentsBuffer.SetData(startTangents);
            computeShader.SetBuffer(0, "startTangents", startTangentsBuffer);

            ComputeBuffer endTangentsBuffer = new ComputeBuffer(bufferLength, 12);
            endTangentsBuffer.SetData(endTangents);
            computeShader.SetBuffer(0, "endTangents", endTangentsBuffer);

            Quaternion rotation = Quaternion.Inverse(placedPrefab.transform.rotation);
            computeShader.SetVector("objectRotation", new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));

            // Call shader
            computeShader.Dispatch(0, vertices.Length / 64 + 1, 1, 1);

            // Get results
            verticesBuffer.GetData(vertices);
            currentRightVerticesBuffer.GetData(currentRightVertices);

            // Clean up
            verticesBuffer.Dispose();
            lastRightVerticesBuffer.Dispose();
            currentRightVerticesBuffer.Dispose();
            startPointsBuffer.Dispose();
            endPointsBuffer.Dispose();
            startTangentsBuffer.Dispose();
            endTangentsBuffer.Dispose();
        }

        private void BendPrefabsToTerrainCentral(GameObject placedPrefab, ref VertexShaderStruct[] vertices)
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(placedPrefab.transform.position + Vector3.up, Vector3.down, out raycastHit, 100, ~(1 << LayerMask.NameToLayer("Road") | 1 << LayerMask.NameToLayer("Prefab Line") | 1 << LayerMask.NameToLayer("Intersection"))))
            {
                float y = raycastHit.point.y;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i].position.y += (y - placedPrefab.transform.position.y) / placedPrefab.transform.lossyScale.y;
                }
            }
        }

        private void BendPrefabsToTerrain(GameObject placedPrefab, ref VertexShaderStruct[] vertices, float minY, float maxY)
        {
            // X, Z, Y
            Dictionary<Vector2, float> heights = new Dictionary<Vector2, float>();
            for (int j = 0; j < vertices.Length; j++)
            {
                if (!heights.ContainsKey(new Vector2(vertices[j].localPosition.x, vertices[j].localPosition.z)))
                {
                    RaycastHit raycastHit;
                    Vector3 vertexPosition = vertices[j].position;
                    vertexPosition.x *= xScale;
                    vertexPosition.y *= placedPrefab.transform.lossyScale.y;
                    vertexPosition.z *= placedPrefab.transform.lossyScale.z;

                    if (Physics.Raycast(placedPrefab.transform.rotation * vertices[j].position + placedPrefab.transform.position + Vector3.up, Vector3.down, out raycastHit, 1000, ~(1 << LayerMask.NameToLayer("Prefab Line") | 1 << LayerMask.NameToLayer("Road") | 1 << LayerMask.NameToLayer("Intersection"))))
                    {
                        heights.Add(new Vector2(vertices[j].localPosition.x, vertices[j].localPosition.z), (raycastHit.point.y - placedPrefab.transform.position.y) / placedPrefab.transform.lossyScale.y);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (bridgePillarMode)
                {
                    float y;
                    heights.TryGetValue(new Vector2(vertices[j].localPosition.x, vertices[j].localPosition.z), out y);
                    vertices[j].position.y = y - (vertices[j].localPosition.y / maxY) * y;
                }
                else if (!onlyYModifyBottomVertices || vertices[j].localPosition.y == minY)
                {
                    float y;
                    heights.TryGetValue(new Vector2(vertices[j].localPosition.x, vertices[j].localPosition.z), out y);
                    vertices[j].position.y = vertices[j].localPosition.y + y;
                }
            }
        }

        public void RecalculatePrefabWidth()
        {
            if (mainPrefab.GetComponent<MeshFilter>() != null)
            {
                if (rotationDirection == RotationDirection.Left || rotationDirection == RotationDirection.Right)
                {
                    prefabWidth = mainPrefab.GetComponent<MeshFilter>().sharedMesh.bounds.size.x * xScale;
                }
                else
                {
                    prefabWidth = mainPrefab.GetComponent<MeshFilter>().sharedMesh.bounds.size.z * xScale;
                }
            }
        }

        private void RemoveOldPrefabs()
        {
            for (int i = transform.GetChild(1).childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(1).GetChild(i).gameObject);
            }
        }

        private void CheckVariables()
        {
            if (startPrefab == null)
            {
                startPrefab = (GameObject)settings.FindProperty("defaultStartPrefab").objectReferenceValue;
            }

            if (mainPrefab == null)
            {
                mainPrefab = (GameObject)settings.FindProperty("defaultMainPrefab").objectReferenceValue;
            }

            if (endPrefab == null)
            {
                endPrefab = (GameObject)settings.FindProperty("defaultEndPrefab").objectReferenceValue;
            }

            if (computeShader == null)
            {
                computeShader = Resources.Load("Shaders/BendPrefabs") as ComputeShader;
            }

            if (deformPrefabsToCurve || fillGap)
            {
                rotationRandomization = 0;
            }
        }

        public void ConvertToStatic()
        {
            // Move prefabs
            for (int i = transform.GetChild(1).childCount - 1; i >= 0; i--)
            {
                Undo.SetTransformParent(transform.GetChild(1).GetChild(0), transform, "Convert To Static");
            }

            // Remove points
            Undo.DestroyObjectImmediate(transform.GetChild(0).gameObject);

            // Remove prefabs object
            Undo.DestroyObjectImmediate(transform.GetChild(0).gameObject);

            // Remove scipt
            transform.hideFlags = HideFlags.None;
            Undo.DestroyObjectImmediate(this);
        }

        public PrefabData CalculatePoints()
        {
            List<Vector3> mainPoints = new List<Vector3>();
            List<float> percentages = new List<float>();
            List<Vector3> startPoints = new List<Vector3>();
            List<Vector3> endPoints = new List<Vector3>();
            List<float> startTimes = new List<float>();
            List<float> endTimes = new List<float>();
            List<int> segments = new List<int>();
            List<float> startOffsets = new List<float>();
            List<float> endOffsets = new List<float>();

            // Setup start values
            Vector3 lastLerpedPoint = Vector3.zero;
            Vector3 lastPoint = Vector3.zero;
            Vector3 lastPointOnCurve = Vector3.zero;
            float currentDistance = 0;
            float currentTotalDistance = 0;
            float totalDistance = 0;
            float[] curveLengths = new float[transform.GetChild(0).childCount - 1];
            float[] divisionLengths = new float[transform.GetChild(0).childCount - 1];
            float totalIntervalDistance = 0;
            float currentIntervalDistance = 0;

            // Caculate next spacing
            float calculatedSpacing = spacing;
            if (!fillGap && randomizeSpacing)
            {
                calculatedSpacing = Random.Range(spacing, maxSpacing);
            }

            // 0:Start point added 1:Main point added 2:End point added
            int nextStage = 0;

            // Calculate total distance
            for (int i = 0; i < transform.GetChild(0).childCount - 1; i++)
            {
                curveLengths[i] = GetCurveLenth(i, true);
                totalDistance += curveLengths[i];
                divisionLengths[i] = 1f / (Mathf.Max(3f, curveLengths[i] * detailLevel * xScale));
            }

            // Calculate total interval distance
            if (controlled)
            {
                for (int i = 0; i < transform.GetChild(0).childCount - 1; i++)
                {
                    Transform startPoint = transform.GetChild(0).GetChild(i);
                    Transform endPoint = transform.GetChild(0).GetChild(i + 1);
                    Vector3 startTangent = startPoint.position + startPoint.GetComponent<Point>().rightLocalControlPointPosition;
                    Vector3 endTangent = endPoint.position + endPoint.GetComponent<Point>().leftLocalControlPointPosition;

                    float startT = 0;
                    float endT = 1;

                    if (i == 0)
                    {
                        startT = startOffsetPercentage;
                        lastPoint = Utility.Lerp4(startPoint.position, endPoint.position, startTangent, endTangent, startT);
                    }

                    if (i == transform.GetChild(0).childCount - 2)
                    {
                        endT = endOffsetPercentage;
                    }

                    for (float t = startT; t <= endT; t += divisionLengths[i])
                    {
                        Vector3 lerpedPoint = Utility.Lerp4(startPoint.position, endPoint.position, startTangent, endTangent, t);
                        totalIntervalDistance += Vector2.Distance(new Vector2(lastPoint.x, lastPoint.z), new Vector2(lerpedPoint.x, lerpedPoint.z));
                        lastPoint = lerpedPoint;
                    }
                }
            }

            for (int i = 0; i < transform.GetChild(0).childCount - 1; i++)
            {
                Transform startPoint = transform.GetChild(0).GetChild(i);
                Transform endPoint = transform.GetChild(0).GetChild(i + 1);
                Vector3 startTangent = startPoint.position + startPoint.GetComponent<Point>().rightLocalControlPointPosition;
                Vector3 endTangent = endPoint.position + endPoint.GetComponent<Point>().leftLocalControlPointPosition;

                float startT = 0;
                float endT = 1;

                if (i == 0)
                {
                    startT = startOffsetPercentage;
                    Vector3 curveStartPosition = Utility.Lerp4(startPoint.position, endPoint.position, startTangent, endTangent, startT);
                    lastPointOnCurve = curveStartPosition;

                    if (controlled)
                    {
                        Vector3 forward = Utility.Lerp4(startPoint.position, endPoint.position, startTangent, endTangent, startT + 0.01f) - curveStartPosition;
                        Vector3 left = (new Vector3(-forward.z, 0, forward.x)).normalized;
                        curveStartPosition += left * offsetCurve.Evaluate(startT);
                    }

                    lastPoint = curveStartPosition;
                    lastLerpedPoint = curveStartPosition;
                    startPoints.Add(curveStartPosition);
                    startTimes.Add(startT);
                    startOffsets.Add(offsetCurve.Evaluate(startT));
                }

                if (i == transform.GetChild(0).childCount - 2)
                {
                    endT = endOffsetPercentage;
                }

                for (float t = startT; t <= endT; t += divisionLengths[i])
                {
                    Vector3 currentPoint = Utility.Lerp4(startPoint.position, endPoint.position, startTangent, endTangent, t);
                    Vector3 currentPointOnCurve = currentPoint;
                    float distance = Vector2.Distance(new Vector2(lastPointOnCurve.x, lastPointOnCurve.z), new Vector2(currentPointOnCurve.x, currentPointOnCurve.z));
                    currentIntervalDistance += distance;
                    float offset = 0;

                    if (controlled)
                    {
                        Vector3 forward = currentPointOnCurve - lastPointOnCurve;

                        if (t == startT)
                        {
                            forward = Utility.Lerp4(startPoint.position, endPoint.position, startTangent, endTangent, startT + 0.01f) - currentPointOnCurve;
                        }

                        Vector3 left = (new Vector3(-forward.z, 0, forward.x)).normalized;

                        // Prevent errors by trying to generate a 0 object long mesh
                        if (float.IsNaN(currentIntervalDistance / totalIntervalDistance))
                        {
                            return null;
                        }

                        offset = offsetCurve.Evaluate(currentIntervalDistance / totalIntervalDistance);
                        currentPoint += left * offset;
                    }

                    distance = Vector2.Distance(new Vector2(lastLerpedPoint.x, lastLerpedPoint.z), new Vector2(currentPoint.x, currentPoint.z));
                    currentDistance = Vector2.Distance(new Vector2(lastPoint.x, lastPoint.z), new Vector2(currentPoint.x, currentPoint.z));
                    currentTotalDistance += distance;

                    if (currentDistance >= prefabWidth / 2 && nextStage == 0)
                    {
                        // Add main point
                        mainPoints.Add(currentPoint);
                        percentages.Add(Mathf.Min(currentTotalDistance / totalDistance, 1)); // Prevent going outside animation curve
                        segments.Add(i);
                        nextStage = 1;
                    }
                    else
                    {
                        if (currentDistance >= prefabWidth && nextStage == 1)
                        {
                            // Add end points
                            endPoints.Add(currentPoint);
                            endTimes.Add(i + t);
                            endOffsets.Add(offset);
                            nextStage = 2;
                        }

                        if (currentDistance >= calculatedSpacing && nextStage == 2)
                        {
                            // Add start points
                            startPoints.Add(currentPoint);
                            startTimes.Add(i + t);
                            startOffsets.Add(offset);
                            lastPoint = currentPoint;

                            // Next prefab
                            nextStage = 0;
                            currentDistance = 0;

                            // Caculate next spacing
                            if (!fillGap && randomizeSpacing)
                            {
                                calculatedSpacing = Random.Range(spacing, maxSpacing);
                            }
                        }
                    }

                    lastLerpedPoint = currentPoint;
                    lastPointOnCurve = currentPointOnCurve;
                }
            }

            // Remove start and main points that never got an end point
            if (startPoints.Count > endPoints.Count)
            {
                startPoints.RemoveAt(startPoints.Count - 1);
                startTimes.RemoveAt(startTimes.Count - 1);

                if (mainPoints.Count > endPoints.Count)
                {
                    mainPoints.RemoveAt(mainPoints.Count - 1);
                    percentages.RemoveAt(percentages.Count - 1);
                    segments.RemoveAt(segments.Count - 1);
                }
            }

            return new PrefabData(mainPoints, percentages, startPoints, endPoints, startTimes, endTimes, segments, startOffsets, endOffsets);
        }

        public override void InitializeSystem()
        {
            if (settings == null)
            {
                settings = RoadCreatorSettings.GetSerializedSettings();
            }

            if (transform.childCount == 0)
            {
                GameObject points = new GameObject("Points");
                points.transform.SetParent(transform, false);
                points.hideFlags = HideFlags.HideInHierarchy;

                GameObject prefabs = new GameObject("Prefabs");
                prefabs.transform.SetParent(transform, false);
                prefabs.hideFlags = HideFlags.HideInHierarchy;

                // Create tags
                Utility.AddTags();
            }
        }
    }
}
#endif