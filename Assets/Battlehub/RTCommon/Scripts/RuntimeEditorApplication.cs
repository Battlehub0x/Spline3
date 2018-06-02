using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Battlehub.RTCommon
{
    public enum ProjectItemType
    {
        None = 0,
        Folder = 1,
        Scene = 2,
        Resource = 4,
        Any = Folder | Scene | Resource
    }

    public enum KnownResourceType
    {
        Unknown,
        Material,
        Texture,
        Sprite,
        Mesh,
        Prefab,
        Scene,
    }

    [System.Serializable]
    public class ProjectItemOld
    {
        public ProjectItemOld Parent;
        public List<ProjectItemOld> Children;
        public string Id;
        public ProjectItemType Type;
        public KnownResourceType[] ResourceTypes;

        public ProjectItemOld Root
        {
            get
            {
                ProjectItemOld root = Parent;
                if (root == null)
                {
                    return this;
                }

                while (root.Parent != null)
                {
                    root = root.Parent;
                }

                return root;
            }
        }

        public ProjectItemOld()
        {

        }

        public ProjectItemOld(string id, ProjectItemType type, ProjectItemOld parent = null, KnownResourceType[] resourceTypes = null)
        {
            if (!IsValidId(id))
            {
                throw new System.FormatException("id has invalid characters");
            }
            if (id != null)
            {
                if (type != ProjectItemType.Resource)
                {
                    if (id[0] == '-' || char.IsDigit(id[0]))
                    {
                        throw new System.FormatException("id should start with character");
                    }
                }
            }


            Parent = parent;
            Id = id;
            Type = type;
            ResourceTypes = resourceTypes;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ProjectItemOld parent = this;
            while (parent != null)
            {
                sb.Insert(0, parent.Id);
                sb.Insert(0, "/");
                parent = parent.Parent;
            }

            if (Type == ProjectItemType.Scene)
            {
                return sb.ToString() + ".rtsc";
            }
            else if (Type == ProjectItemType.Resource)
            {
                return sb.ToString() + ".rtrs";
            }

            return sb.ToString();

        }

        public static bool IsValidId(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return true;
            }
            return Path.GetInvalidFileNameChars().All(c => !fileName.Contains(c));
        }

        public static string GetUniqueName(string baseName, ProjectItemOld parentFolder, ProjectItemType type)
        {
            if (parentFolder.Children == null || parentFolder.Children.Count == 0)
            {
                return baseName;
            }

            while (baseName.Length > 0)
            {
                if (char.IsDigit(baseName[baseName.Length - 1]))
                {
                    baseName = baseName.Substring(0, baseName.Length - 1);
                    baseName = baseName.TrimEnd(' ');
                }
                else
                {
                    break;
                }
            }

            string[] names = parentFolder.Children.Where(projectItem => (projectItem.Type & type) != 0 && projectItem.Id.Contains(baseName)).Select(projectItem => projectItem.Id).ToArray();
            HashSet<int> ids = new HashSet<int>();
            for (int i = 0; i < names.Length; ++i)
            {
                string name = names[i];
                if (name == baseName)
                {
                    if (!ids.Contains(0))
                    {
                        ids.Add(0);
                        continue;
                    }
                }
                string[] parts = name.Split(' ');
                string lastPart = parts[parts.Length - 1];
                int v;
                if (int.TryParse(lastPart, out v))
                {
                    if (!ids.Contains(v))
                    {
                        ids.Add(v);
                    }
                }
            }

            if (ids.Count == 0)
            {
                return baseName;
            }

            if (ids.Count > 10000)
            {
                return baseName + " " + System.Guid.NewGuid();
            }


            int max = ids.Max();
            do
            {
                max++;
            }
            while (ids.Contains(max));
            return baseName + " " + max;
        }

        public bool HasChild(string id, ProjectItemType itemType)
        {
            return GetChild(id, itemType) != null;
        }

        public ProjectItemOld GetChild(string id, ProjectItemType itemType)
        {
            if (Children == null)
            {
                return null;
            }

            return Children.Where(p => p.Id == id && (p.Type & itemType) != 0).FirstOrDefault();
        }

        public void SetParent(ProjectItemOld item)
        {
            if (Parent != null)
            {
                if (Parent.Children != null)
                {
                    Parent.Children.Remove(this);
                }
            }

            Parent = item;
            if (Parent.Children == null)
            {
                Parent.Children = new List<ProjectItemOld>();
            }
            Parent.Children.Add(this);
        }

        public int GetSiblingIndex()
        {
            return Parent.Children.IndexOf(this);
        }

        public void SetSiblingIndex(int index)
        {
            Parent.Children.Remove(this);
            Parent.Children.Insert(index, this);
        }

        public void AddChild(ProjectItemOld item)
        {
            if (Children == null)
            {
                Children = new List<ProjectItemOld>(1);
            }

            item.Parent = this;
            Children.Add(item);
        }

        public void RemoveChild(ProjectItemOld item)
        {
            Children.Remove(item);
            item.Parent = null;
        }

        public ProjectItemOld Find(string path, ProjectItemType type)
        {
            return Find(this, item => item.ToString() == path && (item.Type & type) != 0);
        }

        public ProjectItemOld Find(System.Func<ProjectItemOld, bool> visit)
        {
            return Find(this, visit);
        }

        public void FindChildren(List<ProjectItemOld> items, ProjectItemType type)
        {
            if (Children != null)
            {
                for (int i = 0; i < Children.Count; ++i)
                {
                    ProjectItemOld child = Children[i];
                    if ((child.Type & type) != 0)
                    {
                        items.Add(Children[i]);
                    }
                }
            }
        }

        public void FindDescendants(List<ProjectItemOld> items, ProjectItemType type)
        {
            Foreach(this, item =>
            {
                if ((item.Type & type) != 0)
                {
                    items.Add(item);
                }
            });
        }



        private ProjectItemOld Find(ProjectItemOld item, System.Func<ProjectItemOld, bool> visit)
        {
            if (visit(item))
            {
                return item;
            }

            if (item.Children == null)
            {
                return null;
            }

            for (int i = 0; i < item.Children.Count; ++i)
            {
                ProjectItemOld result = Find(item.Children[i], visit);
                if (result != null)
                {
                    return result;
                }
            }

            return null;

        }

        public static void Foreach(ProjectItemOld item, System.Action<ProjectItemOld> visit)
        {
            if (item == null)
            {
                return;
            }

            visit(item);
            if (item.Children == null)
            {
                return;
            }

            for (int i = 0; i < item.Children.Count; ++i)
            {
                Foreach(item.Children[i], visit);
            }
        }

        public bool IsFolder
        {
            get { return Type == ProjectItemType.Folder; }
        }

        public bool IsResource
        {
            get { return Type == ProjectItemType.Resource; }
        }

        public bool IsScene
        {
            get { return Type == ProjectItemType.Scene; }
        }

        public bool IsNone
        {
            get { return Type == ProjectItemType.None; }
        }
    }

    public delegate void RuntimeEditorEvent();
    public static class RuntimeEditorApplication
    {
        public static event RuntimeEditorEvent PlaymodeStateChanged;
        public static event RuntimeEditorEvent ActiveWindowChanged;
        public static event RuntimeEditorEvent PointerOverWindowChanged;
        public static event RuntimeEditorEvent IsOpenedChanged;
        public static event RuntimeEditorEvent ActiveSceneChanged;
        public static event RuntimeEditorEvent ActiveProjectChanged;
        public static event RuntimeEditorEvent NewSceneCreated;

        private static List<RuntimeEditorWindow> m_windows = new List<RuntimeEditorWindow>();
        private static RuntimeEditorWindow m_pointerOverWindow;
        private static RuntimeEditorWindow m_activeWindow;

        public static RuntimeEditorWindow PointerOverWindow
        {
            get { return m_pointerOverWindow; }
        }

        public static RuntimeWindowType PointerOverWindowType
        {
            get
            {
                if (m_pointerOverWindow == null)
                {
                    return RuntimeWindowType.None;
                }

                return m_pointerOverWindow.WindowType;
            }
        }

        public static RuntimeEditorWindow ActiveWindow
        {
            get { return m_activeWindow; }
        }

        public static RuntimeWindowType ActiveWindowType
        {
            get
            {
                if (m_activeWindow == null)
                {
                    return RuntimeWindowType.None;
                }

                return m_activeWindow.WindowType;
            }
        }

        public static RuntimeEditorWindow GetWindow(RuntimeWindowType type)
        {
            return m_windows.Where(wnd => wnd != null && wnd.WindowType == type).FirstOrDefault();
        }

        public static void ActivateWindow(RuntimeEditorWindow window)
        {
            if (m_activeWindow != window)
            {
                m_activeWindow = window;
                if (ActiveWindowChanged != null)
                {
                    ActiveWindowChanged();
                }
            }
        }

        public static void ActivateWindow(RuntimeWindowType type)
        {
            ActivateWindow(GetWindow(type));
        }

        public static void PointerEnter(RuntimeEditorWindow window)
        {
            if (m_pointerOverWindow != window)
            {
                m_pointerOverWindow = window;
                if (PointerOverWindowChanged != null)
                {
                    PointerOverWindowChanged();
                }
            }

        }

        public static void PointerExit(RuntimeEditorWindow window)
        {
            if (m_pointerOverWindow == window && m_pointerOverWindow != null)
            {
                m_pointerOverWindow = null;
                if (PointerOverWindowChanged != null)
                {
                    PointerOverWindowChanged();
                }
            }
        }

        public static bool IsPointerOverWindow(RuntimeWindowType type)
        {
            return PointerOverWindowType == type;
        }

        public static bool IsPointerOverWindow(RuntimeEditorWindow window)
        {
            return m_pointerOverWindow == window;
        }

        public static bool IsActiveWindow(RuntimeWindowType type)
        {
            return ActiveWindowType == type;
        }

        public static bool IsActiveWindow(RuntimeEditorWindow window)
        {
            return m_activeWindow == window;
        }

        public static void AddWindow(RuntimeEditorWindow window)
        {
            m_windows.Add(window);
        }

        public static void RemoveWindow(RuntimeEditorWindow window)
        {
            m_windows.Remove(window);
        }

        public static Camera[] GameCameras
        {
            get;
            set;
        }

        public static Camera[] SceneCameras
        {
            get;
            set;
        }

        public static int ActiveSceneCameraIndex
        {
            get;
            set;
        }

        public static Camera ActiveSceneCamera
        {
            get
            {
                if (SceneCameras == null || SceneCameras.Length == 0)
                {
                    return null;
                }
                return SceneCameras[ActiveSceneCameraIndex];
            }
        }
        private static bool m_isOpened = false;
        public static bool IsOpened
        {
            get { return m_isOpened; }
            set
            {
                if (m_isOpened != value)
                {
                    m_isOpened = value;
                    if (!m_isOpened)
                    {
                        ActivateWindow(GetWindow(RuntimeWindowType.GameView));
                    }
                    if (IsOpenedChanged != null)
                    {
                        IsOpenedChanged();
                    }

                }
            }
        }


        //public static RuntimeWindowType PointerOverWindow
        //{
        //    get { return m_pointerOverWindow; }
        //    set
        //    {
        //        if(m_pointerOverWindow != value)
        //        {
        //            m_pointerOverWindow = value;
        //            if(PointerOverWindowChanged != null)
        //            {
        //                PointerOverWindowChanged();
        //            }
        //        }
        //    }
        //}

        //private static RuntimeWindowType m_focusedWindow = RuntimeWindowType.GameView;
        //public static RuntimeWindowType FocusedWindow
        //{
        //    get { return m_focusedWindow; }
        //    set
        //    {
        //        if(m_focusedWindow != value)
        //        {
        //            m_focusedWindow = value;
        //            if(FocusedWindowChanged != null)
        //            {
        //                FocusedWindowChanged();
        //            }
        //        }
        //    }
        //}

        private static bool m_isPlaying;
        public static bool IsPlaying
        {
            get
            {
                return m_isPlaying;
            }
            set
            {
                if (m_isPlaying != value)
                {
                    m_isPlaying = value;
                    if (PlaymodeStateChanged != null)
                    {
                        PlaymodeStateChanged();
                    }
                }
            }
        }


    //    private static ProjectItemOld m_activeScene = new ProjectItemOld(null, ProjectItemType.Scene);
    //    public static ProjectItemOld ActiveScene
    //    {
    //        get { return m_activeScene; }
    //        set
    //        {
    //            if (m_activeScene != value)
    //            {
    //                m_activeScene = value;
    //                if (ActiveSceneChanged != null)
    //                {
    //                    ActiveSceneChanged();
    //                }

    //            }
    //        }
    //    }

    //    private static ProjectItemOld m_activeProject;
    //    public static ProjectItemOld ActiveProject
    //    {
    //        get { return m_activeProject; }
    //        set
    //        {
    //            if (m_activeProject != value)
    //            {
    //                m_activeProject = value;
    //                if (ActiveProjectChanged != null)
    //                {
    //                    ActiveProjectChanged();
    //                }
    //            }
    //        }
    //    }

    //    public static void CreateNewScene()
    //    {
    //        RuntimeSelection.objects = null;
    //        RuntimeUndo.Purge();
    //        ExposeToEditor[] editorObjects = ExposeToEditor.FindAll(ExposeToEditorObjectType.EditorMode, false).Select(go => go.GetComponent<ExposeToEditor>()).ToArray();
    //        for (int i = 0; i < editorObjects.Length; ++i)
    //        {
    //            ExposeToEditor exposeToEditor = editorObjects[i];
    //            if (exposeToEditor != null)
    //            {
    //                Object.DestroyImmediate(exposeToEditor.gameObject);
    //            }
    //        }

    //        GameObject dirLight = new GameObject();
    //        dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
    //        dirLight.transform.position = new Vector3(0, 10, 0);
    //        Light lightComponent = dirLight.AddComponent<Light>();
    //        lightComponent.type = LightType.Directional;

    //        dirLight.name = "Directional Light";
    //        dirLight.AddComponent<ExposeToEditor>();

    //        m_activeScene = new ProjectItemOld(null, ProjectItemType.Scene);
    //        if (NewSceneCreated != null)
    //        {
    //            NewSceneCreated();
    //        }
    //    }
    }

}
