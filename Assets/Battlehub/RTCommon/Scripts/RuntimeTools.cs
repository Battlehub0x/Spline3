using UnityEngine;
using System.Collections;

namespace Battlehub.RTCommon
{
    public enum RuntimeTool
    {
        None,
        Move,
        Rotate,
        Scale,
        View,
    }

    public enum RuntimePivotRotation
    {
        Local,
        Global
    }

    public delegate void RuntimeToolsEvent();
    public delegate void SpawnPrefabChanged(GameObject oldPrefab);

    /// <summary>
    /// Runtime tools and flags
    /// </summary>
    public static class RuntimeTools
    {
        public static event RuntimeToolsEvent ToolChanged;
        public static event RuntimeToolsEvent PivotRotationChanged;
        public static event SpawnPrefabChanged SpawnPrefabChanged;

        public static event RuntimeToolsEvent IsViewingChanged;
        public static event RuntimeToolsEvent IsSceneGizmoSelectedChanged;
        public static event RuntimeToolsEvent ShowSelectionGizmosChanged;
        public static event RuntimeToolsEvent AutoFocusChanged;
        public static event RuntimeToolsEvent UnitSnappingChanged;
        public static event RuntimeToolsEvent BoundingBoxSnappingChanged;

        private static RuntimeTool m_current;
        private static RuntimePivotRotation m_pivotRotation;

        private static bool m_isViewing;
        public static bool IsViewing
        {
            get { return m_isViewing; }
            set
            {
                if(m_isViewing != value)
                {
                    m_isViewing = value;
                    if(IsViewingChanged != null)
                    {
                        IsViewingChanged();
                    }
                }
            }
        }

        private static bool m_isSceneGizmoSelected;
        public static bool IsSceneGizmoSelected
        {
            get { return m_isSceneGizmoSelected; }
            set
            {
                if(m_isSceneGizmoSelected != value)
                {
                    m_isSceneGizmoSelected = value;
                    if(IsSceneGizmoSelectedChanged != null)
                    {
                        IsSceneGizmoSelectedChanged();
                    }
                }
            }
        }

        private static bool m_showSelectionGizmos;
        public static bool ShowSelectionGizmos
        {
            get { return m_showSelectionGizmos; }
            set
            {
                if(m_showSelectionGizmos != value)
                {
                    m_showSelectionGizmos = value;
                    if(ShowSelectionGizmosChanged != null)
                    {
                        ShowSelectionGizmosChanged();
                    }
                }
            }
        }

        private static bool m_autoFocus;
        public static bool AutoFocus
        {
            get { return m_autoFocus; }
            set
            {
                if(m_autoFocus != value)
                {
                    m_autoFocus = value;
                    if(AutoFocusChanged != null)
                    {
                        AutoFocusChanged();
                    }
                }
            }
        }

        private static bool m_unitSnapping;
        public static bool UnitSnapping
        {
            get { return m_unitSnapping; }
            set
            {
                if(m_unitSnapping != value)
                {
                    m_unitSnapping = value;
                    if(UnitSnappingChanged != null)
                    {
                        UnitSnappingChanged();
                    }
                }
            }
        }

        private static bool m_boundingBoxSnapping;
        public static bool IsInSnappingMode
        {
            get { return m_boundingBoxSnapping; }
            set
            {
                if(m_boundingBoxSnapping != value)
                {
                    m_boundingBoxSnapping = value;
                    if(BoundingBoxSnappingChanged != null)
                    {
                        BoundingBoxSnappingChanged();
                    }
                }
            }
        }

        private static GameObject m_spawnPrefab;
        public static GameObject SpawnPrefab
        {
            get { return m_spawnPrefab; }
            set
            {
                if(m_spawnPrefab != value)
                {
                    GameObject oldPrefab = m_spawnPrefab;
                    m_spawnPrefab = value;
                    if(SpawnPrefabChanged != null)
                    {
                        SpawnPrefabChanged(oldPrefab);
                    }
                }
            }
        }

        public static RuntimeTool Current
        {
            get { return m_current; }
            set
            {
                if (m_current != value)
                {
                    m_current = value;
                    if (ToolChanged != null)
                    {
                        ToolChanged();
                    }
                }
            }
        }

        public static RuntimePivotRotation PivotRotation
        {
            get { return m_pivotRotation; }
            set
            {
                if (m_pivotRotation != value)
                {
                    m_pivotRotation = value;
                    if (PivotRotationChanged != null)
                    {
                        PivotRotationChanged();
                    }
                }
            }
        }

        static RuntimeTools()
        {
            m_showSelectionGizmos = true;
            m_unitSnapping = false;
        }
    }
}
