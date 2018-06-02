using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Battlehub.Spline3
{
    [CustomEditor(typeof(ControlPoint))]
    [CanEditMultipleObjects]
    public class ControlPointEditor : Editor
    {
        private SplineBaseEditorImpl m_splineBaseEditor;
        protected virtual void OnEnable()
        {
            SceneGLCamera.Enable();

            if (m_splineBaseEditor != null)
            {
                m_splineBaseEditor.Enable(Selection.objects, Selection.activeObject);
            }

            Undo.undoRedoPerformed += OnUndoPerformed;
        }

        protected virtual void OnDisable()
        {
            if (m_splineBaseEditor == null)
            {
                m_splineBaseEditor = SplineBaseEditorImpl.Instance;
            }
            m_splineBaseEditor.Disable();

            Undo.undoRedoPerformed -= OnUndoPerformed;
        }

        protected virtual void OnSceneGUI()
        {
            if (m_splineBaseEditor == null)
            {
                m_splineBaseEditor = SplineBaseEditorImpl.Instance;
                m_splineBaseEditor.Enable(Selection.objects, Selection.activeObject);
            }

            if (EditorWindow.focusedWindow == SceneView.lastActiveSceneView)
            {
                if (Event.current.rawType == EventType.MouseUp || Event.current.type == EventType.MouseDown)
                {
                    m_splineBaseEditor.SyncCtrlPoints();
                }
            }
            else
            {
                m_splineBaseEditor.SyncCtrlPoints();
            }

            ControlPoint point = (ControlPoint)target;
            if (Selection.activeObject == point.gameObject)
            {
                m_splineBaseEditor.SceneGUI();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ControlPoint[] points = SplineBaseEditorImpl.Instance.SelectedBezierPoints;

            int mode = points[0].Mode;
            for (int i = 1; i < points.Length; ++i)
            {
                ControlPoint bezierPoint = points[i];
                if (bezierPoint.Mode != mode)
                {
                    mode = -1;
                    break;
                }
            }

            int selectedMode;
            if (mode == -1)
            {
                selectedMode = EditorGUILayout.Popup("Mode", mode + 1, new[] { "Different Modes", "Free", "Aligned", "Mirrored" }) - 1;
            }
            else
            {
                selectedMode = EditorGUILayout.Popup("Mode", mode, new[] { "Free", "Aligned", "Mirrored" });
            }

            if (selectedMode != -1 && selectedMode != mode)
            {
                for (int i = 0; i < points.Length; ++i)
                {
                    ControlPoint bezierPoint = points[i];
                    if (bezierPoint.SplinePoint != null)
                    {
                        JunctionBase junction = bezierPoint.SplinePoint.GetJunction();
                        if (junction != null)
                        {
                            int connectionsCount = junction.ConnectionsCount;
                            for(int c = 0; c < connectionsCount; ++c)
                            {
                                SplineBase spline = junction.GetSpline(c);
                                int splinePointIndex = junction.GetSplinePointIndex(c);

                                GameObject splinePointGO = spline.GetPoint(splinePointIndex);
                                SplinePointBase splinePoint = splinePointGO.GetComponent<SplinePointBase>();
                                if(splinePoint != null)
                                {
                                    Undo.RecordObject(splinePoint, "BH.S3.BezierPoint.Mode");
                                }
                                
                                GameObject ctrlPoint = spline.GetCtrlPoint(splinePointIndex, 0);
                                if(ctrlPoint != null)
                                {
                                    Undo.RecordObject(ctrlPoint.transform, "BH.S3.BezierPoint.Mode");
                                }
                                GameObject twinPoint = spline.GetCtrlPoint(splinePointIndex, 1);
                                if (twinPoint != null)
                                {
                                    Undo.RecordObject(twinPoint.transform, "BH.S3.BezierPoint.Mode");
                                }
                            }
                        }
                        else
                        {
                            Undo.RecordObject(bezierPoint.SplinePoint, "BH.S3.BezierPoint.Mode");

                            GameObject twinPoint = bezierPoint.TwinPoint;
                            if (twinPoint != null)
                            {
                                Undo.RecordObject(twinPoint.transform, "BH.S3.BezierPoint.Mode");
                            }

                            Undo.RecordObject(bezierPoint.transform, "BH.S3.BezierPoint.Mode");
                        }
                    }

                    bezierPoint.Mode = selectedMode;
                }

                SceneView.RepaintAll();
            }
        }

        private void OnUndoPerformed()
        {
            Repaint();
        }

    }
}

