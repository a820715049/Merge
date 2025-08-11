/*
 *@Author:chaoran.zhang
 *@Desc:迷你棋盘进度条
 *@Created Time:2024.08.13 星期二 13:56:40
 */

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.Merge;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBMiniBoardProgress : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private GameObject _orderItem;
        [SerializeField] private GameObject _mask;
        [SerializeField] private GameObject _fill;
        [SerializeField] private GameObject _content;
        [SerializeField] private GameObject _orderRoot;
        [SerializeField] private TextMeshProUGUI _desc;
        [SerializeField] private Animator _descAni;
        [SerializeField] private GameObject _effect;
        private List<MbMiniBoardProgressItem> _list = new();
        private int _maxLevel;
        private int _actId;
        private float _minX = -530f;
        private float _maxX = 0f;
        [SerializeField] private float _delay;

        public void SetUp()
        {
            SetText();
            if (_actId == Game.Manager.miniBoardMan.CurActivity.Id)
                return;
            _actId = Game.Manager.miniBoardMan.CurActivity.Id;
            var detail = Game.Manager.miniBoardMan.GetCurDetailConfig();
            _maxLevel = detail.LevelItem.Count;
            //进度条数字
            SetCount();
            //进度条item
            SetProgressItem();
        }

        private void SetCount()
        {
            _num.text = Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() + 1 + "/" + _maxLevel;
        }

        private void SetText()
        {
            if (Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() == _maxLevel - 1)
            {
                _desc.text = I18N.Text("#SysComDesc489");
                _effect.SetActive(true);
            }
            else
            {
                if (Game.Manager.miniBoardMan.GetCurDetailConfig() != null)
                {
                    var cfg = Game.Manager.objectMan.GetBasicConfig(Game.Manager.miniBoardMan.GetCurDetailConfig()
                        .LevelItem[0]);
                    var sprite = "<sprite name=\"" + cfg.Icon.ConvertToAssetConfig().Asset + "\">";
                    _desc.text = I18N.FormatText("#SysComDesc484", sprite);
                }

                _effect.SetActive(false);
            }
        }

        private void SetProgressItem()
        {
            if (_list.Count == 0)
                for (var i = 0; i < Game.Manager.miniBoardMan.GetCurDetailConfig().LevelItem.Count; i++)
                {
                    var obj = Instantiate(_orderItem, _orderRoot.transform)
                        .GetComponent<MbMiniBoardProgressItem>();
                    obj.SetUp(Game.Manager.miniBoardMan.GetCurDetailConfig().LevelItem[i], i);
                    _list.Add(obj);
                }

            SetLayout();
        }

        private void SetLayout()
        {
            var width = (_orderRoot.transform as RectTransform)?.rect.width ?? 0f;
            var rect = _content.transform as RectTransform;
            (_content.transform as RectTransform)?.rect.Set(rect.rect.x, rect.rect.y, width, rect.rect.height);
            rect = _fill.transform as RectTransform;
            (_content.transform as RectTransform)?.rect.Set(rect.rect.x, rect.rect.y, width, rect.rect.height);
            var tar = Vector2.zero;
            if (Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() == 0)
                tar = new Vector2(0, 1);
            else if (Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() == _maxLevel - 1)
                tar = Vector2.one;
            else
                tar = new Vector2(
                    (float)Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() / _maxLevel +
                    (float)1 / (2 * _maxLevel), 1);
            (_mask.transform as RectTransform).anchorMax = tar;
        }

        public void Refresh()
        {
            SetCount();
            SetText();
            RefreshLayout();
            RefreshItem();
        }

        private void RefreshLayout()
        {
            var cur = Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel();
            var offset = 435 - (18 + 44 + cur * (88 + 28));
            if (offset >= _maxX)
            {
                _content.transform.DOLocalMove(Vector3.zero, 0.1f);
            }
            else
            {
                if (offset >= _minX)
                    _content.transform.DOLocalMove(new Vector3(offset, 0, 0), 0.1f);
                else
                    _content.transform.DOLocalMove(new Vector3(_minX, 0, 0), 0.1f);
            }
        }

        private void RefreshItem()
        {
            foreach (var item in _list) item.Refresh();
        }

        public void UnlockNew(Item item)
        {
            IEnumerator unlock()
            {
                yield return new WaitForSeconds(0.1f);
                var from = BoardViewManager.Instance.CoordToWorldPos(item.coord);
                UIFlyUtility.FlyCustom(item.config.Id, 1, from,
                    _list[Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel()].transform.position, FlyStyle.Common,
                    FlyType.None, () =>
                    {
                        RefreshItem();
                        _list[Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel()].Unlock();
                        CheckAllFinish();
                    }, size: 136f);
            }

            RefreshLayout();
            StartCoroutine(unlock());
            var tar = Vector2.zero;
            if (Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() == 0)
                tar = new Vector2(0, 1);
            else if (Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() == _maxLevel - 1)
                tar = Vector2.one;
            else
                tar = new Vector2(
                    (float)Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() / _maxLevel +
                    (float)1 / (2 * _maxLevel), 1);
            DOTween.To(() => (_mask.transform as RectTransform).anchorMax,
                x => (_mask.transform as RectTransform).anchorMax = x,
                tar, 0.3f);
        }

        public void CheckAllFinish()
        {
            if (Game.Manager.miniBoardMan.GetCurUnlockItemMaxLevel() == _maxLevel - 1) StartCoroutine(SetTextChange());
        }

        public IEnumerator SetTextChange()
        {
            yield return new WaitForSeconds(0.8f);
            _descAni.SetTrigger("Show");
            yield return new WaitForSeconds(_delay);
            SetText();
        }

        public void End(int level, MiniBoardActivity activity)
        {
            _maxLevel = Game.Manager.configMan.GetEventMiniBoardDetailConfig(activity.DetailId).LevelItem.Count;
            if (_actId == activity.Id)
            {
                foreach (var item in _list) item.RefreshEnd(level);

                return;
            }

            if (level == _maxLevel - 1)
            {
                _desc.text = I18N.Text("#SysComDesc489");
            }
            else
            {
                if (activity != null)
                {
                    var cfg = Game.Manager.objectMan.GetBasicConfig(Game.Manager.configMan
                        .GetEventMiniBoardDetailConfig(activity.DetailId)
                        .LevelItem[0]);
                    var sprite = "<sprite name=\"" + cfg.Icon.ConvertToAssetConfig().Asset + "\">";
                    _desc.text = I18N.FormatText("#SysComDesc484", sprite);
                }
            }

            // _orderRoot.transform.DestroyAllChildren();
            // _list.Clear();
            for (var i = 0;
                 i < Game.Manager.configMan.GetEventMiniBoardDetailConfig(activity.DetailId).LevelItem.Count;
                 i++)
            {
                if (_list.Count <= i)
                {
                    var obj = Instantiate(_orderItem, _orderRoot.transform)
                        .GetComponent<MbMiniBoardProgressItem>();
                    _list.Add(obj);
                }
                _list[i].SetUp(Game.Manager.configMan.GetEventMiniBoardDetailConfig(activity.DetailId).LevelItem[i], i);
                _list[i].RefreshEnd(level);
            }

            var width = (_orderRoot.transform as RectTransform).rect.width;
            var rect = _content.transform as RectTransform;
            (_content.transform as RectTransform).rect.Set(rect.rect.x, rect.rect.y, width, rect.rect.height);
            rect = _fill.transform as RectTransform;
            (_content.transform as RectTransform).rect.Set(rect.rect.x, rect.rect.y, width, rect.rect.height);
            var tar = Vector2.zero;
            if (level == 0)
                tar = new Vector2(0, 1);
            else if (level == _maxLevel - 1)
                tar = Vector2.one;
            else
                tar = new Vector2(
                    (float)level / _maxLevel +
                    (float)1 / (2 * _maxLevel), 1);
            (_mask.transform as RectTransform).anchorMax = tar;
        }
    }
}