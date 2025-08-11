/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏界面 珠子底座cell
 * @Date: 2024-09-27 12:09:36
 */
using UnityEngine;
using EL;

namespace MiniGame
{
    public class UIBeadRootCell
    {
        private Transform _root;
        private Transform _beadNode;
        private UIImageRes _icon;

        public void Prepare(Transform root, string imageAsset)
        {
            if (root == null) return;
            _root = root;
            _root.gameObject.SetActive(true);
            _beadNode = _root.FindEx<Transform>("BeadNode");
            _icon = _root.FindEx<UIImageRes>("Icon");
            _icon.SetImage(imageAsset);
        }
        
        public Transform GetRoot()
        {
            return _root;
        }

        public Transform GetBeadNode()
        {
            return _beadNode;
        }
    }
}
