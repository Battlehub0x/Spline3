using Battlehub.RTCommon;
using UnityEditor;
using UnityEngine;

namespace Battlehub.Spline3
{
    [InitializeOnLoad]
    public static class SceneGLCamera
    {
        static SceneGLCamera()
        {
            Enable();
        }

        public static void Enable()
        {
            Camera[] cameras = SceneView.GetAllSceneCameras();
            for (int i = 0; i < cameras.Length; ++i)
            {
                Camera camera = cameras[i];
                GLCamera glCamera = camera.GetComponent<GLCamera>();
                if (glCamera == null)
                {
                    camera.gameObject.AddComponent<GLCamera>();
                }
            }
        }

        public static void Disable()
        {
            Camera[] cameras = SceneView.GetAllSceneCameras();
            for (int i = 0; i < cameras.Length; ++i)
            {
                Camera camera = cameras[i];
                GLCamera glCamera = camera.GetComponent<GLCamera>();
                if (glCamera != null)
                {
                    Object.DestroyImmediate(glCamera);
                }
            }
        }
    }
}
