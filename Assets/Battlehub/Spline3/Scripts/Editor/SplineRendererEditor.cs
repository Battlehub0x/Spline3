using Battlehub.RTCommon;
using UnityEditor;
using UnityEngine;

namespace Battlehub.Spline3
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SplineRenderer))]
    public class SplineRendererEditor : Editor
    {
        private void OnEnable()
        {
            SceneGLCamera.Enable();
        }
    }
}

