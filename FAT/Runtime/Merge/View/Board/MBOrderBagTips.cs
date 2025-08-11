/*
 *@Author:chaoran.zhang
 *@Desc:背包中有订单所需物品时弹出tips
 *@Created Time:2024.09.04 星期三 15:57:07
 */

using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using FAT.Merge;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBOrderBagTips : MonoBehaviour
    {
        [SerializeField] private UIImageRes _item;
        [SerializeField] private GameObject _node;
        private Sequence _sequence;
        private int _curID = -1;

        public void Setup()
        {
        }

        public void InitOnPreOpen()
        {
            MessageCenter.Get<MSG.UI_BOARD_DIRTY>().AddListener(Refresh);
            Refresh();
        }

        public void Refresh()
        {
            return; //暂时取消这个功能
            var orderStateCache = BoardViewWrapper.GetBoardOrderRequireItemStateCache();
            if (orderStateCache == null)
                return;
            var going = new List<int>();

            foreach (var item in Game.Manager.bagMan.GetBagGirdDataList((int)BagMan.BagType.Item))
                if (orderStateCache.TryGetValue(item.ItemTId, out var orderState))
                    if (orderState != 1)
                        going.Add(item.ItemTId);

            if (going.Count > 0)
            {
                going.Sort((a, b) => b - a);
                var info = Game.Manager.objectMan.GetBasicConfig(going.First());
                if (going.First() == _curID)
                    return;
                _curID = going.First();
                _item.SetImage(info.Icon);
                _node.SetActive(true);
                PlayTween();
                DataTracker.bag_remind.Track(_curID);
                return;
            }

            _node.SetActive(false);
            _sequence?.Kill();
            _curID = -1;
        }

        private void PlayTween()
        {
            if (_sequence != null) _sequence.Kill();
            _node.transform.eulerAngles = Vector3.zero;
            _node.transform.localScale = Vector3.zero;
            _node.SetActive(true);
            _sequence = DOTween.Sequence();
            _sequence.Append(_node.transform.DOScale(Vector3.one, 0.2f));
            _sequence.Append(_node.transform.DOPunchRotation(8f * Vector3.back, 1f, 5));
            _sequence.Append(_node.transform.DOPunchRotation(8f * Vector3.back, 1f, 5));
            _sequence.Append(_node.transform.DOScale(Vector3.zero, 0.2f));
            _sequence.InsertCallback(6f, () => _node.SetActive(true));
            _sequence.SetLoops(-1);
        }

        public void CleanupOnPostClose()
        {
            MessageCenter.Get<MSG.UI_BOARD_DIRTY>().RemoveListener(Refresh);
            _node.gameObject.SetActive(false);
            _curID = -1;
            if (_sequence != null)
                _sequence.Kill();
        }
    }
}