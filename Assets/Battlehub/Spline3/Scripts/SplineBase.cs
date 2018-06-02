using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif

namespace Battlehub.Spline3
{
    [Serializable]
    public struct TwistData
    {
        public float Angle;
        public float T0;
        public float T1;
    }

    public enum BezierPointMode
    {
        Free,
        Aligned,
        Mirrored
    }

    [Serializable]
    public class ThicknessData
    {
        public Vector3 Thickness;
        public float T0;
        public float T1;
    }

    [Serializable]
    public class PointData : ICloneable
    {
        public JunctionBase Junction;
        public TwistData Twist;
        public int Mode;

        public PointData()
        {
            Twist = new TwistData { T0 = 0, T1 = 0 };
        }

        public object Clone()
        {
            PointData clone = new PointData();
            clone.Twist = Twist;
            clone.Mode = Mode;
            
            return clone;   
        }
    }

    public interface Interpolator<T> where T : PointData, new()
    {
        void Init(SplineBase<T> spline);
        Vector3 GetLocalPosition(SplineBase<T> spline, float t);
        Vector3 GetLocalPosition(SplineBase<T> spline, float t, int curve);
        Vector3 GetPosition(SplineBase<T> spline, float t);
        Vector3 GetPosition(SplineBase<T> spline, float t, int curve);
        Vector3 GetVelocity(SplineBase<T> spline, float t);
        Vector3 GetVelocity(SplineBase<T> spline, float t, int curve);
        Vector3 GetDirection(SplineBase<T> spline,  float t);
        Vector3 GetDirection(SplineBase<T> spline, float t, int curve);
        int ToCurveIndex(SplineBase<T> spline, float t, out float outT);

        void OnPointPositionChanged(SplineBase<T> spline, int index, Vector3 position);
        void OnPointLocalPositionChanged(SplineBase<T> spline, int index, Vector3 position);
        void OnPointModeChanged(SplineBase<T> spline, int index, int mode);
        void OnCtrlPointPositionChanged(SplineBase<T> spline, int index, int ctrlPointIndex, Vector3 position);
        void OnPointJunctionChanged(SplineBase<T> spline, int index);

        void OnPointCreated(SplineBase<T> spline, SplinePointBase<T> point);
        void OnPointInserted(SplineBase<T> spline, int index);

        int GetControlPointsCount(SplinePointBase<T> point);
    }

    public class BezierInterpolator<T> : Interpolator<T> where T : PointData, new()
    {
       

        public virtual void Init(SplineBase<T> spline)
        {
        }

        public virtual Vector3 GetLocalPosition(SplineBase<T> spline, float t)
        {
            int curveIndex = ToCurveIndex(spline, t, out t);
            return GetLocalPosition(spline, t, curveIndex);
        }
        public virtual Vector3 GetLocalPosition(SplineBase<T> spline, float t, int curve)
        {
            int pointIndex = curve;
            return CurveUtils.Bezier(
                spline.GetPointLocalPosition(pointIndex),
                spline.transform.InverseTransformPoint(spline.GetCtrlPointPosition(pointIndex, 1)),
                spline.transform.InverseTransformPoint(spline.GetCtrlPointPosition(pointIndex + 1, 0)),
                spline.GetPointLocalPosition(pointIndex + 1), t);
        }

        public virtual Vector3 GetPosition(SplineBase<T> spline, float t)
        {
            int curveIndex = ToCurveIndex(spline, t, out t);
            return GetPosition(spline, t, curveIndex);
        }

        public virtual Vector3 GetPosition(SplineBase<T> spline, float t, int curve)
        {
            int pointIndex = curve;
            return //spline.transform.TransformVector(
               CurveUtils.Bezier(
                   spline.GetPointPosition(pointIndex),
                   spline.GetCtrlPointPosition(pointIndex, 1),
                   spline.GetCtrlPointPosition(pointIndex + 1, 0),
                   spline.GetPointPosition(pointIndex + 1), t);//);
        }

        public virtual Vector3 GetVelocity(SplineBase<T> spline, float t)
        {
            int curveIndex = ToCurveIndex(spline, t, out t);
            return GetVelocity(spline, t, curveIndex);
        }

        public virtual Vector3 GetVelocity(SplineBase<T> spline, float t, int curve)
        {
            int pointIndex = curve;
            return //spline.transform.TransformVector(
                CurveUtils.BezierDerivative(
                    spline.GetPointPosition(pointIndex),
                    spline.GetCtrlPointPosition(pointIndex, 1),
                    spline.GetCtrlPointPosition(pointIndex + 1, 0),
                    spline.GetPointPosition(pointIndex + 1), t);//);
        }

        public virtual Vector3 GetDirection(SplineBase<T> spline, float t)
        {
            return GetVelocity(spline, t).normalized;
        }

        public virtual Vector3 GetDirection(SplineBase<T> spline, float t, int curve)
        {
            return GetVelocity(spline, t, curve).normalized;
        }

        public virtual int ToCurveIndex(SplineBase<T> spline, float t, out float outT)
        {
            int curveIndex;
            if (t >= 1f)
            {
                t = 1f;
                curveIndex = spline.CurveCount - 1;
            }
            else
            {
                t = Mathf.Clamp01(t) * spline.CurveCount;
                curveIndex = (int)t;
                t -= curveIndex;
            }

            outT = t;
            return curveIndex;
        }

        public virtual void OnPointPositionChanged(SplineBase<T> spline, int index, Vector3 position)
        {

        }

        public virtual void OnPointLocalPositionChanged(SplineBase<T> spline, int index, Vector3 position)
        {

        }

        public virtual void OnPointModeChanged(SplineBase<T> spline, int index, int mode)
        {
            #if UNITY_EDITOR

            if(!UnityEditor.Selection.Contains(spline.GetCtrlPoint(index, 0)) &&
               !UnityEditor.Selection.Contains(spline.GetCtrlPoint(index, 1)))
            {
                ForceControlPointMode(spline, index, 0);
            }

            #else
            ForceControlPointMode(spline, index, 0);
            #endif
        }

        public virtual void OnCtrlPointPositionChanged(SplineBase<T> spline, int index, int ctrlPointIndex, Vector3 position)
        {
            GameObject point = spline.GetPoint(index);
            GameObject ctrlPoint = spline.GetCtrlPoint(index, ctrlPointIndex);
            GameObject oppositeCtrlPoint = spline.GetCtrlPoint(index, (1 + ctrlPointIndex) % 2);

            #if UNITY_EDITOR
            if (Event.current != null && Event.current.rawType != EventType.MouseDrag || 
                !UnityEditor.Selection.Contains(ctrlPoint.gameObject) &&
                !UnityEditor.Selection.Contains(oppositeCtrlPoint.gameObject))
            {
                UpdateCtrlPointRotation(spline, index, ctrlPointIndex, position, point, ctrlPoint, oppositeCtrlPoint);
            }

            #else
            UpdateCtrlPointRotation(spline, index, ctrlPointIndex, position, point, ctrlPoint, oppositeCtrlPoint);
            #endif
            ForceControlPointMode(spline, index, ctrlPointIndex);
            ForceJunctionConnections(spline, index);
        }

        private static void UpdateCtrlPointRotation(SplineBase<T> spline, int index, int ctrlPointIndex, Vector3 position, GameObject point, GameObject ctrlPoint, GameObject oppositeCtrlPoint)
        {
            if (ctrlPoint != null && oppositeCtrlPoint != null && point != null)
            {
                Vector3 oppsiteCtrlPointPosition = oppositeCtrlPoint.transform.position;
                Vector3 toPoint = (point.transform.position - position);
                float twist = spline.GetPointTwistAngle(index);

                if (toPoint.magnitude < SplineBase.Eps)
                {
                    ctrlPoint.transform.rotation = Quaternion.identity;
                    spline.SetPointRotation(index, Quaternion.identity);
                }
                else
                {
                    toPoint.Normalize();
                    if (ctrlPointIndex == 0)
                    {
                        ctrlPoint.transform.rotation = Quaternion.AngleAxis(twist, toPoint) * Quaternion.LookRotation(toPoint);
                        spline.SetPointRotation(index, ctrlPoint.transform.rotation);
                        oppositeCtrlPoint.transform.position = oppsiteCtrlPointPosition;
                    }
                    else
                    {
                        if (index == 0)
                        {
                            Vector3 toOppositePoint = (point.transform.position - oppsiteCtrlPointPosition);
                            if(toOppositePoint.magnitude >= SplineBase.Eps)
                            {
                                spline.SetPointRotation(index, Quaternion.AngleAxis(twist, toOppositePoint) * Quaternion.LookRotation(toOppositePoint));
                            }
                        }

                        ctrlPoint.transform.rotation = Quaternion.AngleAxis(twist, -toPoint) * Quaternion.LookRotation(toPoint);
                    }
                }

                ctrlPoint.transform.position = position;
            }
        }

        private static void ForceJunctionConnections(SplineBase<T> spline, int index)
        {
            JunctionBase junction = spline.GetJunction(index);
            if (junction == null || junction.ConnectionsCount <= 1)
            {
                return;
            }

            Vector3 refPosition = spline.GetPointPosition(index);
            Vector3 refCtrl0Position = spline.GetCtrlPointPosition(index, 0);
            Vector3 refCtrl1Position = spline.GetCtrlPointPosition(index, 1);

            for (int i = 0; i < junction.ConnectionsCount; ++i)
            {
                SplineBase connectedSpline = junction.GetSpline(i);
                int pointIndex = junction.GetSplinePointIndex(i);
                if (connectedSpline != spline || pointIndex != index)
                {

                    if (refPosition != connectedSpline.GetPointPosition(pointIndex))
                    {
                        connectedSpline.SetPointPosition(pointIndex, refPosition);
                    }
                    if (refCtrl0Position != connectedSpline.GetCtrlPointPosition(pointIndex, 0))
                    {
                        connectedSpline.SetCtrlPointPosition(pointIndex, 0, refCtrl0Position);
                    }

                    if (refCtrl1Position != connectedSpline.GetCtrlPointPosition(pointIndex, 1))
                    {
                        connectedSpline.SetCtrlPointPosition(pointIndex, 1, refCtrl1Position);
                    }
                }
            }
        }

        private static void ForceControlPointMode(SplineBase<T> spline, int index, int ctrlPointIndex)
        {
            int mode = spline.GetPointMode(index);
            if (mode == (int)BezierPointMode.Free)
            {
                if (index == 0  || index == spline.PointsCount - 1)
                {
                    if(spline.GetJunction(index) == null)
                    {
                        UpdateOppositeCtrlPointPosition(spline, index, ctrlPointIndex);
                    }
                }
            }
            else
            {
                UpdateOppositeCtrlPointPosition(spline, index, ctrlPointIndex);
            }
        }

        private static void UpdateOppositeCtrlPointPosition(SplineBase<T> spline, int index, int ctrlPointIndex)
        {
            int oppositeCtrlPointIndex = (1 + ctrlPointIndex) % 2;

            Vector3 pointPosition = spline.GetPointPosition(index);
            Vector3 ctrlPointPosition = spline.GetCtrlPointPosition(index, ctrlPointIndex);
            Vector3 oppositeCtrlPointPosition;
            Vector3 tangent = ctrlPointPosition - pointPosition;
            int mode = spline.GetPointMode(index);
            if (mode == (int)BezierPointMode.Aligned)
            { 
                oppositeCtrlPointPosition = pointPosition - tangent.normalized * Vector3.Distance(pointPosition, spline.GetCtrlPointPosition(index, oppositeCtrlPointIndex));   
            }
            else
            {
                oppositeCtrlPointPosition = pointPosition - tangent;
            }

            if(tangent != Vector3.zero)
            {
                if (oppositeCtrlPointPosition != spline.GetCtrlPointPosition(index, oppositeCtrlPointIndex))
                {
                    spline.SetCtrlPointPosition(index, oppositeCtrlPointIndex, oppositeCtrlPointPosition);
                }

                GameObject point = spline.GetPoint(index);
                GameObject ctrlPoint = spline.GetCtrlPoint(index, ctrlPointIndex);
                GameObject oppositeCtrlPoint = spline.GetCtrlPoint(index, (1 + ctrlPointIndex) % 2);

                if (ctrlPoint != null && oppositeCtrlPoint != null && point != null)
                {
                    Vector3 toOpposite = point.transform.position - oppositeCtrlPoint.transform.position;
                    if (toOpposite.magnitude < SplineBase.Eps)
                    {
                        #if UNITY_EDITOR
                        if (UnityEditor.Selection.activeGameObject != oppositeCtrlPoint)
                        {
                            oppositeCtrlPoint.transform.rotation = Quaternion.identity;
                        }
                        #else
                        oppositeCtrlPoint.transform.rotation = Quaternion.identity;
                        #endif
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        if (UnityEditor.Selection.activeGameObject != oppositeCtrlPoint)
                        {
                            oppositeCtrlPoint.transform.rotation = Quaternion.LookRotation(toOpposite);
                        }
                        #else
                        oppositeCtrlPoint.transform.rotation = Quaternion.LookRotation(toOpposite);
                        #endif

                    }
                }
            }
        }

        public virtual void OnPointCreated(SplineBase<T> spline, SplinePointBase<T> point)
        {
            CreateBezierControlPoint(point, Vector3.back);
            CreateBezierControlPoint(point, Vector3.forward);
        }

        private static void CreateBezierControlPoint(SplinePointBase<T> point, Vector3 offset)
        {
            GameObject ctrlPoint = new GameObject();
            ctrlPoint.name = "ctrl point";
            ctrlPoint.transform.SetParent(point.transform, false);
            ctrlPoint.transform.localPosition = offset;
            ctrlPoint.transform.rotation = Quaternion.LookRotation(point.transform.position - ctrlPoint.transform.position);
            ctrlPoint.AddComponent<ControlPoint>();
        }

        public virtual void OnPointInserted(SplineBase<T> spline, int index)
        {
            if(spline.PointsCount <= 1)
            {
                return;
            }

            if(index == 0)
            {
                DuplicateSplinePointData(spline, index, index + 1);
            }
            else if(index == spline.PointsCount - 1)
            {
                DuplicateSplinePointData(spline, index, index - 1);
            }
        }

        private static void DuplicateSplinePointData(SplineBase<T> spline, int index, int nextIndex)
        {
            Vector3 nextPos = spline.GetPointPosition(nextIndex);
            Vector3 ctrl0Pos = spline.GetCtrlPointPosition(nextIndex, 0);
            Vector3 ctrl1Pos = spline.GetCtrlPointPosition(nextIndex, 1);
            object pointData = spline.GetPointData(nextIndex);

            Vector3 pos = nextPos; 
            spline.SetPointPosition(index, pos);
            spline.SetCtrlPointPosition(index, 0, pos + Vector3.back * (ctrl0Pos - nextPos).magnitude);
            spline.SetCtrlPointPosition(index, 1, pos + Vector3.forward * (ctrl1Pos - nextPos).magnitude);
            spline.SetPointData(index, pointData);
        }

        public virtual void OnPointJunctionChanged(SplineBase<T> spline, int index)
        {
            JunctionBase junction = spline.GetJunction(index);
            if (junction == null || junction.ConnectionsCount == 0)
            {
                return;
            }

            int refPointIndex = junction.GetSplinePointIndex(0);
            SplineBase refSpline = junction.GetSpline(0);
            
            spline.SetPointRotation(index, refSpline.GetPointRotation(refPointIndex));

            Vector3 refPosition = refSpline.GetPointPosition(refPointIndex);
            Vector3 refCtrl0Position = refSpline.GetCtrlPointPosition(refPointIndex, 0);
            Vector3 refCtrl1Position = refSpline.GetCtrlPointPosition(refPointIndex, 1);

            spline.SetPointPosition(index, refPosition);
            spline.SetCtrlPointPosition(index, 0, refCtrl0Position);
            spline.SetCtrlPointPosition(index, 1, refCtrl1Position);
        }

        public virtual int GetControlPointsCount(SplinePointBase<T> point)
        {
            return point.transform.childCount;
        }
    }

    public class CatmullRomInterpolator<T> : BezierInterpolator<T> where T : PointData, new()
    {
        public override void Init(SplineBase<T> spline)
        {
        }

        private void GetControlPoints(SplineBase<T> spline, float t, int curve, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
        {
            int i0 = curve - 1;
            int i1 = curve;
            int i2 = curve + 1;
            int i3 = curve + 2;

            JunctionBase j1 = spline.GetJunction(i1);
            if (j1 == null)
            {
                if (i0 < 0)
                {
                    p0 = spline.GetCtrlPointPosition(0, 0);
                }
                else
                {
                    p0 = spline.GetPointPosition(i0);
                }
            }
            else
            {
                int connectionsCount = j1.ConnectionsCount;
                int inConnectionsCount = 0;
                p0 = Vector3.zero;
                for (int i = 0; i < connectionsCount; ++i)
                {
                    if (j1.IsIn(i))
                    {
                        SplineBase inSpline = j1.GetSpline(i);
                        int inPointIndex = j1.GetSplinePointIndex(i) - 1;

                        p0 += inSpline.GetPointPosition(inPointIndex);
                        inConnectionsCount++;
                    }
                }

                if (inConnectionsCount > 0)
                {
                    p0 /= inConnectionsCount;
                }
                else
                {
                    p0 = spline.GetCtrlPointPosition(0, 0);
                }
            }

            p1 = spline.GetPointPosition(i1);
            p2 = spline.GetPointPosition(i2);
            JunctionBase j2 = spline.GetJunction(i2);
            if (j2 == null)
            {
                if (i3 >= spline.PointsCount)
                {
                    p3 = spline.GetCtrlPointPosition(spline.PointsCount - 1, 1);
                }
                else
                {
                    p3 = spline.GetPointPosition(i3);
                }
            }
            else
            {
                int connectionsCount = j2.ConnectionsCount;
                int outConnectionsCount = 0;
                p3 = Vector3.zero;
                for (int i = 0; i < connectionsCount; ++i)
                {
                    if (j2.IsOut(i))
                    {
                        SplineBase outSpline = j2.GetSpline(i);
                        int outPointIndex = j2.GetSplinePointIndex(i) + 1;

                        p3 += outSpline.GetPointPosition(outPointIndex);
                        outConnectionsCount++;
                    }
                }

                if (outConnectionsCount > 0)
                {
                    p3 /= outConnectionsCount;
                }
                else
                {
                    p3 = spline.GetCtrlPointPosition(spline.PointsCount - 1, 1);
                }
            }
        }

        public override Vector3 GetLocalPosition(SplineBase<T> spline, float t, int curve)
        {
            Vector3 p0;
            Vector3 p1;
            Vector3 p2;
            Vector3 p3;

            GetControlPoints(spline, t, curve, out p0, out p1, out p2, out p3);

            p0 = spline.transform.InverseTransformPoint(p0);
            p1 = spline.transform.InverseTransformPoint(p1);
            p2 = spline.transform.InverseTransformPoint(p2);
            p3 = spline.transform.InverseTransformPoint(p3);

            return CurveUtils.CatmullRom(p0, p1, p2, p3, t);
        }

        public override Vector3 GetPosition(SplineBase<T> spline, float t, int curve)
        {
            Vector3 p0;
            Vector3 p1;
            Vector3 p2;
            Vector3 p3;

            GetControlPoints(spline, t, curve, out p0, out p1, out p2, out p3);

            return CurveUtils.CatmullRom(p0, p1, p2, p3, t);
        }

        public override Vector3 GetVelocity(SplineBase<T> spline, float t, int curve)
        {
            Vector3 p0;
            Vector3 p1;
            Vector3 p2;
            Vector3 p3;

            GetControlPoints(spline, t, curve, out p0, out p1, out p2, out p3);

            return CurveUtils.CatmullRomDerivative(p0, p1, p2, p3, t);
        }

 
        public override int ToCurveIndex(SplineBase<T> spline, float t, out float outT)
        {
            return base.ToCurveIndex(spline, t, out outT);
        }

        public override void OnPointPositionChanged(SplineBase<T> spline, int index, Vector3 position)
        {
            UpdateRotationUsingTangent(spline, index, 0, 1);
            UpdateRotationUsingTangent(spline, index - 1, -1, -1);
        }

        public override void OnPointLocalPositionChanged(SplineBase<T> spline, int index, Vector3 position)
        {
            UpdateRotationUsingTangent(spline, index, 0, 1);
            UpdateRotationUsingTangent(spline, index - 1, -1, -1);
        }

        private void UpdateRotationUsingTangent(SplineBase spline, int pointIndex, int offset, int increment)
        {
            if(offset == -3 || offset == 3)
            {
                return;
            }

            if(pointIndex < 0 || pointIndex >= spline.PointsCount)
            {
                return;
            }

            Vector3 dir = pointIndex < spline.CurveCount ?
                spline.GetDirection(0.0f, pointIndex) :
                spline.GetDirection(1.0f, spline.CurveCount - 1);

            float twist = spline.GetPointTwistAngle(pointIndex);
            spline.SetPointRotation(pointIndex, Quaternion.AngleAxis(twist, dir) * Quaternion.LookRotation(dir));

            JunctionBase junction = spline.GetJunction(pointIndex);
            if(junction != null)
            {
                if(offset == 0)
                {
                    for(int i = 0; i < junction.ConnectionsCount; ++i)
                    {
                        SplineBase connectedSpline = junction.GetSpline(i);
                        int connectedPointIndex = junction.GetSplinePointIndex(i);
                        if(spline == connectedSpline && pointIndex == connectedPointIndex)
                        {
                            continue;
                        }

                        if(junction.IsIn(i))
                        {
                            UpdateRotationUsingTangent(connectedSpline, connectedPointIndex - 1, -1, -1);
                        }
                        
                        if(junction.IsOut(i))
                        {
                            UpdateRotationUsingTangent(connectedSpline, connectedPointIndex + 1, 1, 1);
                        }
                    }
                }
                else if(offset < 0)
                {
                    int[] inConnections = junction.GetInputs();
                    for(int i = 0; i < inConnections.Length; ++i)
                    {
                        SplineBase connectedSpline = junction.GetSpline(inConnections[i]);
                        int connectedPointIndex = junction.GetSplinePointIndex(inConnections[i]);
                        UpdateRotationUsingTangent(connectedSpline, connectedPointIndex - 1, offset - 1, -1);
                    }
                }
                else if(offset > 0)
                {
                    int[] outConnections = junction.GetOutputs();
                    for (int i = 0; i < outConnections.Length; ++i)
                    {
                        SplineBase connectedSpline = junction.GetSpline(outConnections[i]);
                        int connectedPointIndex = junction.GetSplinePointIndex(outConnections[i]);
                        UpdateRotationUsingTangent(connectedSpline, connectedPointIndex + 1, offset + 1, 1);
                    }
                }   
            }

            UpdateRotationUsingTangent(spline, pointIndex + increment, offset + increment, increment);
        }



        public override void OnPointModeChanged(SplineBase<T> spline, int index, int mode)
        {
            base.OnPointModeChanged(spline, index, mode);
        }

        public override void OnCtrlPointPositionChanged(SplineBase<T> spline, int index, int ctrlPointIndex, Vector3 position)
        {
            base.OnCtrlPointPositionChanged(spline, index, ctrlPointIndex, position);
        }

        public override void OnPointCreated(SplineBase<T> spline, SplinePointBase<T> point)
        {
            base.OnPointCreated(spline, point);
        }

        public override void OnPointInserted(SplineBase<T> spline, int index)
        {
            if (spline.PointsCount <= 1)
            {
                return;
            }

            if (index == 0)
            {
                DuplicateSplinePointData(spline, index, index + 1);
            }
            else if (index == spline.PointsCount - 1)
            {
                DuplicateSplinePointData(spline, index, index - 1);
            }
        }

        private static void DuplicateSplinePointData(SplineBase<T> spline, int index, int nextIndex)
        {
            Vector3 nextPos = spline.GetPointPosition(nextIndex);
            //Vector3 ctrl0Pos = spline.GetCtrlPointPosition(nextIndex, 0);
            //Vector3 ctrl1Pos = spline.GetCtrlPointPosition(nextIndex, 1);
            object pointData = spline.GetPointData(nextIndex);

            Vector3 pos = nextPos;
            spline.SetPointPosition(index, pos);
            spline.SetCtrlPointPosition(index, 0, pos + Vector3.back);
            spline.SetCtrlPointPosition(index, 1, pos + Vector3.forward);
            spline.SetPointData(index, pointData);
        }

        public override void OnPointJunctionChanged(SplineBase<T> spline, int index)
        {
            base.OnPointJunctionChanged(spline, index);
        }

        public override int GetControlPointsCount(SplinePointBase<T> point)
        {
            return base.GetControlPointsCount(point);
        }
    }

    /// <summary>
    /// SplineBase abstract class
    /// </summary>
    public abstract class SplineBase : MonoBehaviour
    {
        public const float Eps = 0.00001f;

        /// <summary>
        /// Count of points in spline including all additional control points;
        /// </summary>
        public abstract int PointsCount
        {
            get;
        }

        /// <summary>
        /// Count of curves in spline. (curve is a spline segment between two points)
        /// </summary>
        public abstract int CurveCount
        {
            get;
        }

        /// <summary>
        /// 0 - bezier, 1 - catmul-rom
        /// </summary>
        public abstract int InterpolationMode
        {
            get;
            set;
        }

        //LIFECYCLE METHODS
        private void Awake()
        {
            AwakeOverride();
        }

        private void Start()
        {
            StartOverride();
        }

        private void OnEnable()
        {
            OnEnableOverride();
        }

        private void OnDisable()
        {
            OnDisableOverride();
        }

        private void OnDestroy()
        {
            OnDestroyOverride();
        }

        private void OnApplicationQuit()
        {
            OnApplicationQuitOverride();
        }

        //LIFECYCLE METHOD OVERRIDES
        protected virtual void AwakeOverride() { }
        protected virtual void StartOverride() { }
        protected virtual void OnEnableOverride() { }
        protected virtual void OnDisableOverride() { }
        protected virtual void OnDestroyOverride() { }
        protected virtual void OnApplicationQuitOverride() { }
        protected virtual void OnScriptReloaded() { }

        /// <summary>
        /// Get Spline Point rotation by index
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>World Space rotation of SplinePoint</returns>
        public abstract Quaternion GetPointRotation(int index);

        /// <summary>
        /// Set Spline Point rotation by index
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="rotation">World Space rotation of SplinePoint</param>
        public abstract void SetPointRotation(int index, Quaternion rotation);

        /// <summary>
        /// Get Spline Point position by index
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>World Space position of Spline Point</returns>
        public abstract Vector3 GetPointPosition(int index);


        /// <summary>
        /// Set Spline Point position by index
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="position">World Space position of Spline Point</param>
        public abstract void SetPointPosition(int index, Vector3 position);

        /// <summary>
        /// Get Spline Point Local position by index
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>Local Space position of Spline Point</returns>
        public abstract Vector3 GetPointLocalPosition(int index);

        /// <summary>
        /// Set Spline Point Local position by index
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="position">Local Space position of Spline Point</param>
        public abstract void SetPointLocalPosition(int index, Vector3 position);

        /// <summary>
        /// Get Point mode by index
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>returns mode represented by integer value</returns>
        public abstract int GetPointMode(int index);

        /// <summary>
        /// Set Point mode by index
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="mode">mode represented by integer value</param>
        public abstract void SetPointMode(int index, int mode);

        /// <summary>
        /// Set Point mode for all points
        /// </summary>
        /// <param name="mode">mode</param>
        public abstract void SetPointMode(int mode);

        /// <summary>
        /// Get Point Twist Angle
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>angle in degrees</returns>
        public abstract float GetPointTwistAngle(int index);

        /// <summary>
        /// Set Point Twist Angle
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="angle">angle in degrees</param>
        public abstract void SetPointTwistAngle(int index, float angle);

        /// <summary>
        /// Get Point Twist T0 parameter
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>t0 param</returns>
        public abstract float GetPointTwistT0(int index);

        /// <summary>
        /// Set Point Twist T0 parameter
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="t0">[0,1]</param>
        public abstract void SetPointTwistT0(int index, float t0);

        /// <summary>
        /// Get Point Twist T1 parameter
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>t1 parameter</returns>
        public abstract float GetPointTwistT1(int index);

        /// <summary>
        /// Set Point Twist T1 parameter
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="t1">t1 parameter</param>
        public abstract void SetPointTwistT1(int index, float t1);

        /// <summary>
        /// Returns copy of point data by index
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>point data</returns>
        public abstract object GetPointData(int index);

        /// <summary>
        /// Set point data by index
        /// </summary>
        /// <param name="index">index</param>
        /// <param name="data">data</param>
        public abstract void SetPointData(int index, object data);

        /// <summary>
        /// Get Junction by index
        /// </summary>
        /// <param name="index">spline point index</param>
        /// <returns>junction</returns>
        public abstract JunctionBase GetJunction(int index);

        /// <summary>
        /// Set Junction by index
        /// </summary>
        /// <param name="index">spline point index</param>
        /// <param name="junction">junction</param>
        public abstract void SetJunction(int index, JunctionBase junction);

        /// <summary>
        /// Get Control Point Position of Spline Point with index using controlPointIndex
        /// </summary>
        /// <param name="index">index of Spline Point</param>
        /// <param name="ctrlPointIndex">index of Control Point</param>
        /// <returns></returns>
        public abstract Vector3 GetCtrlPointPosition(int index, int ctrlPointIndex);

        /// <summary>
        /// Set Control Point Position of Spline Point with index using controlPointIdex
        /// </summary>
        /// <param name="index"></param>
        /// <param name="ctrlPointIndex"></param>
        /// <param name="position"></param>
        public abstract void SetCtrlPointPosition(int index, int ctrlPointIndex, Vector3 position);

        /// <summary>
        /// Get Control Points count of Spline Point by index
        /// </summary>
        /// <param name="index">index of Spline Point</param>
        /// <returns>couunt of Control Points</returns>
        public abstract int GetCtrlPointsCount(int index);

        /// <summary>
        /// Check whether Spline Point has Control points
        /// </summary>
        /// <returns>returns true if has</returns>
        public abstract bool HasControlPoints(int index);
        
    
        /// <summary>
        /// Get interploated position in local space of spline
        /// </summary>
        /// <param name="t">this value might have different meaning. It depends on interpolator used (for BezierInterploator t value fall in range [0, 1])</param>
        /// <returns>Interploated position in local space</returns>
        public abstract Vector3 GetLocalPosition(float t);

        /// <summary>
        /// Get interploated position in local space of spline
        /// </summary>
        /// <param name="t">this value might have different meaning. It depends on interpolator used (for BezierInterploator t value fall in range [0, 1] where 0 means beginning of the curve and 1 means ending of the curve)</param>
        /// <param name="curve">curve index</param>
        /// <returns>Interploated position in local space</returns>
        public abstract Vector3 GetLocalPosition(float t, int curve);

        /// <summary>
        /// Get interploated position in world space
        /// </summary>
        public abstract Vector3 GetPosition(float t);

        /// <summary>
        /// Get interploated position in world space
        /// </summary>
        public abstract Vector3 GetPosition(float t, int curve);

        /// <summary>
        /// Get Velocity
        /// </summary>
        public abstract Vector3 GetVelocity(float t);
        /// <summary>
        /// Get Velocity
        /// </summary>
        public abstract Vector3 GetVelocity(float t, int curve);

        /// <summary>
        /// Get Normalized Velocity vector
        /// </summary>
        public abstract Vector3 GetDirection(float t);

        /// <summary>
        /// Get Normalized Velocity vector
        /// </summary>
        public abstract Vector3 GetDirection(float t, int curve);

        /// <summary>
        /// Get Curve Index using t parameter
        /// </summary>
        /// <param name="t">[0,1]</param>
        /// <returns>curve index</returns>
        public abstract int GetCurveIndex(float t);

        /// <summary>
        /// Get Curve Index using t parameter
        /// </summary>
        /// <param name="t">[0,1]</param>
        /// <param name="outT">[0,1] withing curve</param>
        /// <returns>curve index</returns>
        public abstract int GetCurveIndex(float t, out float outT);

        /// <summary>
        /// Get T parameter for point at pointIndex
        /// </summary>
        /// <param name="pointIndex">point index</param>
        /// <returns>[0, 1]</returns>
        public abstract float GetTAt(int pointIndex);

        /// <summary>
        /// Get Twist Angle in degrees
        /// </summary>
        /// <param name="t">[0,1]</param>
        /// <returns></returns>
        public abstract float GetTwistAngle(float t);

        /// <summary>
        /// Get Twist Angle in degress
        /// </summary>
        /// <param name="t">[0,1]</param>
        /// <param name="curve">[0, CurveCount - 1]</param>
        /// <returns></returns>
        public abstract float GetTwistAngle(float t, int curve);

        /// <summary>
        /// Get Approximate Spline Length
        /// </summary>
        /// <param name="deltaT"></param>
        /// <returns></returns>
        public float GetLength(float deltaT = 0.01f, float from = 0.0f, float to = 1.0f)
        {
            from = Math.Min(Math.Max(0, from), 1.0f);
            to = Math.Max(Math.Min(1.0f, to), from);

            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Min((to - from) / deltaT, 1000000), 1));
            float length = 0;            
            Vector3 prev = GetPosition(from);
            for (int i = 0; i < steps; ++i)
            {
                float t = from + (((float)(i + 1)) / steps) * (to - from);
                Vector3 next = GetPosition(t);
                length += (next - prev).magnitude;
                prev = next;
            }

            return length;
        }

        /// <summary>
        /// Get Approximate Curve Length
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="deltaT"></param>
        /// <returns></returns>
        public float GetLength(int curve, float deltaT = 0.01f, float from = 0.0f, float to = 1.0f)
        {
            from = Math.Min(Math.Max(0, from), 1.0f);
            to = Math.Max(Math.Min(1.0f, to), from);

            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Min((to - from) / deltaT, 1000000), 1));
            float length = 0;
            Vector3 prev = GetPosition(from, curve);
            for (int i = 0; i < steps; ++i)
            {
                float t = from + (((float)(i + 1)) / steps) * (to - from);
                Vector3 next = GetPosition(t, curve);
                length += (next - prev).magnitude;
                prev = next;
            }

            return length;
        }

        /// <summary>
        /// Creates branch at point
        /// </summary>
        /// <param name="index">spline point index</param>
        /// <returns>connection index</returns>
        public abstract int CreateBranch(int index, bool isOut);
        public abstract void Connect(int index, SplineBase branch, int branchPointIndex);
        public abstract GameObject GetPoint(int index);
        public abstract GameObject GetCtrlPoint(int index, int ctrlPointIndex);
        public abstract void SelectPoint(int index);
        public abstract void UnselectPoint(int index);
        public abstract void SelectCtrlPoint(int index, int ctrlPointIndex);
        public abstract void UnselectCtrPoint(int index, int ctrlPointIndex);
    
        public abstract void Insert(Vector3 position, int index);
        public abstract void Remove(int index);

        public void Append(Vector3 position)
        {
            Insert(position, PointsCount);
        }

        public void Prepend(Vector3 position)
        {
            Insert(position, 0);
        }

        public void UpdateJunctions()
        {
            int pointCount = PointsCount;
            for (int i = 0; i < PointsCount; ++i)
            {
                JunctionBase junction = GetJunction(i);
                if (junction != null)
                {
                    junction.Position = GetPointPosition(i);
                    junction.Rotation = GetPointRotation(i);
                }
            }
        }

        #if UNITY_EDITOR
        [DidReloadScripts(98)]
        private static void OnScriptsReloaded()
        {
            SplineBase[] splines = FindObjectsOfType<SplineBase>();
            for (int i = 0; i < splines.Length; ++i)
            {
                splines[i].OnScriptReloaded();
            }
        }
        #endif
    }

    public interface ISplineInternalsAccessor<T> where T : PointData, new()
    {
        void Insert(int index, SplinePointBase<T> point);
        void Remove(SplinePointBase<T> point);
    }

    public abstract class SplineBase<T> : SplineBase, ISplineInternalsAccessor<T> where T : PointData, new()
    {
        [SerializeField, HideInInspector]
        private bool m_createdFirstTime;
        private List<SplinePointBase<T>> m_points;

        public static readonly Interpolator<T> InterpolatorBezier = new BezierInterpolator<T>();
        public static readonly Interpolator<T> InterpolatorCatmul = new CatmullRomInterpolator<T>();
        private static readonly Interpolator<T>[] m_interpolators = new [] { InterpolatorBezier, InterpolatorCatmul };

        public override int PointsCount
        {
            get { return m_points.Count; }
        }

        public override int CurveCount
        {
            get { return m_points.Count - 1; }
        }

        public override int InterpolationMode
        {
            get { return m_interpolatorIndex; }
            set
            {
                m_interpolatorIndex = value;
                Interpolator = m_interpolators[m_interpolatorIndex];
            }
        }

        [SerializeField, HideInInspector]
        private int m_interpolatorIndex;
        private Interpolator<T> m_interpolator;
        protected virtual Interpolator<T> Interpolator
        {
            get { return m_interpolator; }
            set
            {
                if(m_interpolator != value)
                {
                    m_interpolator = value;
                    m_interpolatorIndex = Array.IndexOf(m_interpolators, m_interpolator);
                }
            }
        }

        private List<SplinePointBase<T>> GetPoints()
        {
            return new List<SplinePointBase<T>>(GetComponentsInChildren<SplinePointBase<T>>().OrderBy( sp => sp.transform.GetSiblingIndex()));
        }

        private void Init()
        {
            Interpolator = m_interpolators[m_interpolatorIndex];
            m_points = GetPoints();
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            Init();

            if (!m_createdFirstTime)
            {
                CreatePoint(transform.position - Vector3.forward * 2);
                CreatePoint(transform.position + Vector3.forward * 2);
                m_points = GetPoints();
             
                m_createdFirstTime = true;
            }
        }

        protected override void OnScriptReloaded()
        {
            base.OnScriptReloaded();
            Init();
        }

        protected abstract SplinePointBase<T> CreatePointOverride(Vector3 position);
        public SplinePointBase<T> CreatePoint(Vector3 position)
        {
            SplinePointBase<T> splinePoint = CreatePointOverride(position);
            Interpolator.OnPointCreated(this, splinePoint);
            return splinePoint;
        }

        public override Quaternion GetPointRotation(int index)
        {
            return m_points[index].transform.rotation;
        }

        public override void SetPointRotation(int index, Quaternion rotation)
        {
            SplinePointBase<T> splinePoint = m_points[index];

            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            splinePoint.transform.rotation = rotation;

            float angle = splinePoint.transform.eulerAngles.z;
            TwistData twistData = pointInternals.Data.Twist;
            twistData.Angle += Mathf.DeltaAngle(twistData.Angle, angle);
            pointInternals.Data.Twist = twistData;

            JunctionBase junction = GetJunction(index);
            if (junction != null)
            {
                junction.Rotation = rotation;
            }
        }

        public override Vector3 GetPointPosition(int index)
        {
            return m_points[index].transform.position;
        }

        public override void SetPointPosition(int index, Vector3 position)
        {
            m_points[index].transform.position = position;

            JunctionBase junction = GetJunction(index);
            if(junction != null)
            {
                junction.Position = m_points[index].transform.position;
            }

            Interpolator.OnPointPositionChanged(this, index, position);
        }

        public override Vector3 GetPointLocalPosition(int index)
        {
            return m_points[index].transform.localPosition;
        }

        public override void SetPointLocalPosition(int index, Vector3 position)
        {
            m_points[index].transform.localPosition = position;
            
            JunctionBase junction = GetJunction(index);
            if(junction != null)
            {
                junction.Position = m_points[index].transform.position;
            }

            Interpolator.OnPointLocalPositionChanged(this, index, position);
        }

        public override int GetPointMode(int index)
        {
            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            return pointInternals.Data.Mode;
        }

        public override void SetPointMode(int index, int mode)
        {
            JunctionBase junction = GetJunction(index);
            if(junction != null)
            {
                ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
                pointInternals.Data.Mode = mode;

                bool isFirstConnection = junction.GetSpline(0) == this && junction.GetSplinePointIndex(0) == index;
                if(isFirstConnection)
                {
                    int connectionsCount = junction.ConnectionsCount;
                    for (int i = 1; i < connectionsCount; ++i)
                    {
                        SplineBase spline = junction.GetSpline(i);
                        int splinePointIndex = junction.GetSplinePointIndex(i);
                        spline.SetPointMode(splinePointIndex, mode);
                    }
                    Interpolator.OnPointModeChanged(this, index, mode);
                }
            }       
            else
            {
                ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
                pointInternals.Data.Mode = mode;

                Interpolator.OnPointModeChanged(this, index, mode);
            }     
        }

        public override void SetPointMode(int mode)
        {
            for(int i = 0; i < m_points.Count; ++i)
            {
                SetPointMode(i, mode);
            }
        }

        public override object GetPointData(int index)
        {
            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            return pointInternals.Data.Clone();
        }

        public override void SetPointTwistAngle(int index, float angle)
        {
            SplinePointBase<T> splinePoint = m_points[index];
            ISplinePointBaseInternalAccessor<T> pointInternals = splinePoint;
            TwistData data = pointInternals.Data.Twist;
            data.Angle = angle;
            pointInternals.Data.Twist = data;

            Vector3 eulerAngles = splinePoint.transform.eulerAngles;
            eulerAngles.z = angle;
            splinePoint.transform.eulerAngles = eulerAngles;

            SetForAllConnections((s, i, v) =>
            {
                if (s.GetPointTwistAngle(i) != v)
                {
                    s.SetPointTwistAngle(i, v);
                }
            }, index, angle); //???? is it ok?
        }

        public override float GetPointTwistAngle(int index)
        {
            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            return pointInternals.Data.Twist.Angle;
        }

        public override float GetPointTwistT0(int index)
        {
            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            return pointInternals.Data.Twist.T0;
        }

        public override void SetPointTwistT0(int index, float t0)
        {
            t0 = Mathf.Clamp01(t0);

            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            TwistData data = pointInternals.Data.Twist;

            data.T0 = t0;
            pointInternals.Data.Twist = data;

            int prevIndex = index - 1;
            if (prevIndex >= 0)
            {
                float t1 = GetPointTwistT1(prevIndex);
                if ((1 - t0) < t1)
                {
                    t1 = 1 - t0;
                    SetPointTwistT1(prevIndex, t1);
                }
            }


            SetForAllConnections((s, i, v) =>
            {
                if (s.GetPointTwistT0(i) != v)
                {
                    s.SetPointTwistT0(i, v);
                }
            }, index, t0);
        }

        public override float GetPointTwistT1(int index)
        {
            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            return pointInternals.Data.Twist.T1;
        }

        public override void SetPointTwistT1(int index, float t1)
        {
            t1 = Mathf.Clamp01(t1);

            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            TwistData data = pointInternals.Data.Twist;

            data.T1 = t1;
            pointInternals.Data.Twist = data;

            int nextIndex = index + 1;
            if (nextIndex < m_points.Count)
            {
                float t0 = GetPointTwistT0(nextIndex);
                if ((1 - t1) < t0)
                {
                    t0 = 1 - t1;
                    SetPointTwistT0(nextIndex, t0);
                }
            }

            SetForAllConnections((s, i, v) =>
            {
                if (s.GetPointTwistT1(i) != v)
                {
                    s.SetPointTwistT1(i, v);
                }
            }, index, t1);
        }

        public override void SetPointData(int index, object data)
        {
            T pointData = (T)data;

            SetPointMode(index, pointData.Mode);
            SetPointTwistAngle(index, pointData.Twist.Angle);
            SetPointTwistT0(index, pointData.Twist.T0);
            SetPointTwistT1(index, pointData.Twist.T1);
                            
            SetForAllConnections((s, i, v) => s.SetPointData(i, v), index, data);
        }


        private void SetForAllConnections<TVal>(Action<SplineBase, int, TVal> action, int index, TVal value)
        {
            JunctionBase junction = GetJunction(index);
            if (junction != null)
            {
                int connectionsCount = junction.ConnectionsCount;
                for (int i = 0; i < connectionsCount; ++i)
                {
                    SplineBase spline = junction.GetSpline(i);
                    int splinePointIndex = junction.GetSplinePointIndex(i);
                    if (spline == this && splinePointIndex == index)
                    {
                        continue;
                    }
                    action(spline, splinePointIndex, value);
                }
            }
        }

        public override JunctionBase GetJunction(int index)
        {
            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            return pointInternals.Data.Junction;
        }

        public override void SetJunction(int index, JunctionBase junction)
        {
            ISplinePointBaseInternalAccessor<T> pointInternals = m_points[index];
            if(pointInternals != null)
            {
                pointInternals.Data.Junction = junction;
                Interpolator.OnPointJunctionChanged(this, index);
            }
        }

        public override Vector3 GetCtrlPointPosition(int index, int ctrlPointIndex)
        {
            if(m_points[index].transform.childCount <= ctrlPointIndex)
            {
                return m_points[index].transform.position;
            }

            return m_points[index].transform.GetChild(ctrlPointIndex).position;
        }

        public override void SetCtrlPointPosition(int index, int ctrlPointIndex, Vector3 position)
        {
            if(m_points != null)
            {
                if (m_points[index].transform.childCount <= ctrlPointIndex)
                {
                    return;
                }

                m_points[index].transform.GetChild(ctrlPointIndex).position = position;
                Interpolator.OnCtrlPointPositionChanged(this, index, ctrlPointIndex, position);
            }
        }

        public override int GetCtrlPointsCount(int index)
        {
            return Interpolator.GetControlPointsCount(m_points[index]);
        }

        public override bool HasControlPoints(int index)
        {
            return Interpolator.GetControlPointsCount(m_points[index]) > 0;
        }

        public override Vector3 GetLocalPosition(float t)
        {
            return Interpolator.GetLocalPosition(this, t);
        }

        public override Vector3 GetLocalPosition(float t, int curve)
        {
            return Interpolator.GetLocalPosition(this, t, curve);
        }

        public override Vector3 GetPosition(float t)
        {
            return Interpolator.GetPosition(this, t);
        }

        public override Vector3 GetPosition(float t, int curve)
        {
            return Interpolator.GetPosition(this, t, curve);
        }

        public override Vector3 GetVelocity(float t)
        {
            return Interpolator.GetVelocity(this, t);
        }

        public override Vector3 GetVelocity(float t, int curve)
        {
            return Interpolator.GetVelocity(this, t, curve);
        }

        public override Vector3 GetDirection(float t)
        {
            return Interpolator.GetDirection(this, t);
        }

        public override Vector3 GetDirection(float t, int curve)
        {
            return Interpolator.GetDirection(this, t, curve);
        }

        public override int GetCurveIndex(float t)
        {
            return Interpolator.ToCurveIndex(this, t, out t);
        }

        public override int GetCurveIndex(float t, out float outT)
        {
            return Interpolator.ToCurveIndex(this, t, out outT);
        }

        public override float GetTAt(int pointIndex)
        {
            float p = pointIndex;
            return p / (PointsCount - 1);
        }

        public override float GetTwistAngle(float t)
        {
            int curveIndex = Interpolator.ToCurveIndex(this, t, out t);
            return GetTwistAngle(t, curveIndex);
        }

        public override float GetTwistAngle(float t, int curve)
        {
            ISplinePointBaseInternalAccessor<T> pinternals0 = m_points[curve];
            ISplinePointBaseInternalAccessor<T> pinternals1 = m_points[curve + 1];

            TwistData td0 = pinternals0.Data.Twist;
            TwistData td1 = pinternals1.Data.Twist;

            float t1 = Mathf.Clamp01(td0.T1);
            float t2 = 1.0f - Mathf.Clamp01(td1.T0);

            if (t <= t1)
            {
                t = 0.0f;
            }
            else if (t >= t2)
            {
                t = 1.0f;
            }
            else
            {
                t = Mathf.Clamp01((t - t1) / (t2 - t1));
            }
            return Mathf.Lerp(td0.Angle, td1.Angle, t);
        }

        public override GameObject GetPoint(int index)
        {
            return m_points[index].gameObject;
        }

        public override GameObject GetCtrlPoint(int index, int ctrlPointIndex)
        {
            if(m_points[index].transform.childCount <= ctrlPointIndex)
            {
                return null;
            }

            return m_points[index].transform.GetChild(ctrlPointIndex).gameObject;
        }

        public override void SelectPoint(int index)
        {
            if (m_points == null)
            {
                Init();
            }

            m_points[index].enabled = true;
        }

        public override void UnselectPoint(int index)
        {
            m_points[index].enabled = false;
        }

        public override void SelectCtrlPoint(int index, int ctrlPointIndex)
        {
            if(m_points == null)
            {
                Init();
            }

            MonoBehaviour mb = m_points[index].transform.GetChild(ctrlPointIndex).GetComponent<MonoBehaviour>();
            if(mb)
            {
                mb.enabled = true;
            }
        }
        public override void UnselectCtrPoint(int index, int ctrlPointIndex)
        {
            MonoBehaviour mb = m_points[index].transform.GetChild(ctrlPointIndex).GetComponent<MonoBehaviour>();
            if (mb)
            {
                mb.enabled = false;
            }
        }

        protected abstract JunctionBase CreateJunctionOverride();
        protected abstract SplineBase<T> CreateSplineOverride();

        public override int CreateBranch(int index, bool isOut)
        {
            JunctionBase junction = GetJunction(index);
            if(junction == null)
            {
                junction = CreateJunctionOverride();
                junction.transform.position = GetPointPosition(index);
                junction.transform.rotation = GetPointRotation(index);
                junction.Connect(this, index);
            }

            SplineBase<T> spline = CreateSplineOverride();

            spline.SetPointMode(GetPointMode(index));
            spline.InterpolationMode = InterpolationMode;

            spline.transform.position = transform.position;
            
            junction.Connect(spline, isOut ? 0 : 1);

            return junction.ConnectionsCount - 1;
        }

        public override void Connect(int index, SplineBase branch, int branchPointIndex)
        {
            if(branch == null)
            {
                throw new ArgumentNullException("branch");
            }

            if(branchPointIndex < 0 || branchPointIndex >= branch.PointsCount)
            {
                throw new ArgumentOutOfRangeException("branchPointIndex");
            }

            JunctionBase junction = GetJunction(index);
            JunctionBase branchJunction = branch.GetJunction(branchPointIndex);

            if(branchJunction == null)
            {
                if (junction == null)
                {
                    junction = CreateJunctionOverride();
                    junction.transform.position = GetPointPosition(index);
                    junction.transform.rotation = GetPointRotation(index);
                    junction.Connect(this, index);

                }
                junction.Connect(branch, branchPointIndex);
            }
            else
            {
                if(junction == null)
                {
                    branchJunction.Position = GetPointPosition(index);                    
                    branchJunction.Connect(this, index);
                }
                else
                {
                    int connectionsCount = branchJunction.ConnectionsCount;
                    for(int i = 0; i < connectionsCount; ++i)
                    {
                        SplineBase branchSpline = branchJunction.GetSpline(i);

                        int branchSplinePointIndex = branchJunction.GetSplinePointIndex(i);
                        junction.Connect(branchSpline, branchSplinePointIndex);
                    }
                    branchJunction.Disconnect();
                }
            }
        }

        public override void Insert(Vector3 position, int index)
        {
            SplinePointBase<T> point = CreatePointOverride(position);
            point.transform.SetSiblingIndex(index);
            Interpolator.OnPointCreated(this, point);

            if(m_points.Contains(point))
            {
                m_points.Remove(point);
            }
            m_points.Insert(index, point);

            Interpolator.OnPointInserted(this, index);
        }

        public override void Remove(int index)
        {
            SplinePointBase<T> point = m_points[index];
            if(point != null)
            {
                Destroy(point.gameObject);
            }

            m_points.RemoveAt(index);
        }

        void ISplineInternalsAccessor<T>.Insert(int index, SplinePointBase<T> point)
        {
            if(m_points != null && !m_points.Contains(point))
            {
                m_points.Insert(index, point);
            }    
        }

        void ISplineInternalsAccessor<T>.Remove(SplinePointBase<T> point)
        {
            m_points.Remove(point);
        }
    }
}

