/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏界面 珠子cell
 * @Date: 2024-09-27 12:09:06
 */

using EL;
using UnityEngine;

namespace MiniGame
{
    public class UIBeadCell
    {
        private Transform _root;
        private Transform _childNode;
        private UIImageRes _icon;
        private GameObject _guideHand;
        private (int x, int y) _coord;

        public void Prepare(Transform root, string imageAsset)
        {
            if (root == null) return;
            _root = root;
            _root.gameObject.SetActive(true);
            _childNode = _root.FindEx<Transform>("ChildNode");
            _icon = _root.FindEx<UIImageRes>("Icon");
            _icon.SetImage(imageAsset);
            _root.FindEx("Icon/GuideHand", out _guideHand);
        }

        public Transform GetRoot()
        {
            return _root;
        }

        public Transform GetChildNode()
        {
            return _childNode;
        }

        public void SetCoord((int, int) coord)
        {
            _coord = coord;
        }

        public (int, int) GetCoord()
        {
            return _coord;
        }

        public void SetGuideHandVisible(bool isShow)
        {
            _guideHand.SetActive(isShow);
        }
    }
}
