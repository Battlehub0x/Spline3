using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.Spline3
{
    public static class ArrayExt
    {
        public static T[] RemoveAt<T>(this T[] arr, int index)
        {
            for(int i = index; i < arr.Length - 1; ++i)
            {
                arr.SetValue(arr.GetValue(i + 1), i);
            }
            Array.Resize(ref arr, arr.Length - 1);
            return arr;
        }

        public static T[] Insert<T>(this T[] arr, T value, int index)
        {
            Array.Resize(ref arr, arr.Length + 1);
            for(int i = arr.Length - 2; i >= index ; i--)
            {
                arr.SetValue(arr.GetValue(i), i + 1);
            }
            arr.SetValue(value, index);
            return arr;
        }

        public static T[] Add<T>(this T[] arr, T value)
        {
            Array.Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = value;
            return arr;
        }
    }

    public interface IJunctionInternalAccessor
    {
        void Add(SplinePointBase point);
        void Remove(SplinePointBase point);
    }

    [ExecuteInEditMode]
    public abstract class JunctionBase : MonoBehaviour, IJunctionInternalAccessor
    {
        public abstract int ConnectionsCount
        {
            get;
        }

        public abstract Vector3 Position
        {
            get;
            set;
        }

        public abstract Quaternion Rotation
        {
            get;
            set;
        }

        private void Awake()
        {
            Unselect();
            AwakeOverride();
        }

        private void Start()
        {
            StartOverride();
        }

        private void Update()
        {
            UpdateOverride();
        }

        private void OnDestroy()
        {
            OnDestroyOverride();
        }

        protected virtual void AwakeOverride()
        {

        }

        protected virtual void StartOverride()
        {

        }

        protected virtual void UpdateOverride()
        {

        }

        protected virtual void OnDestroyOverride()
        {
            
        }

        public void Select()
        {
            enabled = true;
        }

        public void Unselect()
        {
            enabled = false;
        }

        public abstract SplineBase GetSpline(int connectionIndex);
        public abstract int GetSplinePointIndex(int connectionIndex);
        public abstract bool IsOut(int connectionIndex);
        public abstract bool IsIn(int connectionIndex);
        public abstract void Connect(SplineBase spline, int pointIndex);
        public abstract void Disconnect(SplineBase spline, int pointIndex);
        public abstract void Disconnect(SplineBase spline);
        public abstract void Disconnect();

        public int FirstOut()
        {
            int connectionCount = ConnectionsCount;
            for(int i = 0; i < connectionCount; ++i)
            {
                if(IsOut(i))
                {
                    return i;
                }
            }
            return -1;
        }

        public int FirstIn()
        {
            int connectionCount = ConnectionsCount;
            for (int i = 0; i < connectionCount; ++i)
            {
                if (IsIn(i))
                {
                    return i;
                }
            }
            return -1;
        }

        public int[] GetOutputs()
        {
            int connectionCount = ConnectionsCount;
            List<int> outputs = new List<int>();
            for (int i = 0; i < connectionCount; ++i)
            {
                if (IsOut(i))
                {
                    outputs.Add(i);
                }
            }
            return outputs.ToArray();
        }

        public int[] GetInputs()
        {
            int connectionCount = ConnectionsCount;
            List<int> inputs = new List<int>();
            for (int i = 0; i < connectionCount; ++i)
            {
                if (IsIn(i))
                {
                    inputs.Add(i);
                }
            }
            return inputs.ToArray();
        }

        void IJunctionInternalAccessor.Add(SplinePointBase point)
        {
            AddInternal(point);
        }

        protected abstract void AddInternal(SplinePointBase point);

        void IJunctionInternalAccessor.Remove(SplinePointBase point)
        {
            RemoveInternal(point);
        }

        protected abstract void RemoveInternal(SplinePointBase point);
    }

  

    public class Junction : JunctionBase
    {
        [SerializeField]
        private SplinePointBase[] Connections = new SplinePointBase[0];

        private SplinePointBase[] m_connections = new SplinePointBase[0];

        public override Vector3 Position
        {
            get { return transform.position; }
            set
            {
                if(transform.position != value)
                {
                    transform.position = value;
                    SetJunctions();
                    UpdateConnectionPositions();
                }
            }
        }

        public override Quaternion Rotation
        {
            get { return transform.rotation; }
            set
            {
                if(transform.rotation != value)
                {
                    transform.rotation = value;
                    SetJunctions();
                    UpdateConnectionRotations();
                }
            }
        }

        public override int ConnectionsCount
        {
            get { return m_connections.Length; }
        }

        private bool m_isEnabled;
        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            m_isEnabled = enabled;
            enabled = true;
        }

        protected override void StartOverride()
        {
            base.StartOverride();
            SetJunctions();
            UpdateConnectionPositions();
            UpdateConnectionRotations();
            enabled = m_isEnabled;
        }

        protected override void UpdateOverride()
        {
            base.UpdateOverride();
            SetJunctions();
            UpdateConnectionPositions();
            UpdateConnectionRotations();
        }

        private void SetJunctions()
        {
            if (m_connections != Connections)
            {
                m_connections = Connections;
                for (int i = 0; i < m_connections.Length; ++i)
                {
                    if (m_connections[i] != null)
                    {
                        m_connections[i].SetJunction(this);
                    }
                }
            }
        }

        private void UpdateConnectionPositions()
        {
            for (int i = 0; i < m_connections.Length; ++i)
            {
                SplinePointBase splinePoint = m_connections[i];
                if (splinePoint.Position != transform.position)
                {
                    splinePoint.Position = transform.position;
                }
            }
        }

        private void UpdateConnectionRotations()
        {
            for (int i = 0; i < m_connections.Length; ++i)
            {
                SplinePointBase splinePoint = m_connections[i];
                if (splinePoint.Rotation != transform.rotation)
                {
                    splinePoint.Rotation = transform.rotation;
                }
            }
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
        }

        public override void Connect(SplineBase spline, int pointIndex)
        {
            GameObject pointGO = spline.GetPoint(pointIndex);
            SplinePointBase splinePoint = pointGO.GetComponent<SplinePointBase>();
            
            if(Array.IndexOf(Connections, splinePoint) < 0)
            {
                splinePoint.SetJunction(this);
                
                Array.Resize(ref Connections, Connections.Length + 1);
                Connections[Connections.Length - 1] = splinePoint;
                m_connections = Connections;
            }
            
        }

        public override void Disconnect(SplineBase spline, int pointIndex)
        {
            GameObject pointGO = spline.GetPoint(pointIndex);
            SplinePointBase splinePoint = pointGO.GetComponent<SplinePointBase>();
            
            int index = Array.IndexOf(Connections, splinePoint);
            if(index >= 0)
            {
                if (splinePoint.GetJunction() == this)
                {
                    splinePoint.SetJunction(null);
                }
                Connections = Connections.RemoveAt(index);
                m_connections = Connections;
            }
        }

        public override void Disconnect(SplineBase spline)
        {
            for(int i = Connections.Length - 1; i >= 0; i--)
            {
                SplinePointBase splinePoint = Connections[i];
                if(splinePoint.Spline == spline)
                {
                    if (splinePoint.GetJunction() == this)
                    {
                        splinePoint.SetJunction(null);
                    }
                    Connections = Connections.RemoveAt(i);
                    m_connections = Connections;
                }
            }   
        }

        public override void Disconnect()
        {
            for (int i = Connections.Length - 1; i >= 0; i--)
            {
                SplinePointBase splinePoint = Connections[i];
                if(splinePoint != null)
                {
                    if(splinePoint.GetJunction() == this)
                    {
                        splinePoint.SetJunction(null);
                    }
                } 
            }
            Connections = new SplinePointBase[0];
            m_connections = Connections;
        }

        public override SplineBase GetSpline(int connectionIndex)
        {
            if(m_connections.Length <= connectionIndex || connectionIndex < 0)
            {
                return null;
            }

            return m_connections[connectionIndex].Spline;
        }

        public override int GetSplinePointIndex(int connectionIndex)
        {
            if (m_connections.Length <= connectionIndex || connectionIndex < 0)
            {
                return -1;
            }

            return m_connections[connectionIndex].Index;
        }

        public override bool IsIn(int connectionIndex)
        {
            return GetSplinePointIndex(connectionIndex) > 0;
        }

        public override bool IsOut(int connectionIndex)
        {
            SplineBase spline = GetSpline(connectionIndex);
            return GetSplinePointIndex(connectionIndex) < spline.PointsCount - 1;
        }

        protected override void AddInternal(SplinePointBase point)
        {
            int index = Array.IndexOf(Connections, point);
            if(index < 0)
            {
                Connections = Connections.Add(point);
                m_connections = Connections;
            }
        }

        protected override void RemoveInternal(SplinePointBase point)
        {
            int index = Array.IndexOf(Connections, point);
            if(index >= 0)
            {
                Connections = Connections.RemoveAt(index);
                m_connections = Connections;
            }
        }
    }


}
