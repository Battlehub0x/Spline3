using UnityEngine;

namespace Battlehub.Spline3
{
    public class SplineFollowBehavior : MonoBehaviour
    {
        [SerializeField]
        private SplineFollow m_splineFollow;

        protected SplineFollow SplineFollow
        {
            get { return m_splineFollow; }
        }

        private void Awake()
        {
            if(m_splineFollow == null)
            {
                m_splineFollow = GetComponent<SplineFollow>();

                if(m_splineFollow == null)
                {
                    Debug.LogError("SplineFollow is not set");
                    return;
                }
            }

            AwakeOverride();

            m_splineFollow.Fork.AddListener(OnFork);
        }

        private void Start()
        {
            StartOverride();
        }

        private void OnDestroy()
        {
            if(m_splineFollow != null)
            {
                m_splineFollow.Fork.RemoveListener(OnFork);
            }
            

            OnDestroyOverride();
        }

        protected virtual void AwakeOverride()
        {

        }

        protected virtual void StartOverride()
        {

        }

        protected virtual void OnDestroyOverride()
        {

        }

        protected virtual void OnFork(ForkEventArgs args)
        {

        }

    }
}

