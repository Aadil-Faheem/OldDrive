using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace RoadCreatorPro
{
    [InitializeOnLoad]
    static public class ContextClickSelect
    {
        const int MAX_OBJ_FOUND = 30;
        const string LEVEL_SEPARATOR = "          ";

        static ContextClickSelect()
        {
            if (EditorApplication.isPlaying) return;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
        }

        static bool clickDown = false;
        static Vector2 clickDownPos;

        static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                clickDownPos = e.mousePosition;
                clickDown = true;
            }
            else if (e.type == EventType.MouseUp && e.button == 1 && clickDown)
            {
                clickDown = false;
                if (clickDownPos == e.mousePosition && !e.shift)
                {
                    OpenContextMenu(e.mousePosition, sceneView);
                }
            }
        }

        static void OpenContextMenu(Vector2 pos, SceneView sceneView)
        {
            SerializedObject settings = RoadCreatorSettings.GetSerializedSettings();
            GameObject activeObject = Selection.activeGameObject;
            // Only show for road objects
            if (!settings.FindProperty("showSceneViewMenuForAllObjects").boolValue && (activeObject == null || (activeObject.GetComponent<RoadCreator>() == null && activeObject.GetComponent<PrefabLineCreator>() == null && activeObject.GetComponent<Intersection>() == null && activeObject.GetComponent<ProhibitedArea>() == null)))
            {
                return;
            }

            // Menu disabled
            if (settings.FindProperty("sceneViewMenu").enumValueIndex == 0)
            {
                return;
            }

            Event.current.Use();

            GenericMenu contextMenu = new GenericMenu();

            // Creation part
            if (settings.FindProperty("sceneViewMenu").enumValueIndex == 1 || settings.FindProperty("sceneViewMenu").enumValueIndex == 3)
            {
                contextMenu.AddItem(new GUIContent("Create Road"), false, CreateRoad);
                contextMenu.AddItem(new GUIContent("Create Prefab Line"), false, CreatePrefabLine);
            }

            // Only seperate if there is content above and below
            if (settings.FindProperty("sceneViewMenu").enumValueIndex == 3)
            {
                contextMenu.AddSeparator("");
            }

            // Selection part
            if (settings.FindProperty("sceneViewMenu").enumValueIndex == 2 || settings.FindProperty("sceneViewMenu").enumValueIndex == 3)
            {
                Vector2 invertedPos = new Vector2(pos.x, sceneView.position.height - 16 - pos.y);

                Dictionary<Transform, List<Transform>> parentChildsDict = new Dictionary<Transform, List<Transform>>();

                Vector3 worldPosition = Utility.GetMousePosition(true, true);
                RaycastHit[] raycastHits = Physics.SphereCastAll(worldPosition, 1, Vector3.down);

                if (raycastHits.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < raycastHits.Length; i++)
                {
                    GameObject obj = raycastHits[i].transform.gameObject;

                    if (obj != null)
                    {
                        parentChildsDict[obj.transform] = null;

                        Transform currentParent = obj.transform.parent;
                        Transform lastParent = obj.transform;
                        List<Transform> currentChilds;

                        while (currentParent != null)
                        {
                            if (parentChildsDict.TryGetValue(currentParent, out currentChilds))
                            {
                                currentChilds.Add(lastParent);
                            }
                            else
                            {
                                parentChildsDict.Add(currentParent, new List<Transform>() { lastParent });
                            }

                            if (currentParent.hideFlags == HideFlags.HideInHierarchy || currentParent.hideFlags == HideFlags.HideInInspector)
                            {
                                parentChildsDict[currentParent].Remove(lastParent);
                            }

                            lastParent = currentParent;
                            currentParent = currentParent.parent;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var parentChild in parentChildsDict.Where(keyValue => keyValue.Key.parent == null))
                {
                    CreateMenuRecu(contextMenu, parentChild.Key, "", parentChildsDict);
                }

                if (parentChildsDict.Count == 0)
                {
                    AddMenuItem(contextMenu, "No object found below mouse cursor", null);
                }
            }

            contextMenu.ShowAsContext();
        }

        static void CreateMenuRecu(GenericMenu menu, Transform current, string currentPath, Dictionary<Transform, List<Transform>> parentChilds)
        {
            if (current.hideFlags == HideFlags.HideInHierarchy || current.hideFlags == HideFlags.HideInInspector)
            {
                return;
            }

            AddMenuItem(menu, currentPath + current.name, current);
            List<Transform> childs;

            if (!parentChilds.TryGetValue(current, out childs))
            {
                return;
            }

            if (childs == null)
            {
                return;
            }

            foreach (var child in childs)
            {
                CreateMenuRecu(menu, child, currentPath + LEVEL_SEPARATOR, parentChilds);
            }
        }

        // Context menu
        static void AddMenuItem(GenericMenu menu, string menuPath, Transform asset)
        {
            menu.AddItem(new GUIContent(menuPath), false, OnItemSelected, asset);
        }

        private static void OnItemSelected(object itemSelected)
        {
            if (itemSelected != null)
            {
                Selection.activeTransform = itemSelected as Transform;
            }
        }

        // Creation of objects
        public static void CreateRoad()
        {
            GameObject road = new GameObject("Road");
            road.AddComponent<RoadCreator>();
            Undo.RegisterCreatedObjectUndo(road, "Create Road");
            Selection.activeObject = road;
        }

        public static void CreatePrefabLine()
        {
            GameObject prefabLine = new GameObject("Prefab Line");
            prefabLine.AddComponent<PrefabLineCreator>();
            Undo.RegisterCreatedObjectUndo(prefabLine, "Create Prefab Line");
            Selection.activeObject = prefabLine;
        }
    }
}