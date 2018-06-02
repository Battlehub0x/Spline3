using UnityEditor;
using UnityEngine;

namespace Battlehub.Spline3
{
    public class SplinePointBaseEditor : Editor
    {
        private SerializedProperty m_pointDataProp;
        private SerializedProperty m_twistProp;
        private SerializedProperty m_twistAngleProp;
        private SerializedProperty m_twistT0Prop;
        private SerializedProperty m_twistT1Prop;
        private SerializedProperty m_junctionProp;

        private SplineBaseEditorImpl m_splineBaseEditor;
        protected virtual void OnEnable()
        {
            m_pointDataProp = serializedObject.FindProperty("m_data");
            m_junctionProp = m_pointDataProp.FindPropertyRelative("Junction");
            m_twistProp = m_pointDataProp.FindPropertyRelative("Twist");
            m_twistAngleProp = m_twistProp.FindPropertyRelative("Angle");
            m_twistT0Prop = m_twistProp.FindPropertyRelative("T0");
            m_twistT1Prop = m_twistProp.FindPropertyRelative("T1");

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
            if(m_splineBaseEditor == null)
            {
                m_splineBaseEditor = SplineBaseEditorImpl.Instance;
                m_splineBaseEditor.Enable(Selection.objects, Selection.activeObject);
            }

            SplinePointBase point = (SplinePointBase)target;
            if(Selection.activeObject == point.gameObject)
            {
                SplineBaseEditorImpl.Instance.SceneGUI();
            }  
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            
            EditorGUILayout.PropertyField(m_junctionProp, new GUIContent("Junction"));
            EditorGUILayout.PropertyField(m_twistAngleProp, new GUIContent("Angle"));

            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.PrefixLabel("Twist Offset");
            
            EditorGUI.BeginChangeCheck();
            float t0 = GUILayout.HorizontalSlider(m_twistT0Prop.floatValue, 1, 0);
            float t1 = GUILayout.HorizontalSlider(m_twistT1Prop.floatValue, 0, 1);
            if(EditorGUI.EndChangeCheck())
            {
               // Debug.Log(t0);
                m_twistT0Prop.floatValue = t0;
                m_twistT1Prop.floatValue = t1;
            }
            
            //EditorGUILayout.Slider(m_twistT0Prop, 1, 0, new GUIContent("T0"));
            //EditorGUILayout.Slider(m_twistT1Prop, 0, 1, new GUIContent("T1"));

            EditorGUILayout.EndHorizontal();
            
         
            serializedObject.ApplyModifiedProperties();
        }

     



        //public override void OnInspectorGUI()
        //{
        //    base.OnInspectorGUI();

        //    SplinePointBase[] points = SplineBaseEditorImpl.Instance.SelectedSplinePoints;

        //int mode = points[0].;
        //for (int i = 1; i < points.Length; ++i)
        //{
        //    ControlPoint bezierPoint = points[i];
        //    if (bezierPoint.Mode != mode)
        //    {
        //        mode = -1;
        //        break;
        //    }
        //}

        //int selectedMode;
        //if (mode == -1)
        //{
        //    selectedMode = EditorGUILayout.Popup("Mode", mode + 1, new[] { "Different Modes", "Free", "Aligned", "Mirrored" }) - 1;
        //}
        //else
        //{
        //    selectedMode = EditorGUILayout.Popup("Mode", mode, new[] { "Free", "Aligned", "Mirrored" });
        //}

        //if (selectedMode != -1 && selectedMode != mode)
        //{
        //    for (int i = 0; i < points.Length; ++i)
        //    {
        //        ControlPoint bezierPoint = points[i];
        //        if (bezierPoint.SplinePoint != null)
        //        {
        //            JunctionBase junction = bezierPoint.SplinePoint.GetJunction();
        //            if (junction != null)
        //            {
        //                int connectionsCount = junction.ConnectionsCount;
        //                for (int c = 0; c < connectionsCount; ++c)
        //                {
        //                    SplineBase spline = junction.GetSpline(c);
        //                    int splinePointIndex = junction.GetSplinePointIndex(c);

        //                    GameObject splinePointGO = spline.GetPoint(splinePointIndex);
        //                    SplinePointBase splinePoint = splinePointGO.GetComponent<SplinePointBase>();
        //                    if (splinePoint != null)
        //                    {
        //                        Undo.RecordObject(splinePoint, "BH.S3.BezierPoint.Mode");
        //                    }

        //                    GameObject ctrlPoint = spline.GetCtrlPoint(splinePointIndex, 0);
        //                    if (ctrlPoint != null)
        //                    {
        //                        Undo.RecordObject(ctrlPoint.transform, "BH.S3.BezierPoint.Mode");
        //                    }
        //                    GameObject twinPoint = spline.GetCtrlPoint(splinePointIndex, 1);
        //                    if (twinPoint != null)
        //                    {
        //                        Undo.RecordObject(twinPoint.transform, "BH.S3.BezierPoint.Mode");
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                Undo.RecordObject(bezierPoint.SplinePoint, "BH.S3.BezierPoint.Mode");

        //                GameObject twinPoint = bezierPoint.TwinPoint;
        //                if (twinPoint != null)
        //                {
        //                    Undo.RecordObject(twinPoint.transform, "BH.S3.BezierPoint.Mode");
        //                }

        //                Undo.RecordObject(bezierPoint.transform, "BH.S3.BezierPoint.Mode");
        //            }
        //        }

        //        bezierPoint.Mode = selectedMode;
        //    }

        //    SceneView.RepaintAll();
        //}
        //}
    }

}
