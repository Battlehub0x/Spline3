using UnityEditor;
using UnityEngine;

namespace Battlehub.Spline3
{
    public static class JunctionHandle
    {
        public enum Result
        {
            None,
            Click,
            Drag,
        }

        public static Result GUI(Vector3 position, Quaternion rotation, float handleSize, float pickSize, Handles.DrawCapFunction capFunc, Color selectionColor)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Vector3 screenPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 cachedMatrix = Handles.matrix;
            Result result = Result.None;
            switch (Event.current.GetTypeForControl(id))
            {   
                case EventType.MouseDown:
                    {
                        if (HandleUtility.nearestControl == id && Event.current.button == 0)
                        {
                            GUIUtility.hotControl = id;
                            Event.current.Use();
                        }  
                    }
                    break;
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == id && Event.current.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                            result = Result.Click;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == id)
                        {
                            GUIUtility.hotControl = 0;                           
                            Event.current.Use();
                            result = Result.Drag;
                        }
                    }
                    break;
                case EventType.Repaint:
                    {
                        if (handleSize != 0)
                        {
                            Color currentColor = Handles.color;

                            if (id == GUIUtility.hotControl)
                            {
                                Handles.color = selectionColor;
                            }

                            Handles.matrix = Matrix4x4.identity;
                            capFunc(id, screenPosition, rotation, handleSize);
                            Handles.matrix = cachedMatrix;
                            Handles.color = currentColor;
                        }
                    }
                    break;
                case EventType.Layout:
                    {
                       // if (handleSize != 0)
                        {
                            if (SplinePointHandle.DragCtrlId == 0)
                            {
                                Handles.matrix = Matrix4x4.identity;
                                HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(screenPosition, pickSize));
                                Handles.matrix = cachedMatrix;
                            }
                        }
                    }
                    break;
            }

            return result;
        }
    }
}


