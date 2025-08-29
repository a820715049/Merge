// ==================================================
// // File: UIBPReward.cs
// // Author: liyueran
// // Date: 2025-06-23 15:06:43
// // Desc: $发奖界面
// // ==================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIBPReward : UIBase, INavBack
    {
        public float animTime = 1f;
        public AnimationCurve animCurve;


        [SerializeField] private UICommonItem rewardItem;
        [SerializeField] private GameObject privilegeRoot;
        [SerializeField] private GameObject privilegeTxt;
        [SerializeField] private GameObject scrollView;
        [SerializeField] private GameObject expItem;
        [SerializeField] private GameObject privilegeEffect;
        private RectTransform _content;
        private RectTransform _privilegePos;
        private TextMeshProUGUI _privilegeText;
        private TextMeshProUGUI _expCountTxt;
        private Animator _privilegeAnim;
        private Animator _privilegeTxtAnim;
        private TextProOnACircle _titleCircle;
        private NonDrawingGraphic _block;
        private UIImageRes _icon;
        private CanvasGroup _scrollViewCanvasGroup;
        private ScrollRect _scrollRect;
        private Image _privilegeImg;
        private UIImageRes _privilegeRes;

        // 活动实例 
        private BPActivity _activity;

        private PoolMapping.Ref<List<RewardCommitData>> _container;
        private List<RewardConfig> _rewardList = new(); // 界面上显示的奖励

        private bool onlyExp;
        private int _expCount;

        private Dictionary<int, UICommonItem> _rewardItemDict = new(); // key: id
        private string rewardItemKey = "bp_reward_item";

        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("Content/PrivilegeRoot/text", out _privilegeText);
            transform.Access("Content/Scroll View/Viewport/Content", out _content);
            transform.Access("Content/Scroll View", out _scrollViewCanvasGroup);
            transform.Access("Content/Scroll View", out _scrollRect);
            transform.Access("Content/expItem/Icon/privilegePos", out _privilegePos);
            transform.Access("Content/expItem/Count", out _expCountTxt);
            transform.Access("Content/PrivilegeRoot/privilege", out _privilegeAnim);
            transform.Access("Content/PrivilegeRoot/text", out _privilegeTxtAnim);
            transform.Access("Content/PrivilegeRoot/privilege", out _privilegeImg);
            transform.Access("Content/PrivilegeRoot/privilege", out _privilegeRes);
            transform.Access("Content/titleRoot/Title", out _titleCircle);
            transform.Access("block", out _block);
            transform.Access("Content/expItem/Icon", out _icon);
        }

        private void AddButton()
        {
            transform.AddButton("Mask", OnClickClaim);
        }


        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (BPActivity)items[0];
            _container = (PoolMapping.Ref<List<RewardCommitData>>)items[1];
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCd);
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnPreOpen()
        {
            // 领奖界面音效
            Game.Manager.audioMan.TriggerSound("BattlePassReward");
            
            _titleCircle.SetText(I18N.Text("#SysComDesc1359"));
            var token = fat.conf.Data.GetObjBasic(_activity.ConfD.ScoreId);
            _icon.SetImage(token.Icon);
            
            var taskCompleteVisual = _activity.TaskRefreshPopup.visual;
            if (taskCompleteVisual.Theme.AssetInfo.TryGetValue("privilegeBg", out var privilegeKey))
            {
                _privilegeRes.SetImage(privilegeKey);
            }

            _content.gameObject.SetActive(true);

            // 默认隐藏
            privilegeRoot.SetActive(false);

            var privilegeInfo = _activity.GetBpPackInfoByType(BPActivity.BPPurchaseType.Luxury).PrivilegeInfo;
            var tokenIcon = UIUtility.FormatTMPString(_activity.ConfD.ScoreId);
            _privilegeText.SetText($"{tokenIcon}{privilegeInfo * 1f / 10f}");
            privilegeEffect.SetActive(false);

            _rewardList.Clear();

            // 默认不可交互
            _scrollViewCanvasGroup.interactable = false;
            _scrollViewCanvasGroup.blocksRaycasts = false;

            onlyExp = true;

            // 区分是否需要显示飞奖励
            var idHash = new HashSet<int>();
            foreach (var rewardCommitData in _container.obj)
            {
                if (idHash.Contains(rewardCommitData.rewardId))
                {
                    var reward = _rewardList.FirstOrDefault(x => x.Id == rewardCommitData.rewardId);
                    if (reward != null)
                    {
                        reward.Count += rewardCommitData.rewardCount;
                    }
                }
                else
                {
                    _rewardList.Add(new RewardConfig
                    {
                        Id = rewardCommitData.rewardId,
                        Count = rewardCommitData.rewardCount
                    });


                    idHash.Add(rewardCommitData.rewardId);

                    if (onlyExp && rewardCommitData.rewardId != _activity.ConfD.ScoreId)
                    {
                        onlyExp = false;
                    }
                }
            }

            _expCount = onlyExp ? _rewardList[0].Count : 0;

            if (onlyExp)
            {
                expItem.SetActive(true);
                scrollView.SetActive(false);

                // 特权显示判断（付费二档 且不是因为购买获得的经验值）
                if (_activity.PurchaseState == BPActivity.BPPurchaseState.Luxury &&
                    _container.obj[0].reason != ReasonString.purchase)
                {
                    privilegeRoot.SetActive(true);
                }

                _expCountTxt.SetText($"{_expCount}");
            }
            // 结束的时候购买付费 一次发多个奖励 或者 循环宝箱多个奖励
            else
            {
                expItem.SetActive(false);
                scrollView.SetActive(true);

                PreparePool();
                if (_rewardList.Count > 9)
                {
                    _scrollViewCanvasGroup.interactable = true;
                    _scrollViewCanvasGroup.blocksRaycasts = true;
                }

                // 根据传入的界面上显示的奖励的数据 初始化生成 Item
                foreach (var reward in _rewardList)
                {
                    // 创建Item
                    GameObjectPoolManager.Instance.CreateObject(rewardItemKey, _content, obj =>
                    {
                        obj.SetActive(true);
                        var item = obj.GetComponent<UICommonItem>();
                        item.Setup();
                        item.Refresh(reward.Id, reward.Count);

                        _rewardItemDict.Add(reward.Id, item);
                    });
                }

                _scrollRect.verticalNormalizedPosition = 1;
            }
        }

        private void PreparePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(rewardItemKey))
            {
                return;
            }

            GameObjectPoolManager.Instance.PreparePool(rewardItemKey, rewardItem.gameObject);
        }


        protected override void OnPostOpen()
        {
            // 判断是否购买付费二档 并且 不是因为购买获得的积分
            if (onlyExp && _activity.PurchaseState == BPActivity.BPPurchaseState.Luxury &&
                _container.obj[0].reason != ReasonString.purchase)
            {
                PrivilegeAnim();
            }
        }

        private Sequence _privilegeAnimSeq = null;

        private void PrivilegeAnim()
        {
            SetBlock(true);
            var oriPos = privilegeTxt.transform.position;

            // 横幅消失
            _privilegeAnim.SetBool("Punch", true);
            _privilegeAnimSeq?.Kill();

            _privilegeAnimSeq = DOTween.Sequence();
            _privilegeAnimSeq.AppendInterval(0.2f);
            // 文字变大
            _privilegeAnimSeq.AppendCallback(() => { _privilegeTxtAnim.SetTrigger("Punch"); });
            _privilegeAnimSeq.AppendInterval(0.2f);
            _privilegeAnimSeq.Append(
                privilegeTxt.transform.DOMove(_privilegePos.position, animTime).SetEase(animCurve)
                    .OnComplete(() =>
                    {
                        // 奖励加倍音效
                        Game.Manager.audioMan.TriggerSound("BattlePassRewardDouble");

                        privilegeEffect.SetActive(true);
                        var id = _activity.GetCurDetailConfig().PackLuxury;
                        var info = Game.Manager.configMan.GetBpPackInfoConfig(id).PrivilegeInfo;
                        _expCountTxt.SetText($"{_expCount * info / 10}");
                        privilegeTxt.SetActive(false);
                        privilegeTxt.transform.position = oriPos;

                        SetBlock(false);
                    }));
            _privilegeAnimSeq.OnKill(() =>
            {
                privilegeTxt.SetActive(false);
                privilegeTxt.transform.position = oriPos;
                privilegeTxt.transform.localScale = new Vector3(1f, 1f, 1f);
            });
        }

        private void OnClickClaim()
        {
            // 飞奖励 这里默认 如果有多个奖励 一定不会有经验值
            if (onlyExp)
            {
                UIFlyUtility.FlyReward(_container.obj[0], expItem.transform.position, () =>
                {
                    // 播放进度条动画
                    var ui = UIManager.Instance.TryGetUI(UIConfig.UIBPMain);
                    if (ui != null && ui is UIBPMain main)
                    {
                        main.Progress.PlayProgressAnim();
                    }
                }, 85f);
            }
            else
            {
                _content.gameObject.SetActive(false);
                // 飞奖励
                foreach (var reward in _container.obj)
                {
                    UIFlyUtility.FlyReward(reward,
                        TryGetItem(reward.rewardId, out var item) ? item.transform.position : Vector3.zero);
                }
            }

            Close();
        }

        private bool TryGetItem(int id, out UICommonItem item)
        {
            return _rewardItemDict.TryGetValue(id, out item);
        }

        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
            if (IsBlock)
            {
                SetBlock(false);
            }

            foreach (var item in _rewardItemDict.Values)
            {
                GameObjectPoolManager.Instance.ReleaseObject(rewardItemKey, item.gameObject);
            }

            _rewardItemDict.Clear();
            _container.Free();
            onlyExp = false;
            _expCount = 0;

            _privilegeAnim.SetBool("Punch", false);
            _privilegeImg.color = new Color(1f, 1f, 1f, 1f);
            _privilegeImg.transform.localScale = new Vector3(1f, 1f, 1f);
            privilegeEffect.SetActive(false);
            privilegeTxt.SetActive(true);
            privilegeTxt.transform.localScale = new Vector3(1f, 1f, 1f);
        }


        private void RefreshCd()
        {
            if (_activity == null) return;
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
        }

        public void OnNavBack()
        {
            if (IsBlock)
            {
                return;
            }

            OnClickClaim();
        }

        public bool IsBlock => _block.raycastTarget;

        private void SetBlock(bool value)
        {
            _block.raycastTarget = value;
        }
    }
}