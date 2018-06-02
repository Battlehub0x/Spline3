using UnityEngine;

namespace Battlehub.Spline3
{
    [ExecuteInEditMode]
    public class ControlPoint : MonoBehaviour
    {
        private Vector3 m_position;
        //private Quaternion m_rotation;
        //private Vector3 m_euler;

        private SplinePointBase m_splinePoint;
        private SplineBase m_spline;

        public SplinePointBase SplinePoint
        {
            get { return m_splinePoint; }
        }
       
        private int Index
        {
            get { return transform.GetSiblingIndex(); }
        }

        public GameObject TwinPoint
        {
            get
            {
                if(m_spline == null || m_splinePoint == null)
                {
                    return null;
                }
                return m_spline.GetCtrlPoint(m_splinePoint.Index, (1 + Index) % 2);
            }
        }

        public int Mode
        {
            get
            {
                if(m_spline == null || m_splinePoint == null)
                {
                    return (int)BezierPointMode.Free;
                }
                return m_spline.GetPointMode(m_splinePoint.Index);
            }
            set
            {
                if (m_spline == null || m_splinePoint == null)
                {
                    return;
                }
                m_spline.SetPointMode(m_splinePoint.Index, value);
            }
        }

        public void Select()
        {
            enabled = true; 
        }

        public void Unselect()
        {
            enabled = false;
        }

        private void Awake()
        {
            enabled = false;

            m_splinePoint = GetComponentInParent<SplinePointBase>();
            m_spline = GetComponentInParent<SplineBase>();
   
        }

        private void OnTransformParentChanged()
        {
            m_splinePoint = GetComponentInParent<SplinePointBase>();
            m_spline = GetComponentInParent<SplineBase>();
         
        }

        private void Start()
        {
            if(m_splinePoint == null)
            {
                m_splinePoint = GetComponentInParent<SplinePointBase>();
            }
            if (m_spline == null)
            {
                m_spline = GetComponentInParent<SplineBase>();
            }
     
        }

        private void Update()
        {
            if (m_position != transform.localPosition)
            {
                SyncCtrlPointPosition();
            }

            //if(m_rotation != transform.localRotation)
            //{
            //    SyncCtrlPointRotation();
            //}
        }

        public void SyncCtrlPointPosition()
        {
            if (m_spline != null && m_splinePoint != null)
            {
                m_spline.SetCtrlPointPosition(m_splinePoint.Index, Index, transform.position);
                m_position = transform.localPosition;
            }
        }

        //public void SyncCtrlPointRotation()
        //{
        //    if(m_spline != null && m_splinePoint != null)
        //    {
        //        Vector3 currentEuler = transform.localEulerAngles;

        //        float angle = m_spline.GetPointTwistAngle(m_splinePoint.Index);
        //        angle -= Mathf.DeltaAngle(m_euler.z, currentEuler.z);
        //       // Debug.Log(angle);
        //        m_spline.SetPointTwistAngle(m_splinePoint.Index, angle);

        //        m_euler = transform.localEulerAngles;
        //        m_rotation = transform.localRotation;
        //    }
        //}

        private void OnScriptReloaded()
        {
            m_splinePoint = GetComponentInParent<SplinePointBase>();
            m_spline = GetComponentInParent<SplineBase>();
        }

        #if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts(99)]
        private static void OnScriptsReloaded()
        {
            ControlPoint[] bezierPoints = FindObjectsOfType<ControlPoint>();
            for (int i = 0; i < bezierPoints.Length; ++i)
            {
                bezierPoints[i].OnScriptReloaded();
            }
        }
        #endif
    }
}
