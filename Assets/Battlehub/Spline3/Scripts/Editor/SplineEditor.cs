using UnityEditor;
using UnityEngine;

namespace Battlehub.Spline3
{
    [CustomEditor(typeof(Spline))]
    [CanEditMultipleObjects]
    public class SplineEditor : SplineBaseEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
        }
    }
}

