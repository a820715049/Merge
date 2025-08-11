/**
 * @Author: handong.liu
 * @Date: 2022-01-04 10:28:43
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EL
{
    public interface IScene
    {
        IEnumerator ATLoad();           //只load，不可见
        IEnumerator ATCreate(GameObject root);                 //可以造成可见变化, root为这个scene的root
        void Destroy();                         //ATCreate的成对，表示删除可见对象
        void Unload();                          //ATLoad的成对，表示释放资源
        void OnActive();
        void OnDeactive();
        void BindManager(SceneManager man);
        void Update(float dt);
    }

    public class SceneManager : MonoBehaviour
    {
        private enum ScenePhase
        {
            NotActive,
            Active,
            Removed
        }
        private class SceneEntry
        {
            public ScenePhase phase;
            public GameObject root;
            public AsyncTaskBase loadTask;          //这个是isSuccess代表已经load
            public AsyncTaskBase createTask;        //这个是isSuccess代表已经create
            public IScene scene;
        }
        private Dictionary<int, SceneEntry> mAllScene = new Dictionary<int, SceneEntry>();


        public void HideAllScene()
        {
            foreach(var s in mAllScene)
            {
                SetSceneActive(s.Key, false);
            }
        }

        public void UncreateAllScene()
        {
            foreach(var s in mAllScene)
            {
                UncreateScene(s.Key);
            }
        }

        public void UnloadAllScene()
        {
            foreach(var s in mAllScene)
            {
                UnloadScene(s.Key);
            }
        }

        public IScene GetScene(int type)
        {
            return mAllScene.GetDefault(type, null)?.scene;
        }

        public bool IsSceneLoaded(int type)
        {
            if (mAllScene.TryGetValue(type, out var scene))
            {
                return scene.loadTask != null && scene.loadTask.isSuccess;
            }
            else
            {
                return false;
            }
        }

        public bool IsSceneCreated(int type)
        {
            if (mAllScene.TryGetValue(type, out var scene))
            {
                return scene.createTask != null && scene.createTask.isSuccess;
            }
            else
            {
                return false;
            }
        }

        public bool IsSceneActive(int type)
        {
            if (mAllScene.TryGetValue(type, out var scene))
            {
                return scene.phase == ScenePhase.Active;
            }
            else
            {
                return false;
            }
        }

        //destroy the visual elements of scene
        public void UncreateScene(int type)
        {
            DebugEx.FormatInfo("SceneManager::DestroyScene ----> {0}", type);
            if (mAllScene.TryGetValue(type, out var scene))
            {
                SetSceneActive(type, false);
                if (scene.createTask != null)
                {
                    if (scene.createTask.keepWaiting)
                    {
                        scene.createTask.Cancel();
                    }
                    else
                    {
                        scene.scene.Destroy();
                    }
                    if (scene.root != null)
                    {
                        GameObject.Destroy(scene.root);
                        scene.root = null;
                    }
                    scene.createTask = null;
                }
            }
        }

        public void UnloadScene(int type)
        {
            DebugEx.FormatInfo("SceneManager::UnloadScene ----> {0}", type);
            if (mAllScene.TryGetValue(type, out var scene))
            {
                if (scene.loadTask != null)
                {
                    if (scene.loadTask.keepWaiting)
                    {
                        scene.createTask.Cancel();
                    }
                    else
                    {
                        scene.scene.Unload();
                    }
                    scene.loadTask = null;
                }
            }
        }

        public AsyncTaskBase LoadScene(int type)
        {
            DebugEx.FormatInfo("SceneManager::LoadScene ----> {0}", type);
            if (mAllScene.TryGetValue(type, out var scene))
            {
                if (scene.loadTask != null && scene.loadTask.isFailOrCancel)
                {
                    scene.loadTask = null;
                }
                if (scene.loadTask == null)
                {
                    if (scene.phase == ScenePhase.Removed)
                    {
                        DebugEx.FormatWarning("SceneManager::LoadScene ----> {0} wrong state {1}", type, scene.phase);
                        return SimpleAsyncTask.AlwaysFail;
                    }
                    scene.loadTask = (this as MonoBehaviour).StartAsyncTask(scene.scene.ATLoad());
                }
                return scene.loadTask;
            }
            else
            {
                DebugEx.FormatWarning("SceneManager::LoadScene ----> {0} not found", type);
                return SimpleAsyncTask.AlwaysFail;
            }
        }

        public AsyncTaskBase CreateScene(int type)
        {
            DebugEx.FormatInfo("SceneManager::CreateScene ----> {0}", type);
            if (mAllScene.TryGetValue(type, out var scene))
            {
                if (scene.createTask != null && scene.createTask.isFailOrCancel)
                {
                    scene.createTask = null;
                }
                if (scene.createTask == null)
                {
                    if (!IsSceneLoaded(type))
                    {
                        DebugEx.FormatWarning("SceneManager::CreateScene ----> {0} not loaded", type);
                        return SimpleAsyncTask.AlwaysFail;
                    }
                    var go = new GameObject($"Scene{type}");
                    go.transform.position = new Vector3(99999, 0, 0);
                    go.transform.SetParent(transform, true);
                    scene.root = go;
                    scene.createTask = (this as MonoBehaviour).StartAsyncTask(scene.scene.ATCreate(go));
                }
                return scene.createTask;
            }
            else
            {
                DebugEx.FormatWarning("SceneManager::CreateScene ----> {0} not found", type);
                return SimpleAsyncTask.AlwaysFail;
            }
        }

        public bool SetSceneActive(int type, bool active)
        {
            DebugEx.FormatInfo("SceneManager::SetSceneActive ----> {0}, {1}", type, active);
            if (mAllScene.TryGetValue(type, out var scene))
            {
                if (scene.createTask == null || scene.createTask.keepWaiting || !scene.createTask.isSuccess)
                {
                    DebugEx.FormatWarning("SceneManager::SetSceneActive ----> {0} not created", type);
                    return false;
                }
                if (active && scene.phase != ScenePhase.Active)
                {
                    scene.phase = ScenePhase.Active;
                    scene.root.transform.position = new Vector3(0, 0, 0);
                    scene.scene.OnActive();
                    OnSceneActive(type, scene.scene);
                }
                else if (!active && scene.phase != ScenePhase.NotActive)
                {
                    scene.phase = ScenePhase.NotActive;
                    scene.root.transform.position = new Vector3(999999, 0, 0);
                    scene.scene.OnDeactive();
                    OnSceneDeactive(type, scene.scene);
                }
                return true;
            }
            else
            {
                DebugEx.FormatWarning("SceneManager::SetSceneActive ----> {0} not exists", type);
                return false;
            }
        }

        public void RegisterScene(int type, EL.IScene scene)
        {
            if (mAllScene.TryGetValue(type, out var oldScene))
            {
                oldScene.scene.Destroy();
            }
            mAllScene[type] = new SceneEntry()
            {
                scene = scene,
                loadTask = null,
                createTask = null,
                phase = ScenePhase.NotActive
            };
        }

        public void UnregisterScene(int type)
        {
            DebugEx.FormatInfo("SceneManager::RemoveScene ----> {0}", type);
            if (mAllScene.TryGetValue(type, out var scene))
            {
                if (scene.phase != ScenePhase.Removed)
                {
                    SetSceneActive(type, false);
                    UncreateScene(type);
                    UnloadScene(type);
                    scene.scene.BindManager(null);
                    scene.phase = ScenePhase.Removed;
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var s in mAllScene.Values)
            {
                if (s.phase == ScenePhase.Active)
                {
                    s.scene.Update(dt);
                }
            }

            List<int> mRemovedScene = null;
            foreach (var entry in mAllScene)
            {
                if (entry.Value.phase == ScenePhase.Removed)
                {
                    if (mRemovedScene == null)
                    {
                        mRemovedScene = new List<int>();
                    }
                    mRemovedScene.Add(entry.Key);
                }
            }
            if (mRemovedScene != null)
            {
                foreach (var sceneType in mRemovedScene)
                {
                    mAllScene.Remove(sceneType);
                }
            }
        }

        protected virtual void OnSceneActive(int type, EL.IScene scene)
        {

        }

        protected virtual void OnSceneDeactive(int type, EL.IScene scene)
        {

        }
    }
}