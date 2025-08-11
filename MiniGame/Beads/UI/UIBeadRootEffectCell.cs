/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏界面 珠子底座特效cell 
 * @Date: 2024-10-11 11:10:11
 */
using UnityEngine;
using EL;

namespace MiniGame
{
    public class UIBeadRootEffectCell
    {
        private Transform _root;
        private GameObject _effectGo;

        public void Prepare(Transform root)
        {
            if (root == null) return;
            _root = root;
            _root.gameObject.SetActive(true);
            _root.FindEx("Effect", out _effectGo);
        }
        
        public Transform GetRoot()
        {
            return _root;
        }
        
        public void SetEffectVisible(bool isShow)
        {
            _effectGo.SetActive(isShow);
        }
    }
}
