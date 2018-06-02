using System;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;
namespace Battlehub.Spline3
{
    public class SplineBaseEditorImpl : SplineBaseEditorBase
    {
        private const float m_pointSizeConst = 0.065f;

        private static SplineBaseEditorImpl m_instance = new SplineBaseEditorImpl();
        public static SplineBaseEditorImpl Instance
        {
            get { return m_instance; }
        }

        static SplineBaseEditorImpl()
        {
            
        }

        private GUIStyle m_buttonStyle;
        private string[] m_dragPlaneText = new string[] { "cam", "x", "y", "z" };
        private DragPlaneType m_dragPlaneType = DragPlaneType.Cam;
        protected override DragPlaneType DragPlane
        {
            get { return m_dragPlaneType; }
        }

        private bool m_draggingDragPointHandle = false;
        private SplineBase m_pointerOverSpline;
        private int m_pointerOverSplinePointIndex = -1;
        private bool m_pointerOver;

        protected override Vector3 MousePosition
        {
            get { return Event.current.mousePosition; }
        }

        protected override float GetGridSize()
        {
            return 1;
        }

        protected override bool IsUnitSnapping()
        {
            return Event.current.control;
        }

        protected override bool IsRotate()
        {
            return Event.current.shift;
        }

        protected override bool IsSnapToGrid()
        {
            return Event.current.control && Event.current.keyCode == KeyCode.G;
        }

        protected override bool IsMouseUp()
        {
            return Event.current.rawType == EventType.MouseUp;
        }

        protected override float HandleSize(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position);
        }

        protected override void RegisterCreatedObjectUndo(UnityObject obj, string name)
        {
            Undo.RegisterCreatedObjectUndo(obj, name);
        }

        protected override void SetTransformParentUndo(Transform transform, Transform parent, string name)
        {
            Undo.SetTransformParent(transform, transform.parent, name);
        }

        protected override void RecordObject(UnityObject obj, string name)
        {
            Undo.RecordObject(obj, name);
        }

        protected override void RegisterFullObjectHierarchyUndo(UnityObject obj, string name)
        {
            Undo.RegisterFullObjectHierarchyUndo(obj, name);
        }

        protected override void DestroyObject(UnityObject obj)
        {
            Undo.DestroyObjectImmediate(obj);
        }

        protected override Ray ScreenPointToRay(Vector2 position)
        {
            return HandleUtility.GUIPointToWorldRay(position);
        }

        protected override void Select(GameObject obj)
        {
            if (Event.current.shift || Event.current.control)
            {
                UnityObject[] selection =  Selection.objects.Add(obj);
                Selection.activeObject = obj;
                Selection.objects = selection;
                
            }
            else
            {
                Selection.activeGameObject = obj;
            }
        }

        protected override SplineEditorEvent GetEvent(out SplineEditorEventArgs args)
        {
            SplineEditorEvent evnt = SplineEditorEvent.None;
            args = null;

            SplineBase[] splines = Splines;
            SplineRenderer[] splineRenderers = SplineRenderers;            
            if (splines == null || splineRenderers == null)
            {
                return SplineEditorEvent.None;
            }

            SplineRenderer dragSplineRenderer = null;
            for (int i = 0; i < splines.Length; ++i)
            {
                SplineRenderer splineRenderer = splineRenderers[i];
                SplineBase spline = splines[i];
               
                for(int j = 0; j < spline.PointsCount; ++j)
                {
                    if ((IsJunctionDrag || IsSplinePointCreator) && DragSpline == spline && j == DragPointIndex)
                    {
                        dragSplineRenderer = splineRenderer;
                        continue;
                    }

                    DrawSplinePoint(ref args, ref evnt, splineRenderer, spline, j);
                }
            }

            if(IsSplinePointCreator || IsJunctionDrag)
            {
                DrawSplinePoint(ref args, ref evnt, dragSplineRenderer, DragSpline, DragPointIndex);
            }

            if(evnt == SplineEditorEvent.SplinePointDrag || evnt == SplineEditorEvent.SplinePointCreatorDrag)
            {
                m_pointerOver = false;
            }  
              
            if(evnt == SplineEditorEvent.SplinePointDrop || evnt == SplineEditorEvent.SplinePointCreatorDrop)
            {                
                if (m_pointerOverSpline != null && m_pointerOverSplinePointIndex != -1)
                {
                    if (!m_pointerOver)
                    {
                        m_pointerOverSpline = null;
                        m_pointerOverSplinePointIndex = -1;
                    }

                    args = new SplineEditorEventArgs(m_pointerOverSpline, m_pointerOverSplinePointIndex);
                    m_pointerOverSpline = null;
                    m_pointerOverSplinePointIndex = -1;
                }
            }
          
            return evnt;
        }

        private void DrawSplinePoint(ref SplineEditorEventArgs args, ref SplineEditorEvent evnt, SplineRenderer splineRenderer, SplineBase spline, int j)
        {
            int mode = spline.GetPointMode(j);

            if (mode >= 0 && mode < splineRenderer.ControlPointColors.Length)
            {
                Handles.color = splineRenderer.ControlPointColors[mode];
            }

            int controlPointsCount = spline.GetCtrlPointsCount(j);
            for (int k = 0; k < controlPointsCount; ++k)
            {
                int scale = 1;
                if (!splineRenderer.IsControlPointVisible(j, k))
                {
                    scale = 0;
                }

                Vector3 ctrlPosition = spline.GetCtrlPointPosition(j, k);
                SplinePointHandle.Result ctrlPointDragResult = DrawPointHandle(splineRenderer, ctrlPosition, scale, true);
                if(ctrlPointDragResult == SplinePointHandle.Result.Drag)
                {
                    if (evnt == SplineEditorEvent.None)
                    {
                        args = new SplineEditorEventArgs(spline, j, k);
                        evnt = SplineEditorEvent.SplineCtrlPointDrag;
                    }
                }
                if (ctrlPointDragResult == SplinePointHandle.Result.EndDrag)
                {
                    if (evnt == SplineEditorEvent.None)
                    {
                        args = new SplineEditorEventArgs(spline, j, k);
                        evnt = SplineEditorEvent.SplineCtrlPointDrop;
                    }
                }
            }

            float dragHandleScale = 1;
            if (m_draggingDragPointHandle && spline == DragSpline && DragPointIndex == j)
            {
                dragHandleScale = 0;
            }

            JunctionBase junction = spline.GetJunction(j);
            if (junction != null && junction != NewJunction)
            {
                bool drawSplinePoint = junction == null || spline == junction.GetSpline(0) && j == junction.GetSplinePointIndex(0);
                if (drawSplinePoint)
                {
                    dragHandleScale = 1.5f;
                }
                else
                {
                    dragHandleScale = 0;
                }
            }


            Handles.color = splineRenderer.SplinePointColor;
            Vector3 position = spline.GetPointPosition(j);

            SplinePointHandle.Result dragResult = DrawDragHandle(splineRenderer, position, dragHandleScale, DragSpline == null);
            if (dragResult == SplinePointHandle.Result.Drag)
            {
                if (evnt == SplineEditorEvent.None)
                {
                    args = new SplineEditorEventArgs(spline, j);
                    evnt = SplineEditorEvent.SplinePointDrag;
                }
            }
            else if(dragResult == SplinePointHandle.Result.EndDrag)
            {
                if (evnt == SplineEditorEvent.None)
                {
                    args = new SplineEditorEventArgs(spline, j);
                    evnt = SplineEditorEvent.SplinePointDrop;
                }
            }
            else if(dragResult == SplinePointHandle.Result.PointerOver)
            {
                m_pointerOverSpline = spline;
                m_pointerOverSplinePointIndex = j;
                m_pointerOver = true;
            }

            if(junction != null && junction != NewJunction)
            {
                Handles.color = splineRenderer.JunctionColor;
                JunctionHandle.Result junctionResult = DrawJunctionHandle(splineRenderer, junction.transform.position, 1);
                if (junctionResult == JunctionHandle.Result.Click)
                {
                    evnt = SplineEditorEvent.JunctionClick;
                    args = new SplineEditorEventArgs(junction);
                }
                else if (junctionResult == JunctionHandle.Result.Drag)
                {
                    evnt = SplineEditorEvent.JunctionDrag;
                    args = new SplineEditorEventArgs(junction);
                }
            }

            Handles.color = splineRenderer.SplinePointColor;
            SplinePointHandle.Result result = DrawPointHandle(splineRenderer, position, 1, false);            
            if (result == SplinePointHandle.Result.EndDrag)
            {
                m_draggingDragPointHandle = false;
                if (evnt == SplineEditorEvent.None)
                {
                    args = new SplineEditorEventArgs(spline, j);
                    evnt = SplineEditorEvent.SplinePointCreatorDrop;
                }
            }
            else if (result == SplinePointHandle.Result.Drag)
            {
                m_draggingDragPointHandle = true;
                if (evnt == SplineEditorEvent.None)
                {
                    args = new SplineEditorEventArgs(spline, j);
                    evnt = SplineEditorEvent.SplinePointCreatorDrag;
                }
            }
        }

        private SplinePointHandle.Result DrawPointHandle(SplineRenderer renderer, Vector3 position, float scale, bool hasSelectionMargin)
        {
            Handles.DrawCapFunction dcf = Handles.CircleCap;
            float size = HandleUtility.GetHandleSize(position) * m_pointSizeConst;

            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(position);

            if (Vector2.Distance(guiPosition, Event.current.mousePosition) > 10 * renderer.PointSize * scale || Event.current.alt)
            {
                size *= 0;
            }

            float selectionSize;
            if(hasSelectionMargin)
            {
                selectionSize = size * (renderer.PointSize + renderer.SelectionMargin) * scale;
            }
            else
            {
                selectionSize = size * renderer.PointSize * scale;
            }

            SplinePointHandle.Result result = SplinePointHandle.HandleGUI(position, Quaternion.LookRotation(Camera.current.transform.forward),
                size * renderer.PointSize * scale, selectionSize, dcf, renderer.SelectionColor);
            return result;
        }

        private SplinePointHandle.Result DrawDragHandle(SplineRenderer renderer,  Vector3 position, float scale, bool hasSelectionMargin)
        {
            Handles.DrawCapFunction dcf = Handles.CircleCap;
            float size = HandleUtility.GetHandleSize(position) * 2.0f * m_pointSizeConst;

            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(position);

            if (Vector2.Distance(guiPosition, Event.current.mousePosition) > 15 * renderer.PointSize * scale || IsControlPointDrag || Event.current.alt)
            {
                size *= 0;
            }

            float selectionSize;
            if(hasSelectionMargin)
            {
                selectionSize = size * (renderer.PointSize + renderer.SelectionMargin) * scale;
            }
            else
            {
                selectionSize = size * renderer.PointSize * scale;
            }

            SplinePointHandle.Result result = SplinePointHandle.DragHandleGUI(position, Quaternion.LookRotation(Camera.current.transform.forward),
                size * renderer.PointSize * scale, selectionSize, dcf, renderer.SelectionColor);
            return result;
        }

        private JunctionHandle.Result DrawJunctionHandle(SplineRenderer renderer, Vector3 position, float scale)
        {
            Handles.DrawCapFunction dcf = Handles.CircleCap;
            float size = HandleUtility.GetHandleSize(position) * 2.0f * scale * m_pointSizeConst;

            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(position);

            if (Vector2.Distance(guiPosition, Event.current.mousePosition) > 15 * renderer.PointSize * scale || IsControlPointDrag || Event.current.alt)
            {
                size *= 0;
            }

            JunctionHandle.Result result = JunctionHandle.GUI(position, Quaternion.LookRotation(Camera.current.transform.forward),
                size * renderer.PointSize, size * renderer.PointSize, dcf, renderer.SelectionColor);
            return result;
        }

        public override void SceneGUI()
        {
            if (m_buttonStyle == null)
            {
                m_buttonStyle = new GUIStyle(EditorStyles.miniButton);
                m_buttonStyle.fixedWidth = 40;
            }

            Handles.BeginGUI();
            m_dragPlaneType = (DragPlaneType)GUILayout.SelectionGrid((int)m_dragPlaneType, m_dragPlaneText, 4, m_buttonStyle);
            Handles.EndGUI();

            base.SceneGUI();

            SceneView.RepaintAll();
        }
    }
}

