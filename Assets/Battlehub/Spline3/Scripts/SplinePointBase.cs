
using UnityEngine;

namespace Battlehub.Spline3
{
    [ExecuteInEditMode]
    public abstract class SplinePointBase : MonoBehaviour
    {
        public Vector3 Position
        {
            get { return transform.position; }
            set
            { 
                transform.position = value;
                Spline.SetPointPosition(Index, value);
            }
        }

        public Quaternion Rotation
        {
            get { return transform.rotation; }
            set { Spline.SetPointRotation(Index, value); }
        }
        
        public abstract SplineBase Spline
        {
            get;
        }

        public abstract int Index
        {
            get;
        }

        //public SplinePoin

        private void Awake()
        {
            Unselect();
            AwakeOverride();
        }

        private void OnBeforeTransformParentChanged()
        {
            OnBeforeTransformParentChangedOverride();
        }

        private void OnTransformParentChanged()
        {
            OnTransformParentChangedOverride();
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

        public JunctionBase GetJunction()
        {
            return Spline.GetJunction(Index);
        }

        public void SetJunction(JunctionBase junction)
        {
            if(Spline != null)
            {
                Spline.SetJunction(Index, junction);
            }
        }

    
    

        public virtual void Select() { }
        public virtual void Unselect() { }

        protected virtual void AwakeOverride() { }
        protected virtual void OnBeforeTransformParentChangedOverride() { }
        protected virtual void OnTransformParentChangedOverride() { }
        protected virtual void StartOverride() { }
        protected virtual void OnEnableOverride() { }
        protected virtual void OnDisableOverride() { }
        protected virtual void OnDestroyOverride() { }
        protected virtual void OnApplicationQuitOverride() { }
        protected virtual void OnScriptReloaded() { }

        #if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts(99)]
        private static void OnScriptsReloaded()
        {
            SplinePointBase[] splinePoints = FindObjectsOfType<SplinePointBase>();
            for (int i = 0; i < splinePoints.Length; ++i)
            {
                splinePoints[i].OnScriptReloaded();
            }
        }
        #endif
    }

    public interface ISplinePointBaseInternalAccessor<T> where T : PointData, new()
    {
        T Data
        {
            get;
            set;
        }
    }

    public class SplinePointBase<T> : SplinePointBase, ISplinePointBaseInternalAccessor<T> where T : PointData, new()
    {
        private Quaternion m_rotation;
        private Vector3 m_euler;
        private SplineBase<T> m_spline;
        private Vector3 m_position;

        private float m_twistAngle;
        private float m_twistT0;
        private float m_twistT1;

        [SerializeField, HideInInspector]
        private T m_data = new T();

        T ISplinePointBaseInternalAccessor<T>.Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        public override SplineBase Spline
        {
            get { return m_spline; }
        }

        public override int Index
        {
            get
            {
                return transform.GetSiblingIndex();
            }
        }

     


        private void Update()
        {
            if (m_spline != null)
            {
                if(m_position != transform.position)
                {
                    m_spline.SetPointPosition(Index, transform.position);
                    m_position = transform.position;
                }
            }

            TwistData twistData = m_data.Twist;
            if (m_rotation != transform.rotation)
            {
                Vector3 euler = transform.eulerAngles;
                m_spline.SetPointRotation(Index, transform.rotation);
                m_rotation = transform.rotation;
                m_euler = transform.eulerAngles;
            }

            if(m_euler.z != twistData.Angle)
            {
                m_euler.z = twistData.Angle;
                transform.eulerAngles = m_euler;
                m_rotation = transform.rotation;
            }

            if(m_twistAngle != twistData.Angle)
            {
                m_spline.SetPointTwistAngle(Index, twistData.Angle);
                m_spline.SetPointRotation(Index, transform.rotation);
                m_twistAngle = twistData.Angle;
            }

            if (m_twistT0 != twistData.T0)
            {
                m_spline.SetPointTwistT0(Index, twistData.T0);
                m_twistT0 = twistData.T0;
            }

            if (m_twistT1 != twistData.T1)
            {
                m_spline.SetPointTwistT1(Index, twistData.T1);
                m_twistT1 = twistData.T1;
            }

            UpdateOverride();
        }

        public override void Select()
        {
            base.Select();
            enabled = true;
        }

        public override void Unselect()
        {
            base.Unselect();
            enabled = false;
        }

        private bool m_isEnabled;
        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            m_spline = GetComponentInParent<SplineBase<T>>();
            
            float angle = transform.eulerAngles.z;
            TwistData twistData = m_data.Twist;
            twistData.Angle -= Mathf.DeltaAngle(twistData.Angle, angle);

            m_rotation = transform.rotation;
            m_euler = transform.eulerAngles;

            JunctionBase junction = m_data.Junction;
            if (junction != null)
            {
                IJunctionInternalAccessor junctionInternal = junction;
                junctionInternal.Add(this);
            }

            //this is required to run Start method. 
            //Index has wrong value here, can't insert point to spline here, Redo operation does not work correctly in this case.
            //Record enabled value, set enabled to true, recorver enabled value back in Start method.
            m_isEnabled = enabled; 
            
            enabled = true;

          
        }

        protected override void OnTransformParentChangedOverride()
        {
            m_spline = GetComponentInParent<SplineBase<T>>();
        }

        protected override void StartOverride()
        {
            base.StartOverride();

            if(m_spline == null)
            {
                m_spline = GetComponentInParent<SplineBase<T>>();
            }
            
            if (m_spline != null)
            {
                ISplineInternalsAccessor<T> internals = m_spline;
                internals.Insert(Index, this);
            }

            m_spline.SetPointPosition(Index, transform.position);
            m_position = transform.position;
            m_twistAngle = m_data.Twist.Angle;
            m_twistT0 = m_data.Twist.T0;
            m_twistT1 = m_data.Twist.T1;

            enabled = m_isEnabled;
        }

        protected override void OnEnableOverride()
        {
            base.OnEnableOverride();
            m_position = transform.position;
            m_twistAngle = m_data.Twist.Angle;
            m_twistT0 = m_data.Twist.T0;
            m_twistT1 = m_data.Twist.T1;
        }

        protected virtual void UpdateOverride()
        {
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            JunctionBase junction = m_data.Junction;
            if (junction != null)
            {
                IJunctionInternalAccessor junctionInternal = junction;
                junctionInternal.Remove(this);
            }

            if (m_spline != null)
            {
                ISplineInternalsAccessor<T> internals = m_spline;
                internals.Remove(this);
            }

        }

        protected override void OnScriptReloaded()
        {
            base.OnScriptReloaded();
            m_spline = GetComponentInParent<SplineBase<T>>();
        }

    }
}

