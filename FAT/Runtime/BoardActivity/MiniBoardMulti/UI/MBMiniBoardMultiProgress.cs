using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using FAT.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBMiniBoardMultiProgress : MonoBehaviour
    {
        public float pacing;
        public float itemWidth;
        public float width;
        private RectMask2D _mask;
        private Transform _content;
        public GameObject _orderItem;
        private MiniBoardMultiActivity _activity;
        private TextMeshProUGUI _desc;
        private Animator _descAni;
        private GameObject _effect;
        private bool _hasInit;
        private float _delay = 0.16f;
        private List<MBMiniBoardMultiProgressItem> _list = new();

        public void SetUp()
        {
            transform.Access("TipsNode/Tips", out _desc);
            transform.Access("TipsNode", out _descAni);
            transform.Access("ProgressScroll/ViewPort/Content/ProgressBar/Mask", out _mask);
            _content = transform.Find("ProgressScroll/ViewPort/Content");
            _effect = transform.GetChild(1).GetChild(1).gameObject;
        }

        public void CheckInit(MiniBoardMultiActivity activity)
        {
            if (activity == _activity) return;
            _activity = activity;
            Init();
        }

        public void Init()
        {
            _effect.SetActive(false);
            var info = Game.Manager.configMan
                .GetEventMiniBoardMultiGroupConfig(_activity.GroupId).InfoId;
            if (info.Count < 1) return;
            var id = info[^1];
            if (Game.Manager.configMan.GetEventMiniBoardMultiInfoConfig(id) == null) return;
            if (_activity == null) return;
            _hasInit = true;
            for (var i = 0;
                 i < Game.Manager.configMan.GetEventMiniBoardMultiInfoConfig(id).LevelItem.Count;
                 i++)
            {
                if (_list.Count > i) return;
                var obj = Instantiate(_orderItem, _content.transform)
                    .GetComponent<MBMiniBoardMultiProgressItem>();
                obj.Init();
                obj.SetUp(Game.Manager.configMan.GetEventMiniBoardMultiInfoConfig(id).LevelItem[i], i, _activity);
                obj.gameObject.SetActive(true);
                _list.Add(obj);
            }
        }

        public void Refresh()
        {
            SetText();
            RefreshProgress();
            foreach (var item in _list) item.Refresh(_activity);
        }

        private void SetText()
        {
            if (!Game.Manager.activity.mapR.ContainsKey(_activity))
            {
                _desc.gameObject.SetActive(false);
                _effect.SetActive(false);
                return;
            }

            if (!Game.Manager.miniBoardMultiMan.IsValid) return;
            if (!Game.Manager.miniBoardMultiMan.CheckHasNextBoard() &&
                Game.Manager.miniBoardMultiMan.GetCurUnlockItemMaxLevel() + 1 >=
                Game.Manager.miniBoardMultiMan.GetCurInfoConfig().LevelItem.Count())
            {
                _desc.gameObject.SetActive(true);
                _desc.text = I18N.Text("#SysComDesc489");
                _effect.SetActive(true);
            }
            else
            {
                if (Game.Manager.miniBoardMultiMan.GetCurInfoConfig() != null)
                {
                    _desc.gameObject.SetActive(true);
                    var cfg = Game.Manager.objectMan.GetBasicConfig(Game.Manager.miniBoardMultiMan.GetCurInfoConfig()
                        .LevelItem[0]);
                    var sprite = "<sprite name=\"" + cfg.Icon.ConvertToAssetConfig().Asset + "\">";
                    _desc.text = I18N.FormatText("#SysComDesc484", sprite);
                }

                _effect.SetActive(false);
            }
        }

        public void UnlockNew(Item item)
        {
            IEnumerator unlock()
            {
                yield return new WaitForSeconds(0.1f);
                var from = BoardViewManager.Instance.CoordToWorldPos(item.coord);
                var index = Game.Manager.miniBoardMultiMan.GetCurInfoConfig().LevelItem.IndexOf(item.tid);
                if (index == -1) yield break;
                UIFlyUtility.FlyCustom(item.config.Id, 1, from,
                    _list[index].transform.position,
                    FlyStyle.Common,
                    FlyType.None, () =>
                    {
                        if (!Game.Manager.miniBoardMultiMan.IsValid) return;
                        _list[index].Unlock();
                        CheckAllFinish();
                    }, size: 136f);
            }

            StartCoroutine(unlock());
            var max = (_content.transform as RectTransform).rect.width;
            var cur = 50f + (pacing + itemWidth) * Game.Manager.miniBoardMultiMan.GetCurUnlockItemMaxLevelEntry() +
                      itemWidth;
            var offset = width > cur ? 0f : max - width;
            _content.transform.DOLocalMoveX(offset > 0 ? -offset - width / 2 : -width / 2, 0.1f);
            var right = 0f;
            var count = Game.Manager.miniBoardMultiMan.GetCurInfoConfig().LevelItem.Count -
                        Game.Manager.miniBoardMultiMan.GetCurUnlockItemMaxLevelEntry();
            if (Game.Manager.miniBoardMultiMan.CheckHasNextBoard())
            {
                if (count > 0) right = itemWidth / 2 + count * (itemWidth + pacing);
            }
            else
            {
                if (count > 1) right = itemWidth / 2 + (count - 1) * (itemWidth + pacing);
            }

            var z = _mask.padding.z;
            DOTween.To(() => z, x =>
            {
                z = x;
                _mask.padding = new Vector4(0, 0, z, 0);
            }, right, 0.1f);
        }

        public void MoveToEnd()
        {
            var max = (_content.transform as RectTransform).rect.width;
            var offset = max - width;
            if (offset > 0)
                _content.transform.DOLocalMoveX(-width / 2 - offset, 0.1f);
        }

        private void RefreshProgress()
        {
            var right = 0f;
            int count;
            if (!Game.Manager.activity.mapR.ContainsKey(_activity))
            {
                var id = Game.Manager.configMan.GetEventMiniBoardMultiGroupConfig(_activity.GroupId)?.InfoId[^1] ?? 0;
                var conf = Game.Manager.configMan.GetEventMiniBoardMultiInfoConfig(id);
                count = conf.LevelItem.Count() - _activity.UnlockMaxLevel;
                if (count > 1) right = itemWidth / 2 + (count - 1) * (itemWidth + pacing);
                _mask.padding = new Vector4(0, 0, right, 0);

                var max_ = (_content.transform as RectTransform).rect.width;
                var cur_ = 50f + (pacing + itemWidth) * _activity.UnlockMaxLevel +
                          itemWidth;
                if (max_ == 0) max_ = cur_;
                var offset_ = width > cur_ ? 0f : max_ - width;
                _content.transform.localPosition = new Vector3(offset_ > 0 ? -offset_ - width / 2 : -width / 2,
                    _content.transform.localPosition.y, _content.transform.localPosition.z);

                return;
            }

            count = Game.Manager.miniBoardMultiMan.GetCurInfoConfig().LevelItem.Count -
                    Game.Manager.miniBoardMultiMan.GetCurUnlockItemMaxLevelEntry();
            if (Game.Manager.miniBoardMultiMan.CheckHasNextBoard())
            {
                if (count > 0) right = itemWidth / 2 + count * (itemWidth + pacing);
            }
            else
            {
                if (count > 1) right = itemWidth / 2 + (count - 1) * (itemWidth + pacing);
            }

            _mask.padding = new Vector4(0, 0, right, 0);
            var max = (_content.transform as RectTransform).rect.width;
            var cur = 50f + (pacing + itemWidth) * Game.Manager.miniBoardMultiMan.GetCurUnlockItemMaxLevelEntry() +
                      itemWidth;
            if (max == 0) max = cur;
            var offset = width > cur ? 0f : max - width;
            _content.transform.localPosition = new Vector3(offset > 0 ? -offset - width / 2 : -width / 2,
                _content.transform.localPosition.y, _content.transform.localPosition.z);
        }

        private void CheckAllFinish()
        {
            if (Game.Manager.miniBoardMultiMan.GetCurUnlockItemMaxLevel() + 1 <
                Game.Manager.miniBoardMultiMan.GetCurInfoConfig().LevelItem.Count())
                return;
            if (Game.Manager.miniBoardMultiMan.CheckHasNextBoard())
                MessageCenter.Get<MSG.UI_MINI_BOARD_MULTI_FINISH>().Dispatch();
            if (!Game.Manager.miniBoardMultiMan.CheckHasNextBoard())
                StartCoroutine(SetTextChange());
        }

        public IEnumerator SetTextChange()
        {
            MessageCenter.Get<MSG.UI_MINI_BOARD_SHOW_END>().Dispatch();
            yield break;
        }

        public void StartTextChange()
        {
            StartCoroutine(ShowTextAnim());
        }

        private IEnumerator ShowTextAnim()
        {
            _descAni.SetTrigger("Show");
            yield return new WaitForSeconds(_delay);
            SetText();
        }

        public float GetKeyPosX()
        {
            return _list[Game.Manager.miniBoardMultiMan.GetCurInfoConfig().LevelItem.Count - 1].transform.position.x;
        }
    }
}
