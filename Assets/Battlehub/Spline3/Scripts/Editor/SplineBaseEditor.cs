using UnityEditor;

namespace Battlehub.Spline3
{
    public class SplineBaseEditor : Editor
    {
        private SplineBaseEditorImpl m_splineBaseEditor;
        protected virtual void OnEnable()
        {
            SceneGLCamera.Enable();
            if (m_splineBaseEditor != null)
            {
                m_splineBaseEditor.Enable(Selection.objects, Selection.activeObject);
            }
        }

        protected virtual void OnDisable()
        {
            if (m_splineBaseEditor == null)
            {
                m_splineBaseEditor = SplineBaseEditorImpl.Instance;
            }
            m_splineBaseEditor.Disable();
        }

        protected virtual void OnSceneGUI()
        {
            if (m_splineBaseEditor == null)
            {
                m_splineBaseEditor = SplineBaseEditorImpl.Instance;
                m_splineBaseEditor.Enable(Selection.objects, Selection.activeObject);
            }
            SplineBase spline = (SplineBase)target;
            if (Selection.activeObject == spline.gameObject)
            {
                m_splineBaseEditor.SceneGUI();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SplineBase[] selectedSplines = SplineBaseEditorImpl.Instance.SelectedSplines;
            if(selectedSplines == null)
            {
                return;
            }
            int mode = selectedSplines[0].InterpolationMode;
            for (int i = 1; i < selectedSplines.Length; ++i)
            {
                SplineBase spline = selectedSplines[i];
                if (spline.InterpolationMode != mode)
                {
                    mode = -1;
                    break;
                }
            }

            int selectedMode;
            if (mode == -1)
            {
                selectedMode = EditorGUILayout.Popup("Interpolation Mode", mode + 1, new[] { "Different Modes", "Bezier", "Catmull-Rom" }) - 1;
            }
            else
            {
                selectedMode = EditorGUILayout.Popup("Interpolation Mode", mode, new[] { "Bezier", "Catmull-Rom" });
            }

            if (selectedMode != -1 && selectedMode != mode)
            {
                for (int i = 0; i < selectedSplines.Length; ++i)
                {
                    SplineBase spline = selectedSplines[i];
                    Undo.RecordObject(spline, "BH.S3.Spline.InterpolationMode");
                    spline.InterpolationMode = selectedMode;
                }

                SceneView.RepaintAll();
            }
        }
    }
}



