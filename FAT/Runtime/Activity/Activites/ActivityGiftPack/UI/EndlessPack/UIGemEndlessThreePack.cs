/*
 * @Author: tang.yan
 * @Description: 三格无限礼包钻石版界面 
 * @Date: 2024-11-08 14:11:16
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EL;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace FAT
{
    public class UIGemEndlessThreePack : UIBase
    {
        private Queue<UIGemEndlessThreePkgModule> _moduleList = new Queue<UIGemEndlessThreePkgModule>();
        private TMP_Text _remainTimeText;       //剩余时间文本
        private TextProOnACircle _titleText1;           //标题文本1
        private TMP_Text _titleText2;           //标题文本2
        private List<Vector2> _pointV2List = new List<Vector2>();   //记录格子移动点位
        private Vector2 _pointOutV2;        //格子向上移出时的目标位置
        private const int MaxCellNum = 4;   //最大格子数
        private Coroutine _tweenCoroutine;
        private Sequence _tweenSeq;
        private const float DurationTime = 0.6f;    //tween动画表现时间
        private const float WaitUnlockTime = 1.5f;    //解锁特效播完等待时间
        
        private Action WhenInit;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private PackGemEndlessThree pack;

        private bool _isDelayTween = false; //是否延迟tween动画表现 在随机宝箱开启时会延迟界面表现到随机宝箱领完后执行
        private bool _isBlock = false;      //记录是否在block
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/BtnClose", base.Close).FixPivot();
            //固定点位
            string pointPath = "Content/Panel/Reward/PointList/Point";
            for (int i = 0; i < MaxCellNum; i++)
            {
                _pointV2List.Add(transform.FindEx<RectTransform>(pointPath + i).anchoredPosition);
            }
            _pointOutV2 = transform.FindEx<RectTransform>("Content/Panel/Reward/PointList/PointOut").anchoredPosition;
            //各个module
            string rewardPath = "Content/Panel/Reward/RewardList/UIGemEndlessThreeReward";
            for (int i = 0; i < MaxCellNum; i++)
            {
                var module = new UIGemEndlessThreePkgModule();
                _moduleList.Enqueue(module);
                module.BindModuleRoot(transform.Find(rewardPath + i));
                module.OnModuleCreate();
            }
            _remainTimeText = transform.FindEx<TMP_Text>("Content/Panel/Bg/_cd/text");
            _titleText1 = transform.FindEx<TextProOnACircle>("Content/Panel/Bg/Title1");
            _titleText2 = transform.FindEx<TMP_Text>("Content/Panel/Bg/Title2");
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackGemEndlessThree)items[0];
        }

        protected override void OnPreOpen()
        {
            foreach (var module in _moduleList)
            {
                module.OnModulePreOpen(pack);
            }
            //初始刷新各个module位置以及UI
            _InitRefreshModule();
            _RefreshTheme();
            _RefreshPrice();
            _RefreshCD();
        }

        protected override void OnAddListener()
        {
            WhenInit ??= _RefreshPrice;
            WhenEnd ??= _RefreshEnd;
            WhenTick ??= _RefreshCD;
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<MSG.GAME_GEM_ENDLESS_THREE_PGK_REC_SUCC>().AddListener(_PurchaseComplete);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(_OnRandomBoxFinish);
            MessageCenter.Get<MSG.GAME_GEM_ENDLESS_THREE_PGK_REC_FAIL>().AddListener(Close);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.GAME_GEM_ENDLESS_THREE_PGK_REC_SUCC>().RemoveListener(_PurchaseComplete);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(_OnRandomBoxFinish);
            MessageCenter.Get<MSG.GAME_GEM_ENDLESS_THREE_PGK_REC_FAIL>().RemoveListener(Close);
        }

        protected override void OnPostClose()
        {
            foreach (var module in _moduleList)
            {
                module.OnModulePostClose();
            }
            _ResetCoroutine();
            _ResetTween();
            _isDelayTween = false;
            if (_isBlock)
                _SetBlockerActive(false);
        }
        
        private void _InitRefreshModule()
        {
            if (pack == null)
                return;
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
            {
                pack.FillCurShowPkgIndexList(list, out var isLast);
                int index = 0;
                foreach (var module in _moduleList)
                {
                    if (index < list.Count)
                    {
                        module.SetModuleActive(true);
                        module.SetModuleAnchorPos(_pointV2List[index]);
                        module.UpdateRewardDataAndUI(list[index], isLast);
                    }
                    else
                    {
                        module.SetModuleActive(false);
                        module.UpdateRewardDataAndUI(-1, isLast);
                    }
                    index++;
                }
            }
        }
        
        private void _RefreshTheme()
        {
            if (pack == null) return;
            var visual = pack.Visual;
            visual.Refresh(_titleText1, "mainTitle");
            visual.Refresh(_titleText2, "subTitle");
        }

        private void _RefreshPrice() {
            foreach (var module in _moduleList)
            {
                module.OnMessageInitIAP();
            }
        }

        private void _RefreshCD() {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, pack.endTS - t);
            UIUtility.CountDownFormat(_remainTimeText, diff);
        }

        private void _RefreshEnd(ActivityLike pack_, bool expire_) {
            if (pack_ != pack) return;
            Close();
        }
        
        private void _PurchaseComplete(int targetIndex, IList<RewardCommitData> list_) {
            //奖励中有体力时才reset popup
            var hasEnergy = false;
            foreach (var reward in list_)
            {
                if (reward.rewardId != Constant.kMergeEnergyObjId) continue;
                hasEnergy = true;
                break;
            }
            if (hasEnergy)
                pack.ResetPopup();
            foreach (var module in _moduleList)
            {
                module.OnPurchaseComplete(targetIndex, list_);
            }
            if (!Game.Manager.specialRewardMan.CheckCanClaimSpecialReward())
            {
                _OnRecSucc();
            }
            else
            {
                _isDelayTween = true;
            }
        }

        private void _OnRandomBoxFinish()
        {
            if (_isDelayTween)
            {
                _isDelayTween = false;
                _OnRecSucc();
            }
        }
        
        private void _OnRecSucc()
        {
            if (pack == null)
                return;
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
            {
                pack.FillCurShowPkgIndexList(list, out var isLast);
                _ResetCoroutine();
                //根据是否是最后的格子选择不同表现方式
                if (!isLast)
                {
                    _tweenCoroutine = StartCoroutine(_CoPlayTweenWithCycle(list.Last()));
                }
                else
                {
                    _tweenCoroutine = StartCoroutine(_CoPlayTweenAtLast(list));
                }
            }
        }

        private IEnumerator _CoPlayTweenWithCycle(int lastIndex)
        {
            //开始播动画时打开点击屏蔽
            _SetBlockerActive(true);
            _ResetTween();
            //使用tween队列
            _tweenSeq = DOTween.Sequence();
            //第一个module 出队列 播动画alpha变为0 播完后刷新并重新加入队列末尾
            var oldFirstModule = _moduleList.Dequeue();
            _tweenSeq.Append(oldFirstModule.StartTweenMoveAndAlpha(_pointOutV2.x, _pointOutV2.y, 0, DurationTime, () =>
            {
                oldFirstModule.SetModuleAnchorPos(_pointV2List[MaxCellNum - 1]);
                oldFirstModule.SetModuleAlpha(1);
                _moduleList.Enqueue(oldFirstModule);
                oldFirstModule.UpdateRewardDataAndUI(lastIndex, false);
            }));
            //后续3个module依次向前一个index对应的位置挪动
            int index = 1;
            foreach (var module in _moduleList)
            {
                var pos = _pointV2List[index - 1];
                _tweenSeq.Join(module.StartTweenMove(pos.x, pos.y, DurationTime));
                index++;
            }
            _tweenSeq.Play();
            //先等一段时间 用于显示最新module的背光特效
            yield return new WaitForSeconds(DurationTime);
            //重新赋值firstModule 取到现在最新的module 
            var newFirstModule = _moduleList.Peek();
            //播最新module的解锁特效
            newFirstModule.PlayUnlockEffect();
            //在等待一段时间 突出下最后的解锁特效
            yield return new WaitForSeconds(WaitUnlockTime);
            //动画结束时关闭点击屏蔽
            _SetBlockerActive(false);
        }

        private IEnumerator _CoPlayTweenAtLast(List<int> list)
        {
            //开始播动画时打开点击屏蔽
            _SetBlockerActive(true);
            _ResetTween();
            //直接刷新module
            int index = 0;
            foreach (var module in _moduleList)
            {
                if (index < list.Count)
                {
                    //isRefresh传true 会播解锁特效
                    module.UpdateRewardDataAndUI(list[index], true, true);
                }
                else
                {
                    module.UpdateRewardDataAndUI(-1, true);
                }
                index++;
            }
            //在等待一段时间 突出下最后的解锁特效
            yield return new WaitForSeconds(WaitUnlockTime);
            //动画结束时关闭点击屏蔽
            _SetBlockerActive(false);
        }

        private void _SetBlockerActive(bool b)
        {
            _isBlock = b;
            UIManager.Instance.Block(b);
        }

        private void _ResetCoroutine()
        {
            if (_tweenCoroutine != null)
                StopCoroutine(_tweenCoroutine);
            _tweenCoroutine = null;
        }
        
        private void _ResetTween()
        {
            _tweenSeq?.Kill();
            _tweenSeq = null;
        }
    }
}