using System;
using UnityEngine;
using UnityEngine.Events;

namespace Battlehub.Spline3
{
    [Serializable]
    public class ForkEventArgs
    {
        private JunctionBase m_junction;

        /// <summary>
        /// Junction
        /// </summary>
        public JunctionBase Junction
        {
            get { return m_junction; }
        }

        private SplineBase m_spline;

        /// <summary>
        /// Current Spline
        /// </summary>
        public SplineBase Spline
        {
            get { return m_spline; }
        }

        private int m_splinePointIndex;

        /// <summary>
        /// Current SplinePointIndex
        /// </summary>
        public int SplinePointIndex
        {
            get { return m_splinePointIndex; }
        }

        /// <summary>
        /// -1 will force SplineFollow not to choose any branch. (To choose branch set SelectedConnectionIndex to [0, Junction.ConnectionsCount - 1])
        /// </summary>
        public int SelectedConnectionIndex
        {
            get;
            set;
        }

        public ForkEventArgs(SplineBase spline, int splinePointIndex)
        {
            SelectedConnectionIndex = -1;
            m_junction = spline.GetJunction(splinePointIndex);
            m_spline = spline;
            m_splinePointIndex = splinePointIndex;
        }
    }

    [Serializable]
    public class ForkEvent : UnityEvent<ForkEventArgs> { }

    public class SplineFollow : MonoBehaviour
    {
        public float Speed = 5.0f;
        public SplineBase Spline;
        public float Offset;
        public bool IsRunning = true;
        public bool StopAtJunction = false;
        public bool IsLoop = false;
        public ForkEvent Fork;
        public UnityEvent Completed;

        private SplineBase m_spline;
        private bool m_isRunning;
        private bool m_isCompleted;

        private float m_t;

        private void Start()
        {
            if (!Spline)
            {
                Debug.LogError("Set Spline Field!");
                enabled = false;
                return;
            }
            m_isCompleted = true;
        }

        private void Update()
        {
            if (IsRunning != m_isRunning)
            {
                if (m_isCompleted)
                {
                    Restart();
                }
                m_isRunning = IsRunning;
            }

            if (IsRunning)
            {
                Move(Time.deltaTime, true);
            }
        }

        private void Restart()
        {
            m_spline = Spline;
            m_t = (1.0f + Offset % 1.0f) % 1.0f;
            m_isCompleted = false;
            IsRunning = true;
        }

        private void Move(float deltaTime, bool allowRecursiveCall)
        {
            float v, deltaT;
            v = m_spline.GetVelocity(m_t).magnitude;
            v *= m_spline.CurveCount;
            deltaT = (deltaTime * Speed) / v;// F.1

            int pointIndex;
            if (deltaT >= 0)
            {
                pointIndex = m_spline.GetCurveIndex(m_t);
            }
            else
            {
                pointIndex = m_spline.GetCurveIndex(m_t) + 1;
            }

            int nextPointIndex;
            float nextT = m_t + deltaT;
            bool endOfSpline = nextT <= 0.0f || 1.0f <= nextT;
            if (endOfSpline)
            {
                if(deltaT >= 0)
                {
                    nextPointIndex = (pointIndex + 1) % m_spline.PointsCount;
                }
                else
                {
                    nextPointIndex = (m_spline.PointsCount + pointIndex - 1) % m_spline.PointsCount; 
                }
            }
            else
            {
                if(deltaT >= 0)
                {
                    nextPointIndex = m_spline.GetCurveIndex(nextT);
                }
                else
                {
                    nextPointIndex = m_spline.GetCurveIndex(nextT) + 1;
                }
            }


            bool continueIfLoop = IsLoop;
            if (pointIndex != nextPointIndex)
            {
                //Debug.Log("Point Index " + pointIndex + " Next Point Index " + nextPointIndex);
                JunctionBase junction = m_spline.GetJunction(nextPointIndex);
                if (junction != null)
                {
                    ForkEventArgs args = new ForkEventArgs(m_spline, nextPointIndex);
                    Fork.Invoke(args);

                    int connection = -1;
                    if (args.SelectedConnectionIndex == -1)
                    {
                        if (StopAtJunction)
                        {
                            IsRunning = false;
                            return;
                        }

                        if (endOfSpline)
                        {
                            if (deltaT > 0)
                            {
                                connection = junction.FirstOut();
                                if (connection == -1)
                                {
                                    connection = junction.FirstIn();
                                }
                            }
                            else
                            {
                                connection = junction.FirstIn();
                                if (connection == -1)
                                {
                                    connection = junction.FirstOut();
                                }
                            }

                            if (connection == -1)
                            {
                                continueIfLoop = false;
                            }
                        }
                    }
                    else
                    {
                        connection = args.SelectedConnectionIndex;
                    }

                    if (connection != -1)
                    {
                        float nextPointT = m_spline.GetTAt(nextPointIndex);
                        deltaT = m_t + deltaT - nextPointT;
                        float remDeltaTime = deltaT * v / Speed; //Calculated using F.1

                        m_spline = junction.GetSpline(connection);
                        int splinePointIndex = junction.GetSplinePointIndex(connection);
                        m_t = m_spline.GetTAt(splinePointIndex);

                        bool isOut = junction.IsOut(connection);
                        bool isIn = junction.IsIn(connection);

                        if (isOut && !isIn && Speed < 0 || isIn && !isOut  && Speed > 0)
                        {
                            Speed *= -1;
                        }
                        
                        if(allowRecursiveCall)
                        {
                            Move(remDeltaTime, false);
                            return;
                        }
                        else
                        {
                            v = m_spline.GetVelocity(m_t).magnitude;
                            v *= m_spline.CurveCount;
                            deltaT = (remDeltaTime * Speed) / v;// F.1
                        }
                    }
                }
            }

            MoveOrStop(deltaT, endOfSpline, continueIfLoop);
        }

    

        private void MoveOrStop(float deltaT, bool endOfSpline, bool continueIfLoop)
        {
            m_t += deltaT;
            if (endOfSpline)
            {
                if (continueIfLoop)
                {
                    m_t = (1.0f + m_t % 1.0f) % 1.0f;
                }
                else
                {
                    m_t = Mathf.Max(Mathf.Min(m_t, 1.0f), 0.0f);
                    IsRunning = false;
                    m_isRunning = false;
                    m_isCompleted = true;
                    Completed.Invoke();
                }
            }
            else
            {
                m_t = Mathf.Max(Mathf.Min(m_t, 1.0f), 0.0f);
            }

            UpdatePosition(m_t);
        }

        private void UpdatePosition(float t)
        {
            Vector3 position = m_spline.GetPosition(t);
            Vector3 dir = m_spline.GetDirection(t);
            float twist = m_spline.GetTwistAngle(t);

            Quaternion rotation = Quaternion.AngleAxis(twist, dir) * Quaternion.LookRotation(dir, Vector3.up);

            transform.position = position;
            transform.rotation = rotation;
        }
    }
}

