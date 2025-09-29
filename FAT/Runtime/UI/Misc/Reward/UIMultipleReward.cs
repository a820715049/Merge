/*
 * @Author: ange.shentu
 * @Date: 2025-09-16
 * @Desc: 通用多奖励弹窗（支持三列网格、3.5行可滚、点击遮罩领取、非宝箱先飞、宝箱后开）
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIMultipleReward : UIBase, INavBack
    {
        [SerializeField] private UICommonItem rewardItem;
        [SerializeField] private RectTransform content;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button maskBtn;
        [SerializeField] private NonDrawingGraphic scrollGraphic;

        private readonly string _poolKey = "multiple_reward_item";
        private readonly Dictionary<int, UICommonItem> _rewardItemDict = new();
        private readonly List<UICommonItem> _rewardItemList = new();

        // 入参
        private List<RewardCommitData> _rewardCommitList;
        private bool _mergeSame = true;


        // 运行态
        private List<RewardCommitData> _displayList = new();
        private bool _isClaimed = false;

        protected override void OnCreate()
        {
            if (maskBtn != null)
            {
                maskBtn.onClick.AddListener(_OnClickClaim);
            }
        }

        public void OnNavBack()
        {
            _OnClickClaim();
        }

        /// <summary>
        /// 打开参数说明（OpenWindow 参数映射）
        /// 用法：UIManager.Instance.OpenWindow(UIConfig.UIMultipleReward, rewards, mergeSame);
        /// - items[0]: List&lt;RewardCommitData&gt; rewards
        ///   界面展示并用于飞行动画的奖励列表（必传）。
        /// - items[1]: bool mergeSame
        ///   是否合并相同奖励ID的数量，默认 true；传 false 则不合并。
        /// 备注：
        /// - 展示为三列网格；超过 9 个时开启滚动；点击遮罩领取；非宝箱先飞、宝箱后弹。
        /// - 标题由 Prefab 自行配置，不由脚本入参控制。
        /// </summary>
        protected override void OnParse(params object[] items)
        {
            _rewardCommitList = items != null && items.Length > 0 ? items[0] as List<RewardCommitData> : null;
            _mergeSame = items != null && items.Length > 1 && items[1] is bool b ? b : true;
        }

        protected override void OnPreOpen()
        {
            if (content != null)
            {
                content.gameObject.SetActive(true);
            }
            if (scrollRect != null)
            {
                scrollRect.enabled = true;
                scrollRect.inertia = true;
            }
            _PreparePool();
            _BuildDisplayList();
            _RefreshGrid();
            _SetScrollInteractable(_displayList.Count > 9);
            _ResizeContentByItemCount();
        }

        protected override void OnPostClose()
        {
            if (_rewardItemList.Count > 0)
            {
                foreach (var item in _rewardItemList)
                {
                    GameObjectPoolManager.Instance.ReleaseObject(_poolKey, item.gameObject);
                }
                _rewardItemList.Clear();
            }
            _rewardItemDict.Clear();
            _displayList.Clear();
            _isClaimed = false;
        }



        private void _PreparePool()
        {
            if (rewardItem == null) return;
            if (GameObjectPoolManager.Instance.HasPool(_poolKey)) return;
            GameObjectPoolManager.Instance.PreparePool(_poolKey, rewardItem.gameObject);
        }

        private void _BuildDisplayList()
        {
            _displayList.Clear();
            if (_rewardCommitList == null || _rewardCommitList.Count == 0) return;

            if (_mergeSame)
            {
                // 合并相同ID
                var dict = new Dictionary<int, RewardCommitData>();
                foreach (var r in _rewardCommitList)
                {
                    if (r == null || r.rewardId <= 0) continue;
                    if (Game.Manager.objectMan.IsType(r.rewardId, ObjConfigType.RandomBox))
                    {
                        // 宝箱不合并，逐一展示以便数量感知
                        _displayList.Add(r);
                        continue;
                    }
                    if (dict.TryGetValue(r.rewardId, out var exist))
                    {
                        exist.rewardCount += r.rewardCount;
                    }
                    else
                    {
                        dict[r.rewardId] = new RewardCommitData(r._l, r._f, r._m)
                        {
                            rewardId = r.rewardId,
                            rewardType = r.rewardType,
                            rewardCount = r.rewardCount,
                            reason = r.reason,
                            context = r.context,
                            flags = r.flags,
                            WaitCommit = r.WaitCommit,
                            isFake = r.isFake,
                        };
                    }
                }
                _displayList.AddRange(dict.Values);
            }
            else
            {
                _displayList.AddRange(_rewardCommitList);
            }
        }

        private void _RefreshGrid()
        {
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                if (_displayList.Count <= 3)
                {
                    grid.padding.top = 150;
                }
                else
                {
                    grid.padding.top = 50;
                }
            }

            // 清理旧实例
            if (_rewardItemList.Count > 0)
            {
                foreach (var item in _rewardItemList)
                {
                    GameObjectPoolManager.Instance.ReleaseObject(_poolKey, item.gameObject);
                }
                _rewardItemList.Clear();
            }
            _rewardItemDict.Clear();

            if (_displayList.Count == 0)
            {
                return;
            }

            foreach (var r in _displayList)
            {
                var rr = r; // 捕获
                GameObjectPoolManager.Instance.CreateObject(_poolKey, content, obj =>
                {
                    obj.SetActive(true);
                    var item = obj.GetComponent<UICommonItem>();
                    item.Setup();
                    item.Refresh(rr.rewardId, rr.rewardCount);
                    _rewardItemList.Add(item);
                    if (!_rewardItemDict.ContainsKey(rr.rewardId))
                    {
                        _rewardItemDict.Add(rr.rewardId, item);
                    }
                });
            }

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
            _ResizeContentByItemCount();
        }

        private void _SetScrollInteractable(bool interactable)
        {
            if (scrollRect != null)
            {
                scrollRect.enabled = interactable; // 少于9个不可拖动
                if (scrollGraphic != null)
                {
                    scrollGraphic.enabled = interactable;
                }
                if (!interactable)
                {
                    scrollRect.velocity = Vector2.zero;
                }
            }
        }

        private void _ResizeContentByItemCount()
        {
            if (content == null || grid == null) return;
            int count = _displayList?.Count ?? 0;
            if (count <= 0) return;
            int cols = 3;
            int rows = Mathf.CeilToInt(count / (float)cols);
            var cell = grid.cellSize;
            var spacing = grid.spacing;
            var pad = grid.padding;
            float h = pad.top + pad.bottom + rows * cell.y + Mathf.Max(0, rows - 1) * spacing.y;
            var rt = content;
            var size = rt.sizeDelta;
            rt.sizeDelta = new Vector2(size.x, h);
        }

        private void _OnClickClaim()
        {
            if (_isClaimed) return;
            _isClaimed = true;
            content.gameObject.SetActive(false);
            var nonChest = new List<RewardCommitData>();
            var chests = new List<RewardCommitData>();
            foreach (var r in _rewardCommitList)
            {
                if (r == null) continue;
                if (Game.Manager.objectMan.IsType(r.rewardId, ObjConfigType.RandomBox))
                {
                    chests.Add(r);
                }
                else
                {
                    nonChest.Add(r);
                }
            }

            if (nonChest.Count == 0)
            {
                _CommitChestList(chests);
                Close();
                return;
            }

            int finished = 0;
            int total = nonChest.Count;
            Action onOneFinish = () =>
            {
                finished++;
                if (finished >= total)
                {
                    _CommitChestList(chests);
                    Close();
                }
            };

            foreach (var r in nonChest)
            {
                var from = _TryGetItemPos(r.rewardId);
                UIFlyUtility.FlyReward(r, from, onOneFinish);
            }
        }

        private Vector3 _TryGetItemPos(int id)
        {
            if (_rewardItemDict.TryGetValue(id, out var item))
            {
                return item.transform.position;
            }
            return Vector3.zero;
        }

        private void _CommitChestList(List<RewardCommitData> list)
        {
            if (list == null || list.Count == 0) return;
            // 将宝箱的commit放到最后，交给SpecialRewardMan顺序弹出
            foreach (var r in list)
            {
                UIFlyUtility.FlyReward(r, Vector3.zero);
            }
        }
    }
}


