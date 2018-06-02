using UnityEditor;
using UnityEngine;

namespace Battlehub.Spline3
{
    public static class SplinePointHandle 
    {
        public enum Result
        {
            None,
            BeginDrag,
            Drag,
            EndDrag,
            PointerOver,
        }


 
        private static int m_dragCtrlId;
        public static int DragCtrlId
        {
            get { return m_dragCtrlId; }
        }


        public static Result DragHandleGUI(Vector3 position, Quaternion rotation, float handleSize, float pickSize, Handles.DrawCapFunction capFunc, Color selectionColor)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            //if(handleSize == 0)
            //{
            //    return Result.None;
            //}
            Vector3 screenPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 cachedMatrix = Handles.matrix;
            Result result = Result.None;

            if(m_dragCtrlId != 0 )
            {
                if (HandleUtility.nearestControl == id && id != GUIUtility.hotControl)
                {
                    result = Result.PointerOver;
                }
            }

            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    {

                        MouseDown(id);
                        if (GUIUtility.hotControl == id)
                        {
                            result = Result.BeginDrag;
                        }

                    }
                    break;
                case EventType.MouseUp:
                    {

                        if (GUIUtility.hotControl == id && Event.current.button == 0)
                        {
                            result = Result.EndDrag;

                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                            m_dragCtrlId = 0;
                        }

                    }
                    break;

                case EventType.MouseDrag:
                    {

                        if (GUIUtility.hotControl == id)
                        {

                            m_dragCtrlId = id;
                            result = Result.Drag;
                            Event.current.Use();
                        }

                    }
                    break;
                case EventType.Repaint:
                    {
                        if (handleSize != 0)
                        {
                            Repaint(rotation, handleSize, capFunc, selectionColor, id, screenPosition, cachedMatrix);
                        }
                    }
                    break;
                case EventType.Layout:
                    {
                        if(m_dragCtrlId != id)
                        {
                            //if (handleSize != 0)
                            {
                                Layout(pickSize, id, screenPosition, cachedMatrix);
                            }
                            
                        }
                    }
                    break;
            }

            return result;
        }

        public static Result HandleGUI(Vector3 position, Quaternion rotation, float handleSize, float pickSize, Handles.DrawCapFunction capFunc, Color selectionColor)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);

            Vector3 screenPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 cachedMatrix = Handles.matrix;
            Result result = Result.None;

            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    {

                        MouseDown(id);

                    }
                    break;
                case EventType.MouseUp:
                    {

                        if (GUIUtility.hotControl == id && Event.current.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                            result = Result.EndDrag;
                            m_dragCtrlId = 0;
                        }

                    }
                    break;

                case EventType.MouseDrag:
                    {

                        if (GUIUtility.hotControl == id)
                        {

                            m_dragCtrlId = id;
                            result = Result.Drag;
                            Event.current.Use();
                        }


                    }
                    break;   
                case EventType.Repaint:
                    {
                        if (handleSize != 0)
                        {
                            Repaint(rotation, handleSize, capFunc, selectionColor, id, screenPosition, cachedMatrix);
                        }
                    }
                    break;
                case EventType.Layout:
                    {
                       // if (handleSize != 0)
                        {
                            if (m_dragCtrlId == 0)
                            {
                                Layout(pickSize, id, screenPosition, cachedMatrix);
                            }
                        }
                    }
                    break;
            }


            return result;
        }

        private static void MouseDown(int id)
        {
            if (HandleUtility.nearestControl == id && Event.current.button == 0)
            {
                if (Event.current.keyCode != KeyCode.V && !Event.current.shift)
                {
                    GUIUtility.hotControl = id;
                    Event.current.Use();
                }
            }
        }

        private static void Repaint(Quaternion rotation, float handleSize, Handles.DrawCapFunction capFunc, Color selectionColor, int id, Vector3 screenPosition, Matrix4x4 cachedMatrix)
        {
            Color currentColor = Handles.color;
            if(id != m_dragCtrlId)
            {
                if (id == GUIUtility.hotControl)
                {
                    Handles.color = selectionColor;
                }

                if (id == HandleUtility.nearestControl)
                {
                    Handles.color = selectionColor;
                }

            }

            Handles.matrix = Matrix4x4.identity;
            capFunc(id, screenPosition, rotation, handleSize);
            Handles.matrix = cachedMatrix;
            Handles.color = currentColor;
        }

        private static void Layout(float pickSize, int id, Vector3 screenPosition, Matrix4x4 cachedMatrix)
        {
            if (Event.current.keyCode != KeyCode.V && !Event.current.shift)
            {
                Handles.matrix = Matrix4x4.identity;
                HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(screenPosition, pickSize));
                Handles.matrix = cachedMatrix;
            }
        }
    }
}

