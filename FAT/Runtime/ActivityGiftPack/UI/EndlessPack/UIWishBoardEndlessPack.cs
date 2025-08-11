/*
 * @Author: yanfuxing
 * @Date: 2025-06-16 15:59:00
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIWishBoardEndlessPack : UIBase
    {
        [SerializeField] private TextProOnACircle mainTitleCircle;  //主标题文本 弯曲版
        [SerializeField] private TMP_Text mainTitleNormal;  //主标题文本 水平版
        [SerializeField] private TMP_Text subTitle;         //副标题文本
        [SerializeField] private TMP_Text remainTimeText;   //剩余时间文本
        [SerializeField] private RectTransform infoRoot;    //info根节点
        [SerializeField] private Button closeBtn;           //关闭按钮
        [SerializeField] private int freeFontStyle = 9;     //免费奖励数量文本样式
        [SerializeField] private int payFontStyle = 17;     //付费奖励数量文本样式

        private Queue<UIWishBoardEndlessPackModule> _moduleList = new Queue<UIWishBoardEndlessPackModule>();
        private List<Vector2> _pointV2List = new List<Vector2>();   //记录格子移动点位
        private Vector2 _pointOutV2;        //格子移出后的复原位置
        private const int MaxCellNum = 6;   //界面显示的最大格子数
        private Coroutine _tweenCoroutine;
        private Sequence _tweenSeq;
        private const float IntervalTime = 0.1f;    //各段tween动画之间的间隔时间
        private const float DurationTime = 0.1f;    //各段tween动画的持续时间

        private Action WhenInit;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private PackEndlessWishBoard pack;

        private bool _isDelayTween = false; //是否延迟tween动画表现 在随机宝箱开启时会延迟界面表现到随机宝箱领完后执行
        private bool _isBlock = false;      //记录是否在block

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            closeBtn.WithClickScale().FixPivot().onClick.AddListener(base.Close);
            //固定点位
            string pointPath = "Reward/PointList/Point";
            for (int i = 0; i < MaxCellNum; i++)
            {
                _pointV2List.Add(infoRoot.FindEx<RectTransform>(pointPath + i).anchoredPosition);
            }
            _pointOutV2 = infoRoot.FindEx<RectTransform>("Reward/PointList/PointOut").anchoredPosition;
            //各个module
            string rewardPath = "Reward/RewardList/UIEndlessPackReward";
            for (int i = 0; i < MaxCellNum; i++)
            {
                var module = new UIWishBoardEndlessPackModule();
                _moduleList.Enqueue(module);
                module.BindModuleRoot(infoRoot.Find(rewardPath + i));
                module.OnModuleCreate(freeFontStyle, payFontStyle);
            }
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackEndlessWishBoard)items[0];
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
            MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_SUCC>().AddListener(_PurchaseComplete);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(_OnRandomBoxFinish);
            MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_FAIL>().AddListener(Close);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_SUCC>().RemoveListener(_PurchaseComplete);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(_OnRandomBoxFinish);
            MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_FAIL>().RemoveListener(Close);
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
                pack.FillCurShowPkgIndexList(list);
                int index = 0;
                foreach (var module in _moduleList)
                {
                    if (index < list.Count)
                    {
                        module.SetModuleActive(true);
                        module.SetModuleAnchorPos(_pointV2List[index]);
                        module.SetModuleScale(Vector3.one);
                        module.UpdateRewardDataAndUI(list[index]);
                    }
                    else
                    {
                        module.SetModuleActive(false);
                    }
                    index++;
                }
            }
        }

        private void _RefreshTheme()
        {
            if (pack == null) return;
            var visual = pack.Visual;
            visual.Refresh(mainTitleCircle, "mainTitle");
            visual.Refresh(mainTitleNormal, "mainTitle");
            visual.Refresh(subTitle, "subTitle");
        }

        private void _RefreshPrice()
        {
            foreach (var module in _moduleList)
            {
                module.OnMessageInitIAP();
            }
        }

        private void _RefreshCD()
        {
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, pack.endTS - t);
            UIUtility.CountDownFormat(remainTimeText, diff);
        }

        private void _RefreshEnd(ActivityLike pack_, bool expire_)
        {
            if (pack_ != pack) return;
            Close();
        }

        private void _PurchaseComplete(int targetIndex, IList<RewardCommitData> list_, RewardCommitData tokenReward)
        {
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
                pack.FillCurShowPkgIndexList(list);
                _ResetCoroutine();
                _tweenCoroutine = StartCoroutine(_CoPlayTween(list.Last()));
            }
        }

        private IEnumerator _CoPlayTween(int lastIndex)
        {
            //开始播动画时打开点击屏蔽
            _SetBlockerActive(true);
            _ResetTween();
            //使用tween队列
            _tweenSeq = DOTween.Sequence();
            //第一个module 出队列 播动画缩小至scale为0 播完后刷新并重新加入队列末尾
            var oldFirstModule = _moduleList.Dequeue();
            _tweenSeq.Append(oldFirstModule.StartTweenScale(0, DurationTime, () =>
            {
                oldFirstModule.SetModuleAnchorPos(_pointOutV2);
                oldFirstModule.UpdateRewardDataAndUI(lastIndex);
                _moduleList.Enqueue(oldFirstModule);
            }));
            //后续5个module依次向前一个index对应的位置挪动
            int index = 1;
            foreach (var module in _moduleList)
            {
                var pos = _pointV2List[index - 1];
                //每隔IntervalTime秒插入一段长度为DurationTime的动画
                _tweenSeq.Insert(index * IntervalTime, module.StartTweenMove(pos.x, pos.y, DurationTime));
                index++;
            }
            _tweenSeq.Play();
            //先等一段时间 用于显示最新module的背光特效
            yield return new WaitForSeconds(IntervalTime);
            //重新赋值firstModule 取到现在最新的module 
            var newFirstModule = _moduleList.Peek();
            //播最新module的背光特效
            newFirstModule.ShowCurEffect();
            //等待上面的动画播完
            float totalTime = (index - 2) * IntervalTime;
            yield return new WaitForSeconds(totalTime);
            //到这一步时 firstModule已经加入到队列末尾了
            var lastPos = _pointV2List.Last();
            oldFirstModule.StartTweenMoveAndScale(lastPos.x, lastPos.y, 1, DurationTime);
            //播最新module的解锁特效
            newFirstModule.PlayUnlockEffect();
            //在等待一段时间 突出下最后的解锁特效
            yield return new WaitForSeconds(DurationTime * 5);
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

