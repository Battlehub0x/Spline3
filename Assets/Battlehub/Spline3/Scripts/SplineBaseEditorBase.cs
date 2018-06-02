using UnityEngine;

using UnityObject = UnityEngine.Object;
using System.Collections.Generic;
using System.Linq;

namespace Battlehub.Spline3
{
    public class ConnectedSplinesFinder
    {
        private HashSet<JunctionBase> m_visitedJunctions = new HashSet<JunctionBase>();
        private HashSet<SplineBase> m_visitedSplines = new HashSet<SplineBase>();

        private Queue<JunctionBase> m_queue = new Queue<JunctionBase>();

        public JunctionBase[] Junctions
        {
            get { return m_visitedJunctions.ToArray(); }
        }

        public SplineBase[] Splines
        {
            get { return m_visitedSplines.ToArray(); }
        }
        
        public void Find(IEnumerable<JunctionBase> junctions, IEnumerable<SplineBase> splines)
        {
            m_queue.Clear();

            m_visitedSplines = new HashSet<SplineBase>(splines.Distinct());
            m_visitedJunctions = new HashSet<JunctionBase>(junctions.Distinct());
            foreach (JunctionBase junction in m_visitedJunctions)
            {
                m_queue.Enqueue(junction);
            }

            foreach(SplineBase spline in m_visitedSplines)
            {
                ExtractJunctions(spline);
            }

            while(m_queue.Count != 0)
            {
                JunctionBase junction = m_queue.Dequeue();
                for(int i = 0; i < junction.ConnectionsCount; ++i)
                {
                    SplineBase spline = junction.GetSpline(i);
                    if(spline != null)
                    {
                        if(!m_visitedSplines.Contains(spline))
                        {
                            ExtractJunctions(spline);
                            m_visitedSplines.Add(spline);
                        }
                    }
                }
            }
        }

        private void ExtractJunctions(SplineBase spline)
        {
            for (int i = 0; i < spline.PointsCount; ++i)
            {
                JunctionBase junction = spline.GetJunction(i);
                if (junction != null && !m_visitedJunctions.Contains(junction))
                {
                    m_visitedJunctions.Add(junction);
                    m_queue.Enqueue(junction);
                }
            }
        }

    }

    public abstract class SplineBaseEditorBase
    {        
        private const float MinMag = 0.0001f;

        protected enum DragPlaneType
        {
            Cam,
            X,
            Y,
            Z,
        }

        protected enum SplineEditorEvent
        {
            None,
            SplinePointCreatorDrag,
            SplinePointCreatorDrop,
            SplineCtrlPointDrag,
            SplineCtrlPointDrop,
            SplinePointDrag,
            SplinePointDrop,
            JunctionClick,
            JunctionDrag,
        }

        protected class SplineEditorEventArgs
        {
            public SplineBase Spline;            
            public int PointIndex;
            public int CtrlPointIndex = -1;
            public JunctionBase Junction;

            public SplineEditorEventArgs(SplineBase spline, int pointIndex, int ctrlPointIndex = -1)
            {
                Spline = spline;
                PointIndex = pointIndex;
                CtrlPointIndex = ctrlPointIndex;
            }

            public SplineEditorEventArgs(JunctionBase junction)
            {
                Junction = junction;
            }
        }

        private SplineBase m_refSpline;
        private Vector3 m_refSplinePosition;
        private Vector3 m_refSplineScale;
        private Quaternion m_refSplineRotation;

        private JunctionBase[] m_selectedJunctions;
        private SplineBase[] m_splines;
        private SplineBase[] m_selectedSplines;
        private SplineRenderer[] m_splineRenderers;
        private SplinePointBase[] m_selectedSplinePoints;
        private ControlPoint[] m_selectedBezierPoints;

        private Vector3 m_beginDragPosition;
        private SplineBase m_dragSpline;
        private JunctionBase m_newJunction;
        private JunctionBase m_dragJunction;
        private int m_dragPointIndex = -1;
        private int m_dragCtrlPointIndex = -1;
        private Plane m_dragPlane;
        private bool m_beforeSplinePointCreatorDrag;
        private bool m_splinePointCreatorDrag;
        private bool m_splinePointDrag;
        private bool m_beforeJunctionDrag;
        private bool m_junctionDrag;
        private bool m_splineCtrlPointDrag;

        public bool IsSplinePointCreator
        {
            get { return m_splinePointCreatorDrag; }
        }

        public bool IsJunctionDrag
        {
            get { return m_junctionDrag; }
        }

        public bool IsControlPointDrag
        {
            get { return m_splineCtrlPointDrag; }
        }

        protected JunctionBase NewJunction
        {
            get { return m_newJunction; }
        }

        protected SplineBase DragSpline
        {
            get { return m_dragSpline; }
        }

        protected int DragPointIndex
        {
            get { return m_dragPointIndex; }
        }

        protected SplineBase[] Splines
        {
            get { return m_splines; }
        }

        public SplineBase[] SelectedSplines
        {
            get { return m_selectedSplines; }
        }
        
        protected SplineRenderer[] SplineRenderers
        {
            get { return m_splineRenderers; }
        }

        public SplinePointBase[] SelectedSplinePoints
        {
            get { return m_selectedSplinePoints; }
        }

        public ControlPoint[] SelectedBezierPoints
        {
            get { return m_selectedBezierPoints; }
        }

        protected abstract Vector3 MousePosition
        {
            get;
        }

        protected abstract DragPlaneType DragPlane
        {
            get;
        }

        protected abstract float GetGridSize();
        protected abstract bool IsUnitSnapping();
        protected abstract bool IsSnapToGrid();
        protected abstract bool IsRotate();
        protected abstract bool IsMouseUp();
        protected abstract float HandleSize(Vector3 position);
        protected abstract Ray ScreenPointToRay(Vector2 position);
        protected abstract SplineEditorEvent GetEvent(out SplineEditorEventArgs args);
        protected abstract void Select(GameObject obj);
        protected abstract void RegisterCreatedObjectUndo(UnityObject obj, string name);
        protected abstract void SetTransformParentUndo(Transform transform, Transform parent, string name);
        protected abstract void RecordObject(UnityObject obj, string name);
        protected abstract void RegisterFullObjectHierarchyUndo(UnityObject obj, string name);
        protected abstract void DestroyObject(UnityObject obj);
        
        private void GetDragPlane(Vector3 position)
        {
            Vector3 camFwd = Camera.current.transform.forward;
            switch (DragPlane)
            {
                case DragPlaneType.Cam:
                    m_dragPlane = new Plane(-camFwd, position);
                    break;
                case DragPlaneType.X:
                    m_dragPlane = new Plane(Vector3.right, position);
                    break;
                case DragPlaneType.Y:
                    m_dragPlane = new Plane(Vector3.up, position);
                    break;
                case DragPlaneType.Z:
                    m_dragPlane = new Plane(Vector3.forward, position);
                    break;
            }
        }

        private bool GetPositionOnDragPlane(Ray ray, out Vector3 position)
        {
            float distance;
            if (m_dragPlane.Raycast(ray, out distance))
            {
                position = ray.GetPoint(distance);
                return true;
            }
            position = Vector3.zero;
            return false;
        }

        public void SyncCtrlPoints()
        {
            for (int i = 0; i < m_selectedBezierPoints.Length; ++i)
            {
                m_selectedBezierPoints[i].SyncCtrlPointPosition();
            }
        }

        public virtual void Enable(UnityObject[] selection, UnityObject activeObject)
        {
            Disable();

            m_refSpline = selection.Where(s => s is GameObject && ((GameObject)s).GetComponent<SplineBase>()).Select(g => ((GameObject)g).GetComponent<SplineBase>()).FirstOrDefault();
            if(m_refSpline != null)
            {
                m_refSplinePosition = m_refSpline.transform.position;
                m_refSplineRotation = m_refSpline.transform.rotation;
                m_refSplineScale = m_refSpline.transform.localScale;              
            }
            HashSet<JunctionBase> junctions = new HashSet<JunctionBase>();
            HashSet<SplineBase> splines = new HashSet<SplineBase>();
            HashSet<SplinePointBase> splinePoints = new HashSet<SplinePointBase>();
            HashSet<ControlPoint> bezierPoints = new HashSet<ControlPoint>();

            List<SplineBase> selectedSplines = new List<SplineBase>();
            for(int i = 0; i < selection.Length; ++i)
            {
                UnityObject obj = selection[i];
                if(obj is GameObject)
                {
                    GameObject go = (GameObject)obj;

                    SplineBase spline = go.GetComponent<SplineBase>();
                    if (spline != null)
                    {
                        if(!splines.Contains(spline))
                        {
                            splines.Add(spline);
                        }

                        selectedSplines.Add(spline);
                    }
                    SplinePointBase splinePoint = go.GetComponent<SplinePointBase>();
                    if (splinePoint != null)
                    {
                        SplineBase parentSpline = splinePoint.GetComponentInParent<SplineBase>();
                        if (parentSpline != null)
                        {
                            if (!splines.Contains(parentSpline))
                            {
                                splines.Add(parentSpline);
                            }
                        }
                        if(!splinePoints.Contains(splinePoint))
                        {
                            splinePoints.Add(splinePoint);
                        }
                    }
                    ControlPoint bezierPoint = go.GetComponent<ControlPoint>();
                    if(bezierPoint != null)
                    {
                        SplineBase parentSpline = bezierPoint.GetComponentInParent<SplineBase>();
                        if(parentSpline != null)
                        {
                            if(!splines.Contains(parentSpline))
                            {
                                splines.Add(parentSpline);
                            }
                        }
                        if(!bezierPoints.Contains(bezierPoint))
                        {
                            bezierPoints.Add(bezierPoint);
                        }
                    }
                    JunctionBase junction = go.GetComponent<Junction>();
                    if(junction != null)
                    {
                        if(!junctions.Contains(junction))
                        {
                            junctions.Add(junction);
                        }
                    }
                }
            }

            m_selectedJunctions = junctions.ToArray();
            for(int i = 0; i < m_selectedJunctions.Length; ++i)
            {
                m_selectedJunctions[i].Select();
            }

            ConnectedSplinesFinder finder = new ConnectedSplinesFinder();
            finder.Find(junctions, splines);

            m_splines = finder.Splines;
            m_splineRenderers = new SplineRenderer[m_splines.Length];
            for(int i = 0; i < m_splines.Length; ++i)
            {
                m_splineRenderers[i] = m_splines[i].GetComponent<SplineRenderer>();
                m_splineRenderers[i].IsSelected = true;
            }

            m_selectedSplinePoints = splinePoints.ToArray();
            for(int i = 0; i < m_selectedSplinePoints.Length; ++i)
            {
                m_selectedSplinePoints[i].Select();
            }

            m_selectedBezierPoints = bezierPoints.ToArray();
            for(int i = 0; i < m_selectedBezierPoints.Length; ++i)
            {
                m_selectedBezierPoints[i].Select();
            }
            SyncCtrlPoints();

            m_selectedSplines = selectedSplines.ToArray();
        }

        public virtual void Disable()
        {
            EndDrag();

            if (m_selectedJunctions != null)
            {
                for(int i = 0; i < m_selectedJunctions.Length; ++i)
                {
                    JunctionBase junction = m_selectedJunctions[i];
                    if(junction != null)
                    {
                        junction.Unselect();
                    }
                }
            }

            if(m_splineRenderers != null)
            {
                for(int i = 0; i < m_splineRenderers.Length; ++i)
                {
                    SplineRenderer splineRenderer = m_splineRenderers[i];
                    if(splineRenderer != null)
                    {
                        splineRenderer.IsSelected = false;
                    }
                }
            }

            if(m_selectedSplinePoints != null)
            {
                for(int i = 0; i < m_selectedSplinePoints.Length; ++i)
                {
                    SplinePointBase splinePoint = m_selectedSplinePoints[i];
                    if(splinePoint != null)
                    {
                        splinePoint.Unselect();
                    }
                }
            }

            if(m_selectedBezierPoints != null)
            {
                for(int i = 0; i < m_selectedBezierPoints.Length; ++i)
                {
                    ControlPoint bezierPoint = m_selectedBezierPoints[i];
                    if(bezierPoint != null)
                    {
                        bezierPoint.Unselect();
                    }
                }
            }

            m_splines = null;
            m_splineRenderers = null;
            m_selectedSplines = null;
            m_selectedJunctions = null;
            m_selectedSplinePoints = null;
            m_selectedBezierPoints = null;
        }

        public virtual void SceneGUI()
        {
            if (m_refSpline != null)
            {
                if(m_refSplinePosition != m_refSpline.transform.position ||
                   m_refSplineRotation != m_refSpline.transform.rotation ||
                   m_refSplineScale != m_refSpline.transform.localScale)
                {

                    m_refSpline.UpdateJunctions();
                    m_refSplinePosition = m_refSpline.transform.position;
                    m_refSplineRotation = m_refSpline.transform.rotation;
                    m_refSplineScale = m_refSpline.transform.localScale;
                }
            }

            SplineEditorEventArgs eventArgs;
            SplineEditorEvent eventType = GetEvent(out eventArgs);

            if (m_splinePointCreatorDrag || m_splinePointDrag || m_beforeSplinePointCreatorDrag || m_beforeJunctionDrag || m_junctionDrag || m_splineCtrlPointDrag)
            {
                if (IsMouseUp())
                {
                    EndDrag();
                }
            }

            if(IsSnapToGrid())
            {
                SnapToGrid();
            }

            switch (eventType)
            {
                case SplineEditorEvent.SplineCtrlPointDrag:
                    {
                        m_dragSpline = eventArgs.Spline;
                        m_dragPointIndex = eventArgs.PointIndex;
                        m_dragCtrlPointIndex = eventArgs.CtrlPointIndex; 
                        GetDragPlane(m_dragSpline.GetCtrlPointPosition(m_dragPointIndex, m_dragCtrlPointIndex));
                        if (GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out m_beginDragPosition))
                        {
                            GameObject ctrlPoint = m_dragSpline.GetCtrlPoint(m_dragPointIndex, m_dragCtrlPointIndex);
                            GameObject splinePoint = m_dragSpline.GetPoint(m_dragPointIndex);
                            if(ctrlPoint != null)
                            {
                                RecordObject(ctrlPoint.transform, "BH.S3.DragSplinePoint");
                            }
                            if(splinePoint != null)
                            {
                                RecordObject(splinePoint.transform, "BH.S3.DragSplinePoint");
                            }
                            m_splineCtrlPointDrag = true;
                        }
                        else
                        {
                            m_dragSpline = null;
                            m_dragPointIndex = -1;
                            m_dragCtrlPointIndex = -1;
                        }
                    }
                    break;
                case SplineEditorEvent.SplineCtrlPointDrop:
                    {
                        if (m_splineCtrlPointDrag)
                        {
                            EndDrag();
                        }
                        else
                        {
                            if (eventArgs.CtrlPointIndex >= 0)
                            {
                                GameObject splinePoint = eventArgs.Spline.GetCtrlPoint(eventArgs.PointIndex, eventArgs.CtrlPointIndex);
                                Select(splinePoint);
                            }
                            else
                            {
                                GameObject splinePoint = eventArgs.Spline.GetPoint(eventArgs.PointIndex);
                                Select(splinePoint);
                            }
                        }
                    }
                    break;
                case SplineEditorEvent.SplinePointCreatorDrag:
                    {
                        if(!m_beforeSplinePointCreatorDrag && !m_splinePointCreatorDrag)
                        {
                            m_dragSpline = eventArgs.Spline;
                            m_dragPointIndex = eventArgs.PointIndex;
                            GetDragPlane(m_dragSpline.GetPointPosition(m_dragPointIndex));
                            if (GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out m_beginDragPosition))
                            {
                                m_beforeSplinePointCreatorDrag = true;
                            }
                            else
                            {
                                m_dragSpline = null;
                                m_dragPointIndex = -1;
                            }
                        }    
                    }
                    break;
                case SplineEditorEvent.SplinePointCreatorDrop:
                    {
                        if(m_splinePointCreatorDrag)
                        {
                            SplineBase dropSpline = eventArgs.Spline;
                            int dropPointIndex = eventArgs.PointIndex;
                            if (m_dragPointIndex >= 0 && m_dragSpline != null && dropPointIndex >= 0 && dropSpline != null && (dropSpline != m_dragSpline || dropPointIndex != m_dragPointIndex))
                            {
                                dropSpline.Connect(dropPointIndex, m_dragSpline, m_dragPointIndex);
                                JunctionBase junction = dropSpline.GetJunction(dropPointIndex);
                                if(junction.ConnectionsCount == 2)
                                {
                                    RegisterCreatedObjectUndo(junction.gameObject, "BH.S3.Junction");
                                }
                            }

                            Select(m_dragSpline.gameObject);
                            EndDrag();
                        }
                        else
                        {
                            if (eventArgs.CtrlPointIndex >= 0)
                            {
                                GameObject splinePoint = eventArgs.Spline.GetCtrlPoint(eventArgs.PointIndex, eventArgs.CtrlPointIndex);
                                Select(splinePoint);
                            }
                            else
                            {
                                GameObject splinePoint = eventArgs.Spline.GetPoint(eventArgs.PointIndex);
                                Select(splinePoint);
                            }
                        }
                        
                    }
                    break;

                case SplineEditorEvent.SplinePointDrag:
                    {
                        if(!m_splinePointDrag)
                        {
                            m_dragSpline = eventArgs.Spline;
                            m_dragPointIndex = eventArgs.PointIndex;
                            GetDragPlane(m_dragSpline.GetPointPosition(m_dragPointIndex));

                            if (GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out m_beginDragPosition))
                            {
                                m_splinePointDrag = true;

                                JunctionBase junction = m_dragSpline.GetJunction(m_dragPointIndex);
                                if(junction == null)
                                {
                                    GameObject splinePointGO = m_dragSpline.GetPoint(m_dragPointIndex);
                                    if (splinePointGO != null)
                                    {
                                        RegisterFullObjectHierarchyUndo(splinePointGO, "BH.S3.DragSplinePoint");
                                    }
                                }
                                else
                                {
                                    int connectionsCount = junction.ConnectionsCount;
                                    for(int i = 0; i < connectionsCount; ++i)
                                    {
                                        SplineBase spline = junction.GetSpline(i);
                                        int pointIndex = junction.GetSplinePointIndex(i);
                                        GameObject splinePointGO = spline.GetPoint(pointIndex);
                                        if(splinePointGO != null)
                                        {
                                            RegisterFullObjectHierarchyUndo(splinePointGO, "BH.S3.DragSplinePoint");
                                        }
                                    }
                                    RecordObject(junction.transform, "BH.S3.DragSplinePoint");
                                }
                            }
                            else
                            {
                                m_dragSpline = null;
                                m_dragPointIndex = -1;
                            }
                        }
                    }
                    break;
                case SplineEditorEvent.SplinePointDrop:
                    {
                        if(m_splinePointDrag)
                        {
                            SplineBase dropSpline = eventArgs.Spline;
                            int dropPointIndex = eventArgs.PointIndex;
                            if (m_dragPointIndex >= 0 && m_dragSpline != null && dropPointIndex >= 0 && dropSpline != null && (dropSpline != m_dragSpline || dropPointIndex != m_dragPointIndex))
                            {
                                JunctionBase dropJunction = dropSpline.GetJunction(dropPointIndex);
                                JunctionBase dragJunction = m_dragSpline.GetJunction(m_dragPointIndex);
                                if(dragJunction != dropJunction || dragJunction == null && dropJunction == null)
                                {
                                    if (dropJunction != null)
                                    {
                                        RecordObject(dropJunction, "BH.S3.EndDragSplinePoint");
                                        GameObject splinePointGO = m_dragSpline.GetPoint(m_dragPointIndex);
                                        if (splinePointGO != null)
                                        {
                                            SplinePointBase splinePoint = splinePointGO.GetComponent<SplinePointBase>();
                                            if (splinePoint != null)
                                            {
                                                RecordObject(splinePoint, "BH.S3.EndDragSplinePoint");
                                            }
                                        }
                                    }


                                    if (dragJunction != null)
                                    {
                                        RecordObject(dragJunction, "BH.S3.EndDragSplinePoint");
                                        GameObject splinePointGO = dropSpline.GetPoint(dropPointIndex);
                                        if (splinePointGO != null)
                                        {
                                            SplinePointBase splinePoint = splinePointGO.GetComponent<SplinePointBase>();
                                            if (splinePoint != null)
                                            {
                                                RegisterFullObjectHierarchyUndo(splinePointGO, "BH.S3.EndDragSplinePoint");
                                            }
                                        }
                                    }

                                    dropSpline.Connect(dropPointIndex, m_dragSpline, m_dragPointIndex);

                                    if (dragJunction != null && dropJunction != null)
                                    {
                                        DestroyObject(dragJunction.gameObject);
                                    }

                                    if (dropJunction == null)
                                    {
                                        dropJunction = dropSpline.GetJunction(dropPointIndex);
                                        if (dropJunction.ConnectionsCount == 2)
                                        {
                                            RegisterCreatedObjectUndo(dropJunction.gameObject, "BH.S3.EndDragSplinePoint");
                                        }
                                    }
                                }   
                            }
                        }
                        else
                        {
                            GameObject splinePoint = eventArgs.Spline.GetPoint(eventArgs.PointIndex);
                            Select(splinePoint);
                        }
                        
                        EndDrag();
                    }
                    break;
                case SplineEditorEvent.JunctionClick:
                    {
                        Select(eventArgs.Junction.gameObject);
                    }
                    break;
                case SplineEditorEvent.JunctionDrag:
                    {
                        if (!m_beforeJunctionDrag && !m_junctionDrag)
                        {
                            m_dragJunction = eventArgs.Junction;
                            GetDragPlane(m_dragJunction.transform.position);
                            if (GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out m_beginDragPosition))
                            {
                                m_beforeJunctionDrag = true;
                            }
                            else
                            {
                                m_dragJunction = null;
                            }
                        }
                    }
                    break;
            }

            if (m_beforeSplinePointCreatorDrag)
            {
                if(BeginDragSplinePoint())
                {
                    m_beforeSplinePointCreatorDrag = false;
                    m_splinePointCreatorDrag = true;
                }
            }

            if(m_splinePointCreatorDrag)
            {
                DragSplinePoint();
            }

            if(m_splinePointDrag)
            {
                DragSplinePointUsingOffset();
            }

            if(m_splineCtrlPointDrag)
            {
                DragSplineCtrlPoint();
            }

            if(m_beforeJunctionDrag)
            {
                if (BeginDragJunction())
                {
                    m_beforeJunctionDrag = false;
                    m_junctionDrag = true;
                }
            }

            if(m_junctionDrag)
            {
                DragSplinePoint();
            }
        }
   
        private bool BeginDragSplinePoint()
        {
            Vector3 position;
            if (GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out position))
            {
                Vector3 offset = position - m_dragSpline.GetPointPosition(m_dragPointIndex);
                const float s = 0.1f;
                if (offset.magnitude > HandleSize(m_beginDragPosition) * s)
                {
                    JunctionBase junction = m_dragSpline.GetJunction(m_dragPointIndex);

                    if (m_dragPointIndex == 0 && junction == null)
                    {
                        m_dragSpline.Prepend(position);

                        GameObject splinePoint = m_dragSpline.GetPoint(0);

                        RegisterCreatedObjectUndo(splinePoint, "BH.S3.Prepend");
                        SetTransformParentUndo(splinePoint.transform, splinePoint.transform.parent, "BH.S3.Prepend");

                    }
                    else if (m_dragPointIndex == m_dragSpline.PointsCount - 1 && junction == null)
                    {
                        m_dragSpline.Append(position);
                        m_dragPointIndex = m_dragSpline.PointsCount - 1;
                        RegisterCreatedObjectUndo(m_dragSpline.GetPoint(m_dragPointIndex), "BH.S3.Append");
                    }
                    else
                    {
                        Vector3 dir;
                        if(m_dragSpline.CurveCount == m_dragPointIndex)
                        {
                            dir = m_dragSpline.GetDirection(1.0f);
                        }
                        else
                        {
                            dir = m_dragSpline.GetDirection(0, m_dragPointIndex);
                        }
                        
                        bool isOut = Mathf.Sign(Vector3.Dot(offset.normalized, dir)) >= 0;
                        int connectionIndex = m_dragSpline.CreateBranch(m_dragPointIndex, isOut);
                        
                        junction = m_dragSpline.GetJunction(m_dragPointIndex);

                        m_dragSpline = junction.GetSpline(connectionIndex);
                        RegisterCreatedObjectUndo(m_dragSpline.gameObject, "BH.S3.Branch");

                        if (junction.ConnectionsCount == 2)
                        {
                            m_newJunction = junction;
                            RegisterCreatedObjectUndo(junction.gameObject, "BH.S3.Branch");
                        }

                        m_splines = m_splines.Add(m_dragSpline);

                        SplineRenderer splineRenderer = m_dragSpline.GetComponent<SplineRenderer>();
                        m_splineRenderers = m_splineRenderers.Add(splineRenderer);

                        if (splineRenderer != null)
                        {
                            splineRenderer.IsSelected = true;
                        }

                        m_dragPointIndex = isOut ? 1 : 0;
                    }

                    return true;
                }  
            }
            return false;
        }

        private void DragSplinePoint()
        {
            Vector3 position;
            if (GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out position))
            {
                Vector3 prevPosition = m_dragSpline.GetPointPosition(m_dragPointIndex);
                Vector3 offset = position - prevPosition;
                float gridSize = GetGridSize();
                if (IsUnitSnapping())
                {
                    Quaternion quat = Quaternion.LookRotation(m_dragPlane.normal);
                    Vector3 planeOffset = Quaternion.Inverse(quat) * offset;

                    planeOffset.x = planeOffset.x > 0 ? Mathf.Floor(planeOffset.x / gridSize) * gridSize : Mathf.Ceil(planeOffset.x / gridSize) * gridSize;
                    planeOffset.y = planeOffset.y > 0 ? Mathf.Floor(planeOffset.y / gridSize) * gridSize : Mathf.Ceil(planeOffset.y / gridSize) * gridSize;

                    if (planeOffset.x != 0 || planeOffset.y != 0)
                    {
                        offset = quat * planeOffset;
                    }
                    else
                    {
                        offset = Vector3.zero;
                    }
                }

                m_dragSpline.SetPointPosition(m_dragPointIndex, prevPosition + offset);
                SetDragPointRotation();
            }
        }

        private void DragSplineCtrlPoint()
        {
            Vector3 position;
            if(GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out position))
            {
                Vector3 prevPosition = m_dragSpline.GetCtrlPointPosition(m_dragPointIndex, m_dragCtrlPointIndex);
                Vector3 offset = position - prevPosition;
                float gridSize = GetGridSize();
                if (IsUnitSnapping())
                {
                    Quaternion quat = Quaternion.LookRotation(m_dragPlane.normal);
                    Vector3 planeOffset = Quaternion.Inverse(quat) * offset;

                    planeOffset.x = planeOffset.x > 0 ? Mathf.Floor(planeOffset.x / gridSize) * gridSize : Mathf.Ceil(planeOffset.x / gridSize) * gridSize;
                    planeOffset.y = planeOffset.y > 0 ? Mathf.Floor(planeOffset.y / gridSize) * gridSize : Mathf.Ceil(planeOffset.y / gridSize) * gridSize;

                    if (planeOffset.x != 0 || planeOffset.y != 0)
                    {
                        offset = quat * planeOffset;
                    }
                    else
                    {
                        offset = Vector3.zero;
                    }
                }
                
                m_dragSpline.SetCtrlPointPosition(m_dragPointIndex, m_dragCtrlPointIndex, prevPosition + offset);
            }
        }

        private void SetDragPointRotation()
        {
            if (m_splinePointCreatorDrag || IsRotate())
            {
                if (m_dragSpline.PointsCount > 1)
                {
                    JunctionBase junction = m_dragSpline.GetJunction(m_dragPointIndex);
                    if (junction != null)
                    {
                        SplineBase closestSpline = null;
                        int closestPointIndex = -1;
                        int closestPointIndex2 = -1;

                        int ctrlPointIndex = -1;
                        float minMag = float.MaxValue;

                        int connectionsCount = junction.ConnectionsCount;
                        for (int i = 0; i < connectionsCount; ++i)
                        {
                            SplineBase spline = junction.GetSpline(i);
                            int pointIndex = junction.GetSplinePointIndex(i);
                  

                            if (pointIndex == spline.PointsCount - 1)
                            {
                                float mag = (spline.GetPointPosition(pointIndex) - spline.GetPointPosition(pointIndex - 1)).sqrMagnitude;
                                if (mag < minMag)
                                {
                                    ctrlPointIndex = 1;
                                    closestPointIndex = pointIndex;
                                    closestPointIndex2 = pointIndex - 1;
                                    closestSpline = spline;
                                    minMag = mag;
                                }

                            }
                            else if (pointIndex == 0)
                            {
                                float mag = (spline.GetPointPosition(pointIndex) - spline.GetPointPosition(pointIndex + 1)).sqrMagnitude;
                                if (mag < minMag)
                                {
                                    ctrlPointIndex = 0;
                                    closestPointIndex = pointIndex;
                                    closestPointIndex2 = pointIndex + 1;
                                    closestSpline = spline;
                                    minMag = mag;
                                }
                            }
                            else
                            {
                                float mag1 = (spline.GetPointPosition(pointIndex) - spline.GetPointPosition(pointIndex - 1)).sqrMagnitude;
                                float mag2 = (spline.GetPointPosition(pointIndex) - spline.GetPointPosition(pointIndex + 1)).sqrMagnitude;
                                if (mag1 < mag2)
                                {
                                    if (mag1 < minMag)
                                    {
                                        ctrlPointIndex = 1;
                                        closestPointIndex = pointIndex;
                                        closestPointIndex2 = pointIndex - 1;
                                        closestSpline = spline;
                                        minMag = mag1;
                                    }
                                }
                                else
                                {
                                    if (mag2 < minMag)
                                    {
                                        ctrlPointIndex = 0;
                                        closestPointIndex = pointIndex;
                                        closestPointIndex2 = pointIndex + 1;
                                        closestSpline = spline;
                                        minMag = mag2;
                                    }
                                }
                            }
                        }

                        if (closestSpline != null)
                        {
                            Quaternion rotation = GetDragPointRotation(closestSpline, closestPointIndex, closestPointIndex2, ctrlPointIndex);
                            closestSpline.SetPointRotation(closestPointIndex, rotation);
                        }
                    }
                    else
                    {
                        if (m_dragPointIndex == 0)
                        {
                            Quaternion rotation = GetDragPointRotation(m_dragSpline, m_dragPointIndex, m_dragPointIndex + 1, 0);
                            m_dragSpline.SetPointRotation(m_dragPointIndex, rotation);
                        }
                        else if (m_dragPointIndex == m_dragSpline.PointsCount - 1)
                        {
                            Quaternion rotation = GetDragPointRotation(m_dragSpline, m_dragPointIndex, m_dragPointIndex - 1, 1);
                            m_dragSpline.SetPointRotation(m_dragPointIndex, rotation);
                        }
                        else
                        {
                            Vector3 prevPosition = m_dragSpline.GetPointPosition(m_dragPointIndex - 1);
                            Vector3 nextPosition = m_dragSpline.GetPointPosition(m_dragPointIndex + 1);
                            Vector3 position = m_dragSpline.GetPointPosition(m_dragPointIndex);

                            float toPrevMag = (prevPosition - position).sqrMagnitude;
                            float toNextMag = (nextPosition - position).sqrMagnitude;
                            if (toNextMag >= toPrevMag)
                            {
                                Quaternion r1 = GetDragPointRotation(m_dragSpline, m_dragPointIndex, m_dragPointIndex - 1, 1);
                                m_dragSpline.SetPointRotation(m_dragPointIndex, r1);
                            }
                            else
                            {
                                Quaternion r2 = GetDragPointRotation(m_dragSpline, m_dragPointIndex, m_dragPointIndex + 1, 0);
                                m_dragSpline.SetPointRotation(m_dragPointIndex, r2);
                            }
                        }
                    }
                }
            }
        }

        private Quaternion GetDragPointRotation(SplineBase spline, int index, int nextIndex, int ctrlPointIndex)
        {
            Vector3 nextCtrlPointPosition =
                spline.InterpolationMode == 0 ?
                spline.GetCtrlPointPosition(nextIndex, ctrlPointIndex) :
                spline.GetPointPosition(nextIndex);

            Vector3 toNextSplinePoint = index < nextIndex ?
                nextCtrlPointPosition - spline.GetPointPosition(index) :
                spline.GetPointPosition(index) - nextCtrlPointPosition;

            GameObject ctrlPoint = index < nextIndex ? spline.GetCtrlPoint(index, 1) : spline.GetCtrlPoint(index, 0);
            Vector3 v = index < nextIndex ? Vector3.forward : Vector3.back;

            if (toNextSplinePoint.magnitude > MinMag)
            {
                Quaternion rotation;
                if (ctrlPoint != null)
                {
                    Quaternion rot = Quaternion.FromToRotation(ctrlPoint.transform.localPosition.normalized, v);
                    rotation = Quaternion.LookRotation(toNextSplinePoint) * rot;
                    
                }
                else
                {
                    rotation = Quaternion.LookRotation(toNextSplinePoint);
                }

                return rotation;
            }
            return spline.GetPointRotation(index);
        }

        private void DragSplinePointUsingOffset()
        {
            Vector3 position;
            if (GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out position))
            {
                Vector3 offset = position - m_beginDragPosition;

                float gridSize = GetGridSize();
                if (IsUnitSnapping())
                {
                    Quaternion quat = Quaternion.LookRotation(m_dragPlane.normal);
                    Vector3 planeOffset = Quaternion.Inverse(quat) * offset;
                    
                    planeOffset.x = planeOffset.x > 0 ? Mathf.Floor(planeOffset.x / gridSize) * gridSize : Mathf.Ceil(planeOffset.x / gridSize) * gridSize;
                    planeOffset.y = planeOffset.y > 0 ? Mathf.Floor(planeOffset.y / gridSize) * gridSize : Mathf.Ceil(planeOffset.y / gridSize) * gridSize;

                    if (planeOffset.x != 0 || planeOffset.y != 0)
                    {
                        offset = quat * planeOffset;
                        m_beginDragPosition = m_beginDragPosition + offset;
                    }
                    else
                    {
                        offset = Vector3.zero;
                    }
                }
                else
                {
                    m_beginDragPosition = position;
                }


                m_dragSpline.SetPointPosition(m_dragPointIndex, m_dragSpline.GetPointPosition(m_dragPointIndex) + offset);
                SetDragPointRotation();
            }
        }

    
        private bool BeginDragJunction()
        {
            Vector3 position;
            if (GetPositionOnDragPlane(ScreenPointToRay(MousePosition), out position))
            {
                Vector3 offset = position - m_dragJunction.transform.position;
                const float s = 0.1f;
                if (offset.magnitude > HandleSize(m_beginDragPosition) * s)
                {
                    int connectionsCount = m_dragJunction.ConnectionsCount;
                    SplineBase disconnectSpline = null;
                    int disconnectSplinePointIndex = -1;
                    float maxDot = 0;
                    for(int i = 0; i < connectionsCount; ++i)
                    {
                        SplineBase spline = m_dragJunction.GetSpline(i);
                        int splinePointIndex = m_dragJunction.GetSplinePointIndex(i);


                        float dot;
                        if (splinePointIndex == spline.CurveCount)
                        {
                            dot = Vector3.Dot(offset.normalized, -spline.GetDirection(0.9f));
                        }
                        else if(splinePointIndex == 0)
                        {
                            dot = Vector3.Dot(offset.normalized, spline.GetDirection(0.1f, splinePointIndex));
                        }
                        else
                        {
                            dot = Mathf.Max(
                                    Vector3.Dot(offset.normalized, spline.GetDirection(0.1f, splinePointIndex)),
                                    Vector3.Dot(offset.normalized, -spline.GetDirection(0.9f, splinePointIndex - 1)));
                        }

                        if(dot > maxDot)
                        {
                            maxDot = dot;
                            disconnectSpline = spline;
                            disconnectSplinePointIndex = splinePointIndex;
                        }
                    }
                    
                    if(disconnectSpline != null && disconnectSplinePointIndex != -1)
                    {
                        RecordObject(m_dragJunction, "BH.S3.JunctionDisconnect");
                      
                        m_dragPointIndex = disconnectSplinePointIndex;
                        m_dragSpline = disconnectSpline;

                        GameObject splinePointGO = m_dragSpline.GetPoint(m_dragPointIndex);
                        if (splinePointGO != null)
                        {
                            RecordObject(splinePointGO.transform, "BH.S3.JunctionDisconnect");

                            SplinePointBase splinePoint = splinePointGO.GetComponent<SplinePointBase>();
                            if(splinePoint != null)
                            {
                                RecordObject(splinePoint, "BH.S3.JunctionDisconnect");
                            }
                        }

                        m_dragJunction.Disconnect(disconnectSpline, disconnectSplinePointIndex);

                        return true;
                    }
                }
            }
            return false;
        }

        private void EndDrag()
        {
            if(m_junctionDrag)
            {
                if (m_dragJunction.ConnectionsCount < 2)
                {
                    DestroyObject(m_dragJunction.gameObject);
                }
            }
           
            m_beforeSplinePointCreatorDrag = false;
            m_splinePointCreatorDrag = false;

            m_splinePointDrag = false;

            m_beforeJunctionDrag = false;
            m_junctionDrag = false;

            m_splineCtrlPointDrag = false;

            m_newJunction = null;
            m_dragSpline = null;
            m_dragPointIndex = -1;
            m_dragCtrlPointIndex = -1;
            m_dragJunction = null;
        }
        
        private void SnapToGrid()
        {
            for(int i = 0; i < m_selectedSplines.Length; ++i)
            {
                SplineBase spline = m_selectedSplines[i];
                if(spline != null)
                {
                    SnapToGrid(spline.transform);
                }
            }

            for(int i = 0; i < m_selectedJunctions.Length; ++i)
            {
                JunctionBase junction = m_selectedJunctions[i];
                if(junction != null)
                {
                    SnapToGrid(junction.transform);
                }
            }

            for(int i = 0; i < m_selectedSplinePoints.Length; ++i)
            {
                SplinePointBase splinePoint = m_selectedSplinePoints[i];
                if(splinePoint != null)
                {
                    SnapToGrid(splinePoint.transform);
                }
            }

            for(int i = 0; i < m_selectedBezierPoints.Length; ++i)
            {
                ControlPoint ctrlPoint = m_selectedBezierPoints[i];
                if(ctrlPoint != null)
                {
                    SnapToGrid(ctrlPoint.transform);
                }
            }
        }

        private void SnapToGrid(Transform objectTransform)
        {
            RecordObject(objectTransform, "BH.S3.SnapToGrid");
            float gridSize = GetGridSize();
            Vector3 position = objectTransform.position;
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.y = Mathf.Round(position.y / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            objectTransform.position = position;
        }
    }   
}
