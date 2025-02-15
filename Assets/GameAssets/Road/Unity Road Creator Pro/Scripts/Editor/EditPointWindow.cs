using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoadCreatorPro
{
    public class EditPointWindow : EditorWindow
    {
        public Point lastPoint = null;
        public float lastPointIndex = 0;
        public float x, y, z = 0;
        public static EditPointWindow instance = null;

        [MenuItem("Window/Road Creator/Edit Point", false, 2500)]
        public static void ShowWindow()
        {
            EditorWindow window = EditorWindow.GetWindow(typeof(EditPointWindow));
            window.minSize = new Vector2(400, 65);
            window.maxSize = window.minSize;
            window.titleContent = new GUIContent("Edit Point");
        }

        void OnEnable()
        {
            instance = this;
        }

        void OnGUI()
        {
            if (lastPoint == null)
            {
                GUILayout.Label("Hover over a road, prefab line or prohibited area point to be able to ");
                GUILayout.Label("edit it");
            }
            else
            {
                // Update position
                if (lastPointIndex == 0)
                {
                    x = lastPoint.transform.position.x;
                    y = lastPoint.transform.position.y;
                    z = lastPoint.transform.position.z;
                }
                else if (lastPointIndex == 1)
                {
                    x = lastPoint.leftLocalControlPointPosition.x;
                    y = lastPoint.leftLocalControlPointPosition.y;
                    z = lastPoint.leftLocalControlPointPosition.z;
                }
                else
                {
                    x = lastPoint.rightLocalControlPointPosition.x;
                    y = lastPoint.rightLocalControlPointPosition.y;
                    z = lastPoint.rightLocalControlPointPosition.z;
                }

                EditorGUI.BeginChangeCheck();
                ShowButtons(ref x, "X");
                ShowButtons(ref y, "Y");
                ShowButtons(ref z, "Z");

                if (EditorGUI.EndChangeCheck())
                {
                    if (lastPointIndex == 0)
                    {
                        Undo.RecordObject(lastPoint.transform, "Move Point");
                        lastPoint.transform.position = new Vector3(x, y, z);
                    }
                    else if (lastPointIndex == 1)
                    {
                        Undo.RegisterCompleteObjectUndo(lastPoint, "Move Point");
                        lastPoint.leftLocalControlPointPosition = new Vector3(x, y, z);
                    }
                    else
                    {
                        Undo.RegisterCompleteObjectUndo(lastPoint, "Move Point");
                        lastPoint.rightLocalControlPointPosition = new Vector3(x, y, z);
                    }

                    // Update road/prefab line
                    lastPoint.transform.parent.parent.GetComponent<PointSystemCreator>().Regenerate(false);
                }
            }
        }

        public void ShowButtons(ref float axis, string label)
        {
            GUILayout.BeginHorizontal();

            // Move negative
            if (GUILayout.Button("-1"))
            {
                axis -= 1;
            }
            if (GUILayout.Button("-0.1"))
            {
                axis -= 0.1f;
            }
            if (GUILayout.Button("-0.01"))
            {
                axis -= 0.01f;
            }

            // Move custom
            EditorGUIUtility.labelWidth = 15;
            axis = EditorGUILayout.FloatField(label + ":", axis);

            // Move positive
            if (GUILayout.Button("+1"))
            {
                axis += 1;
            }
            if (GUILayout.Button("+0.1"))
            {
                axis += 0.1f;
            }
            if (GUILayout.Button("+0.01"))
            {
                axis += 0.01f;
            }

            // Copy and paste
            if (GUILayout.Button("Copy"))
            {
                EditorGUIUtility.systemCopyBuffer = "POINTPOSITION" + axis;
            }

            if (GUILayout.Button("Paste"))
            {
                if (EditorGUIUtility.systemCopyBuffer.StartsWith("POINTPOSITION"))
                {
                    axis = float.Parse(EditorGUIUtility.systemCopyBuffer.Replace("POINTPOSITION", ""));
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}