using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoadCreatorPro
{
    [CustomEditor(typeof(SelectParent))]
    public class SelectParentEditor : Editor
    {
        private void OnEnable()
        {
            Transform parent = ((SelectParent)target).transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<RoadCreator>() != null || parent.GetComponent<PrefabLineCreator>() != null || parent.GetComponent<Intersection>() != null || parent.GetComponent<ProhibitedArea>() != null)
                {
                    Selection.activeGameObject = parent.gameObject;
                    break;
                }

                parent = parent.parent;
            }
        }
    }
}