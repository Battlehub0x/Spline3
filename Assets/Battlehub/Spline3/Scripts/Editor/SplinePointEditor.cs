using UnityEditor;

namespace Battlehub.Spline3
{
    [CustomEditor(typeof(SplinePoint))]
    [CanEditMultipleObjects]
    public class SplinePointEditor : SplinePointBaseEditor
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
