using UnityEditor;
using UnityEngine;

namespace Battlehub.Spline3
{
    public static class SplineMenu 
    {
        [MenuItem("Tools/Spline3/Create")]     
        public static void Create()
        {
            Vector3 pivot = Vector3.zero;
            if(SceneView.lastActiveSceneView != null)
            {
                pivot = SceneView.lastActiveSceneView.pivot;
            }

            GameObject splineGO = new GameObject();
            splineGO.name = "Spline";
            Spline spline = splineGO.AddComponent<Spline>();
            spline.SetPointMode((int)BezierPointMode.Mirrored);
            splineGO.transform.position = pivot;

            Undo.RegisterCreatedObjectUndo(splineGO, "BH.S3.Create");

            Selection.activeObject = splineGO;       
        }
    }

}
