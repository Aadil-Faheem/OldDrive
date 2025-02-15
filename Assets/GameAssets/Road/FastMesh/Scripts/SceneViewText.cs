#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class SceneViewText : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            Handles.Label(sceneView.camera.transform.position + Vector3.up * 2, "Custom Scene Text");
        }
    }
#endif
}
