using UnityEditor;

namespace Battlehub.Spline3
{
    [CanEditMultipleObjects]
    public class JunctionBaseEditor : Editor
    {
        private JunctionBase[] m_junctions;
        private SplineBaseEditorImpl m_splineBaseEditor;

        protected virtual void OnEnable()
        {
            SceneGLCamera.Enable();
            if (m_splineBaseEditor != null)
            {
                m_splineBaseEditor.Enable(Selection.objects, Selection.activeObject);
            }

            m_junctions = new JunctionBase[targets.Length];
            for (int i = 0; i < m_junctions.Length; ++i)
            {
                m_junctions[i] = (JunctionBase)targets[i];
                m_junctions[i].Select();
            }
        }

        protected virtual void OnDisable()
        {
            for (int i = 0; i < m_junctions.Length; ++i)
            {
                if(m_junctions[i] != null)
                {
                    m_junctions[i].Unselect();
                }
            }

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
            JunctionBase junction = (JunctionBase)target;
            if (Selection.activeObject == junction.gameObject)
            {
                m_splineBaseEditor.SceneGUI();
            }
        }
    }
}

