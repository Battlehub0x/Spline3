using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif

using Battlehub.RTCommon;

namespace Battlehub.Spline3
{
    [ExecuteInEditMode]
    public class SplineRenderer : MonoBehaviour, IGL
    {
        public Color SplineColor = Color.green;
        public Color SplineAltColor = Color.gray;
        public Color SplinePointColor = Color.green;
        public Color SelectionColor = Color.yellow;
        public Color JunctionColor = Color.magenta;

        public Color[] ControlPointColors = new[] { Color.yellow, Color.blue, Color.red };
        public float PointSize = 1.0f;
        [HideInInspector]
        public float SelectionMargin = 0.00f;
        public bool ShowDirections = true;

        private SplineBase m_spline;
        private bool m_started;
        private static float Smoothness = 20.0f;
        private static Material m_splineMaterial;
        public static Material SplineMaterial
        {
            get { return m_splineMaterial; }
        }

        public static bool SplineMaterialZTest
        {
            get { return PlayerPrefs.GetInt("Battehub.Spline3.SplineMaterialZTest", 0) == 1; }
            set
            {
                if (SplineMaterial != null)
                {
                    SetSplineMaterialZTest(value);
                }

                PlayerPrefs.SetInt("Battehub.Spline3.SplineMaterialZTest", value ? 1 : 0);
            }
        }

        private static void SetSplineMaterialZTest(bool value)
        {
            if (value)
            {
                SplineMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            }
            else
            {
                SplineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            }
        }

        private static void InitSplineMaterial()
        {
            if (m_splineMaterial != null)
            {
                return;
            }
            Shader shader = Shader.Find("Battlehub/Spline3/Spline");
            m_splineMaterial = new Material(shader);
            m_splineMaterial.name = "SplineMaterial";
            SetSplineMaterialZTest(SplineMaterialZTest);
        }

        public bool IsSelected
        {
            get;
            set;
        }

        private void Awake()
        {
            InitSplineMaterial();
            GLRenderer glRenderer = FindObjectOfType<GLRenderer>();
            if (glRenderer == null)
            {
                GameObject go = new GameObject();
                go.name = "GLRenderer";
                go.AddComponent<GLRenderer>();
            }
            m_spline = GetComponent<SplineBase>();
        }

        private void Start()
        {
            m_started = true;
            if (GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Add(this);
            }
        }

        private void OnEnable()
        {
            if(m_started)
            {
                if (GLRenderer.Instance != null)
                {
                    GLRenderer.Instance.Add(this);
                }
            }
        }

        private void OnDisable()
        {
            if (GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Remove(this);
            }
        }


        public bool IsControlPointVisible(int pointIndex, int controlPointIndex)
        {
            JunctionBase junction = m_spline.GetJunction(pointIndex);
            if(junction != null)
            {
                if (m_spline.InterpolationMode == 1)
                {
                    return false;
                }

                if (junction.GetSpline(0) != m_spline || junction.GetSplinePointIndex(0) != pointIndex)
                {
                    return false;
                }

                return true;
            }
            

            if(m_spline.InterpolationMode == 0)
            {
                if (pointIndex == 0 && controlPointIndex == 0 || pointIndex == m_spline.PointsCount - 1 && controlPointIndex == m_spline.GetCtrlPointsCount(pointIndex) - 1)
                {
                    return false;
                }
            }
            else
            {
                if(pointIndex != 0 && pointIndex != m_spline.PointsCount - 1)
                {
                    return false;
                }

                if (pointIndex == 0 && controlPointIndex == m_spline.GetCtrlPointsCount(pointIndex) - 1 || pointIndex == m_spline.PointsCount - 1 && controlPointIndex == 0)
                {
                    return false;
                }
            }
            

            return true;
        }


        void IGL.Draw(int cullingMask)
        {
            if (m_spline.CurveCount < 1)
            {
                return;
            }

            m_splineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(transform.localToWorldMatrix);
            GL.Begin(GL.LINES);

            if (IsSelected)
            {
                GL.Color(SplineAltColor);
                for (int i = 0; i < m_spline.PointsCount; ++i)
                {
                    int ctrlPointsCount = m_spline.GetCtrlPointsCount(i);
                    for (int j = 0; j < ctrlPointsCount; ++j)
                    {
                        if (!IsControlPointVisible(i, j))
                        {
                            continue;
                        }

                        GL.Vertex(m_spline.transform.InverseTransformPoint(m_spline.GetCtrlPointPosition(i, j)));
                        GL.Vertex(m_spline.GetPointLocalPosition(i));
                    }
                }
            }

            GL.Color(SplineColor);
            float step = Mathf.Max(0.01f, Mathf.Min(1, 1 / Smoothness));
            for(int i = 0; i < m_spline.CurveCount; ++i)
            {
                float t = 0.0f;
                do
                {
                    GL.Vertex(m_spline.GetLocalPosition(t, i));
                    t += step;
                    t = Mathf.Min(1.0f, t);
                    GL.Vertex(m_spline.GetLocalPosition(t, i));
                }
                while (t < 1.0f);


                if (ShowDirections)
                {
                    Vector3 dir = m_spline.GetDirection(0.5f, i);
                    Vector3 p = m_spline.GetPosition(0.5f, i);
                    Vector3 dirInCamSpace = Camera.current.transform.InverseTransformDirection(dir).normalized;
                    Vector3 toCamCamSpace = Camera.current.transform.InverseTransformDirection(Camera.current.transform.position - p);
                    if (Mathf.Abs(Vector3.Dot(dirInCamSpace, Vector3.forward)) < 1)
                    {
                        Vector3 perp = Vector3.Cross(dirInCamSpace, toCamCamSpace).normalized;
                        perp = Camera.current.transform.TransformDirection(perp);

                        float scale = GetScreenScale(p, Camera.current) * 0.075f * PointSize;

                        Vector3 p1 = p - dir * scale + perp * 0.5f * scale;
                        Vector3 p2 = p - dir * scale - perp * 0.5f * scale;


                        p = m_spline.transform.InverseTransformPoint(p);
                        p1 = m_spline.transform.InverseTransformPoint(p1);
                        p2 = m_spline.transform.InverseTransformPoint(p2);
                        GL.Vertex(p);
                        GL.Vertex(p1);
                        GL.Vertex(p);
                        GL.Vertex(p2);
                    }
                }
            }

            GL.Color(SplineAltColor);
            for (int i = 0; i < m_spline.CurveCount; ++i)
            {
                float t = 0.0f;
                do
                {
                    Vector3 dir = m_spline.GetDirection(t, i);
                    float twist = m_spline.GetTwistAngle(t, i);           

                    Quaternion rotation = Quaternion.AngleAxis(twist, dir) * Quaternion.LookRotation(dir, Vector3.up);
                    Vector3 twistVector = m_spline.transform.InverseTransformVector(rotation * Vector3.up);

                    Vector3 p = m_spline.GetPosition(t, i);
                    float scale = GetScreenScale(p, Camera.current) * 0.075f * PointSize;

                    p = m_spline.transform.InverseTransformPoint(p);

                    GL.Vertex(p);
                    GL.Vertex(p + twistVector * scale);

                    t += step;
                    t = Mathf.Min(1.0f, t);
                }
                while (t < 1.0f);
            }

            GL.End();
            GL.PopMatrix();
        }

        public static float GetScreenScale(Vector3 position, Camera camera)
        {
            float h = camera.pixelHeight;
            if (camera.orthographic)
            {
                return camera.orthographicSize * 2f / h * 90;
            }

            Transform transform = camera.transform;
            float distance = Vector3.Dot(position - transform.position, transform.forward);
            float scale = 2.0f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return scale / h * 90;
        }


#if UNITY_EDITOR
        private void OnScriptReloaded()
        {
            m_spline = GetComponent<SplineBase>();
            if(GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Add(this);
            }
        }

        [DidReloadScripts(100)]
        private static void OnScriptsReloaded()
        {
            InitSplineMaterial();

            SplineRenderer[] splineRenderers = FindObjectsOfType<SplineRenderer>();
            for(int i = 0; i < splineRenderers.Length; ++i)
            {
                splineRenderers[i].OnScriptReloaded();
            }
        }
        #endif
    }
}

