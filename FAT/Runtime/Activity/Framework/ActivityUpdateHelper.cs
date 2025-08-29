/*
 * @Author: tang.yan
 * @Description: 能够给各个活动实例提供帧级Update能力的帮助类
 * @Date: 2025-05-09 15:05:58
 */

using System.Collections.Generic;

namespace FAT
{
    public interface IActivityUpdate
    {
        void ActivityUpdate(float dt);
    }
    
    public class ActivityUpdateHelper
    {
        private readonly List<IActivityUpdate> _updateList = new(); //保证执行顺序 + 可索引遍历
        private readonly HashSet<IActivityUpdate> _set = new(); // 保证唯一性 快速判断是否已注册（去重）

        private readonly List<IActivityUpdate> _toAdd = new();
        private readonly List<IActivityUpdate> _toRemove = new();

        private bool _isUpdating = false;

        /// <summary>
        /// 注册一个对象到帧更新系统（仅当未注册过）
        /// </summary>
        public void Register(IActivityUpdate obj)
        {
            if (_set.Contains(obj)) return; // 已在更新队列中，无需再加

            if (_isUpdating)
            {
                // 如果当前帧正在更新，延迟添加
                if (!_toAdd.Contains(obj)) _toAdd.Add(obj);
                // 防止同一帧中同时调用了注册和注销方法的情况，先被调用的方法会失效，后被调用的方法会保留
                _toRemove.Remove(obj);  //移除可能存在的注销请求
            }
            else
            {
                _updateList.Add(obj);
                _set.Add(obj);
            }
        }

        /// <summary>
        /// 注销一个对象（仅当已注册）
        /// </summary>
        public void Unregister(IActivityUpdate obj)
        {
            if (!_set.Contains(obj)) return; // 未注册过，跳过

            if (_isUpdating)
            {
                if (!_toRemove.Contains(obj)) _toRemove.Add(obj);
                // 防止同一帧中同时调用了注册和注销方法的情况，先被调用的方法会失效，后被调用的方法会保留
                _toAdd.Remove(obj);     //移除可能存在的注册请求
            }
            else
            {
                _updateList.Remove(obj);
                _set.Remove(obj);
            }
        }

        /// <summary>
        /// 每帧驱动调用。外部应在 MonoBehaviour 的 Update() 中调用本方法
        /// </summary>
        public void UpdateAll(float deltaTime)
        {
            _isUpdating = true;

            for (int i = 0; i < _updateList.Count; i++)
            {
                _updateList[i].ActivityUpdate(deltaTime);
            }

            _isUpdating = false;

            // 执行延迟注销
            foreach (var obj in _toRemove)
            {
                if (_set.Contains(obj))
                {
                    _updateList.Remove(obj);
                    _set.Remove(obj);
                }
            }
            _toRemove.Clear();

            // 执行延迟注册
            foreach (var obj in _toAdd)
            {
                if (!_set.Contains(obj))
                {
                    _updateList.Add(obj);
                    _set.Add(obj);
                }
            }
            _toAdd.Clear();
        }

        public void Clear()
        {
            _updateList.Clear();
            _set.Clear();
            _toAdd.Clear();
            _toRemove.Clear();
        }

        public int Count => _updateList.Count;
    }
}