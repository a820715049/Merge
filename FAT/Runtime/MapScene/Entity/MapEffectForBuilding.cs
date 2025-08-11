/*
 * @Author: tang.yan
 * @Description: 场景建筑物特效
 * @Date: 2025-04-25 15:04:02
 */
using UnityEngine;
using UnityEngine.Rendering;
using EL;

namespace FAT
{
    public class MapEffectForBuilding
    {
        private Camera _parentCamera;
        private ChildCameraData _cameraAll;    //能渲染场景所有内容的子相机
        private ChildCameraData _cameraBuilding;    //仅能渲染场景建筑物的子相机

        public void ResetClear()
        {
            _cameraAll?.Dispose();
            _cameraBuilding?.Dispose();
            _cameraAll = null;
            _cameraBuilding = null;
        }
        
        public void SetCameraEnable(bool isEnable)
        {
            if (_cameraAll == null || _cameraBuilding == null || _parentCamera == null)
                return;
            //开关节点
            _cameraAll.SetCameraEnable(isEnable);
            _cameraBuilding.SetCameraEnable(isEnable);
            //开关界面
            if (isEnable)
                UIManager.Instance.OpenWindow(UIConfig.UIBuildingEffect);
            else
                UIManager.Instance.CloseWindow(UIConfig.UIBuildingEffect);
            //同步相机参数
            var size = _parentCamera.orthographicSize;
            _cameraAll.SyncOrthographicSize(size);
            _cameraBuilding.SyncOrthographicSize(size);
            //只在相机关闭时才关闭后处理渲染
            if (!isEnable)
                _StopPostProcess();
        }
        
        //针对_buildingRT进行后处理
        public void StartPostProcess()
        {
            _cameraBuilding.StartPostProcess();
        }

        private void _StopPostProcess()
        {
            _cameraBuilding?.StopPostProcess();
        }

        public RenderTexture GetCameraBuildingRT()
        {
            return _cameraBuilding.GetRenderTexture();
        }
        
        public RenderTexture GetCameraAllRT()
        {
            return _cameraAll.GetRenderTexture();
        }

        public MapEffectForBuilding(Camera mainCamera)
        {
            if (mainCamera == null)
                return;
            _parentCamera = mainCamera;
            var canvasRect = UIManager.Instance.CanvasRoot.rect;
            _cameraAll = new ChildCameraData(_parentCamera, "CameraAll", -1, (int)canvasRect.width, (int)canvasRect.height, false);
            _cameraBuilding = new ChildCameraData(_parentCamera, "CameraBuilding", LayerMask.GetMask("Building"),
                (int)canvasRect.width / 4, (int)canvasRect.height / 4, true);
        }
        
        //运行时从主相机拷贝创建出来的子相机数据类
        private class ChildCameraData
        {
            private Camera _camera;
            private RenderTexture _renderTexture;
            private MapEffectHelper _helper;

            public void SetCameraEnable(bool isEnable)
            {
                _camera.gameObject.SetActive(isEnable);
                _camera.enabled = isEnable;
            }

            public void SyncOrthographicSize(float size)
            {
                _camera.orthographicSize = size;
            }
            
            public void StartPostProcess()
            {
                if (_helper != null)
                {
                    _helper.StartPostProcess(_ReqReadBack);
                }
            }
            
            public void StopPostProcess()
            {
                if (_helper != null)
                {
                    _helper.StopPostProcess();
                }
            }

            public RenderTexture GetRenderTexture()
            {
                return _renderTexture;
            }

            public ChildCameraData(Camera mainCamera, string name, int mask, int rtWidth, int rtHeight, bool needHelper)
            {
                var go = new GameObject(name);
                go.transform.SetParent(mainCamera.transform);
                _camera = go.AddComponent<Camera>();
                _camera.CopyFrom(mainCamera);
                if (mask >= 0) _camera.cullingMask = mask;
                _camera.backgroundColor = Color.clear;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                //RT图new RenderTexture
                _renderTexture = RenderTexture.GetTemporary(rtWidth, rtHeight, 0, RenderTextureFormat.ARGB32);
                _camera.targetTexture = _renderTexture;
                //特效helper
                //注意这里挂载了helper后 其OnRenderImage输出的结果会同步到_buildingRT上面
                if (needHelper)
                    _helper = go.AddComponent<MapEffectHelper>();
            }
            
            private void _ReqReadBack()
            {
                //Unity 会自动把这次请求排到当前帧所有渲染命令之后执行，等 GPU 真正把数据写完再触发OnMaskReadback
                AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.RGBA32, _OnMaskReadback);
            }
            
            private void _OnMaskReadback(AsyncGPUReadbackRequest req)
            {
                if (req.hasError) return;
                var pixels = req.GetData<Color32>();
                int w = _renderTexture.width;
                int h = _renderTexture.height;
                //通知界面将UV坐标转成UI坐标
                MessageCenter.Get<MSG.MAP_EFFECT_RT_READ_BACK>().Dispatch(pixels, w, h);
            }
            
            public void Dispose()
            {
                if (_camera != null)
                {
                    _camera.enabled = false;
                    // 断开引用
                    _camera.targetTexture = null;
                    UnityEngine.Object.Destroy(_camera.gameObject);
                }
                _camera = null;
                
                // 回收 RT
                if (_renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(_renderTexture);
                }
                _renderTexture = null;
            }
        }
    }
}