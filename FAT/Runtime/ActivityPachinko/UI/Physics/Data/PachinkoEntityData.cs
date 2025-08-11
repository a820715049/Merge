/*
 * @Author: tang.yan
 * @Description: 弹珠游戏-UI层面实体数据基类  
 * @Date: 2024-12-10 17:12:25
 */
using Box2DSharp.Dynamics;
using UnityEngine;
using EL;

namespace FAT
{
    public class PachinkoEntityData
    {
        protected RectTransform _root;
        protected RectTransform _iconTrans;
        protected Body _body;
        protected int _index;   //当前数据的index

        public void BindRoot(RectTransform root, int index)
        {
            _root = root;
            _index = index;
            AfterBindRoot();
        }

        protected virtual void AfterBindRoot()
        {
            _iconTrans = _root.FindEx<RectTransform>("Icon");
        }

        public void BindBody(Body body)
        {
            _body = body;
            AfterBindBody();
        }
        
        protected virtual void AfterBindBody() { }
        
        public RectTransform GetRoot()
        {
            return _root;
        }

        public RectTransform GetIconTrans()
        {
            return _iconTrans;
        }
        
        public Body GetBody()
        {
            return _body;
        }

        public int GetIndex()
        {
            return _index;
        }

        //传入world时代表自行销毁 不传时代表外部销毁
        public void ClearBody(World belongWorld = null)
        {
            belongWorld?.DestroyBody(_body);
            _body = null;
        }

        public void Reset()
        {
            OnReset();
        }

        protected virtual void OnReset() { }

        public void ExecuteColliderBegin(bool isDebug)
        {
            OnColliderBegin(isDebug);
        }
        
        protected virtual void OnColliderBegin(bool isDebug) { }
    }
}