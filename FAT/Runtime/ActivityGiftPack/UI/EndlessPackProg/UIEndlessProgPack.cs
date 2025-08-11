/*
 * @Author: tang.yan
 * @Description: 六格无限礼包进度条版 
 * @Date: 2025-02-19 16:02:13
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
    public class UIEndlessProgPack : UIBase
    {
        [SerializeField] private TextProOnACircle mainTitleCircle;  //主标题文本 弯曲版
        [SerializeField] private TMP_Text mainTitleNormal;  //主标题文本 水平版
        [SerializeField] private TMP_Text remainTimeText;   //剩余时间文本
        [SerializeField] private RectTransform infoRoot;    //info根节点
        [SerializeField] private RectTransform progRoot;    //进度条根节点
        [SerializeField] private Button closeBtn;           //关闭按钮
        [SerializeField] private int freeFontStyle = 9;     //免费奖励数量文本样式
        [SerializeField] private int payFontStyle = 17;     //付费奖励数量文本样式

        private Queue<UIEndlessProgModule> _moduleList = new Queue<UIEndlessProgModule>();
        private List<Vector2> _pointV2List = new List<Vector2>();   //记录格子移动点位
        private Vector2 _pointOutV2;        //格子移出后的复原位置
        private const int MaxCellNum = 6;   //界面显示的最大格子数
        private Coroutine _tweenCoroutine;
        private Sequence _tweenSeq;
        private const float IntervalTime = 0.1f;    //各段tween动画之间的间隔时间
        private const float DurationTime = 0.1f;    //各段tween动画的持续时间
        //进度条版本相关节点
        private UICommonProgressBar _progressBar;
        private RectTransform _tokenRect;
        private UIImageRes _tokenIcon;
        private Button _tokenBtn;
        private Animation _tokenAnim;
        private UICommonItem _reward;
        private Animator _rewardAnim;
        [SerializeField] private GameObject _effectGo;
        
        private Action WhenInit;
        private Action<ActivityLike, bool> WhenEnd;
        private Action WhenTick;
        private PackEndless pack;

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
            string rewardPath = "Reward/RewardList/UIEndlessProgReward";
            for (int i = 0; i < MaxCellNum; i++)
            {
                var module = new UIEndlessProgModule();
                _moduleList.Enqueue(module);
                module.BindModuleRoot(infoRoot.Find(rewardPath + i));
                module.OnModuleCreate(freeFontStyle, payFontStyle);
            }
            _progressBar = progRoot.FindEx<UICommonProgressBar>("ProgressBar");
            _tokenRect = progRoot.FindEx<RectTransform>("Token/icon");
            _tokenIcon = progRoot.FindEx<UIImageRes>("Token/icon");
            _tokenBtn = progRoot.FindEx<Button>("Token/icon");
            _tokenBtn.onClick.AddListener(_OnClickTokenBtn);
            _tokenAnim = progRoot.FindEx<Animation>("Token");
            _reward = progRoot.FindEx<UICommonItem>("Reward");
            _reward.Setup();
            _rewardAnim = progRoot.FindEx<Animator>("Reward/icon");
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackEndless)items[0];
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
            _RefreshProgress();
            _RefreshToken();
            _RefreshProgressReward();
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
            MessageCenter.Get<MSG.GAME_ENDLESS_PROG_CHANGE>().AddListener(_OnProgressChange);
            MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_FAIL>().AddListener(Close);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(WhenInit);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_SUCC>().RemoveListener(_PurchaseComplete);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(_OnRandomBoxFinish);
            MessageCenter.Get<MSG.GAME_ENDLESS_PROG_CHANGE>().RemoveListener(_OnProgressChange);
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
            _ResetFlyCoroutine();
            _ClearDelay();
            _ClearProgressCache();
            _isWillEnd = false;
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
            UIUtility.CountDownFormat(remainTimeText, diff);
        }
        
        private void _RefreshProgress(int targetNum = -1)
        {
            var progressConf = pack?.GetCurProgressConf();
            if (progressConf == null) return;
            var curPhase = pack.GetCurProgressPhase();
            var curNum = targetNum == -1 ? pack.GetCurProgressNum() : targetNum;
            if (curPhase < progressConf.ProgressNode.Count && curPhase < progressConf.ProgressReward.Count)
            {
                _progressBar.ForceSetup(0, progressConf.ProgressNode[curPhase], curNum);
            }
            else
            {
                var finalNum = progressConf.ProgressNode[curPhase - 1];
                _progressBar.ForceSetup(0, finalNum, finalNum);
                _progressBar.SetInd(I18N.Text("#SysComDesc836"));
            }
        }

        private void _RefreshToken()
        {
            var tokenConf = Game.Manager.objectMan.GetBasicConfig(pack?.GetCurTokenId() ?? 0);
            _tokenIcon.SetImage(tokenConf?.Icon.ConvertToAssetConfig());
            _tokenAnim.Stop();
            _effectGo.SetActive(false);
        }

        private void _RefreshProgressReward()
        {
            var progressConf = pack?.GetCurProgressConf();
            if (progressConf == null) return;
            var curPhase = pack.GetCurProgressPhase();
            if (curPhase < progressConf.ProgressNode.Count && curPhase < progressConf.ProgressReward.Count)
            {
                _reward.Refresh(progressConf.ProgressReward[curPhase].ConvertToRewardConfig());
            }
            else
            {
                _reward.Refresh(null);
            }
        }

        private void _RefreshEnd(ActivityLike pack_, bool expire_) {
            if (pack_ != pack) return;
            if (_cacheProgressReward != null || _cacheFinalProgressNum > 0 || _cacheProgressMaxNum > 0)
            {
                _isWillEnd = true;
                return;
            }
            Close();
        }
        
        private void _PurchaseComplete(int targetIndex, IList<RewardCommitData> list_, RewardCommitData tokenReward) {
            pack.ResetPopup();
            foreach (var module in _moduleList)
            {
                module.OnPurchaseComplete(targetIndex, list_);
            }
            if (!Game.Manager.specialRewardMan.CheckCanClaimSpecialReward())
            {
                _TryFlyTokenReward(targetIndex, tokenReward);
                _OnRecSucc();
            }
            else
            {
                _isDelayTween = true;
                _delayAction = () =>
                {
                    _TryFlyTokenReward(targetIndex, tokenReward);
                };
            }
        }

        private void _OnRandomBoxFinish()
        {
            if (_isDelayTween)
            {
                _delayAction?.Invoke();
                _OnRecSucc();
                _ClearDelay();
            }
        }
        
        private void _ClearDelay()
        {
            _isDelayTween = false;
            _delayAction = null;
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
            if (_cacheProgressReward != null || _cacheFinalProgressNum > 0 || _cacheProgressMaxNum > 0)
            {
                //有进度条相关表现时再等一下
                yield return new WaitForSeconds(DurationTime * 5);
            }
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
        
        private void _OnClickTokenBtn()
        {
            var tokenId = pack?.GetCurTokenId() ?? 0;
            if (tokenId <= 0) return;
            UIManager.Instance.OpenWindow(UIConfig.UIEndlessTokenTips, _tokenRect.position, 4f + _tokenRect.rect.size.y * 0.5f, tokenId);
        }
        
        #region token飞奖励+进度条表现相关
        
        private Action _delayAction = null; //延迟飞图标表现 在随机宝箱开启时会延迟飞图标表现到随机宝箱领完后执行
        
        private void _TryFlyTokenReward(int targetIndex, RewardCommitData tokenReward)
        {
            if (tokenReward == null)
                return;
            _ResetFlyCoroutine();
            _flyCoroutine = StartCoroutine(_CoPlayTokenAnim());
            foreach (var module in _moduleList)
            {
                module.TryFlyTokenReward(targetIndex, tokenReward, _OnTokenFlyFinish);
            }
        }

        private int _cacheFinalProgressNum = 0; //当前缓存的最终进度值 用于界面表现
        private RewardCommitData _cacheProgressReward = null;   //当前缓存的进度条奖励 在合适时commit
        private int _cacheProgressMaxNum = 0;   //当前缓存的进度条最大值 和当前数据层面的最大值不一样
        private bool _isWillEnd = false;    //进度条动画过程中是否触发了自动关闭界面
        private Coroutine _flyCoroutine;
        [SerializeField] private float delayPlayAnimTime = 0f;

        private void _OnProgressChange(int finalNum, RewardCommitData progressReward, int curProgressMax)
        {
            //如果连续收到了进度条奖励变化事件，说明发生了补单行为 此时需要保证奖励可以正常提交 
            if (_cacheProgressReward != null)
            {
                _OnProgressAnimPlayEnd();
            }
            _cacheFinalProgressNum = finalNum;
            _cacheProgressReward = progressReward;
            _cacheProgressMaxNum = curProgressMax;
        }

        private void _ClearProgressCache()
        {
            _cacheFinalProgressNum = 0;
            _cacheProgressReward = null;
            _cacheProgressMaxNum = 0;
        }
        
        //当token飞图标动画播完时
        private void _OnTokenFlyFinish()
        {
            if (pack == null)
                return;
            //没有进度条奖励 只是单纯的进度条增长表现 不需要进度条动画播完回调
            if (_cacheProgressReward == null)
            {
                //如果进度条已经完成了 就不再播动画了
                if (!pack.CheckProgressFinish())
                {
                    _progressBar.SetProgress(_cacheFinalProgressNum);
                }
                _ClearProgressCache();
            }
            //有进度条奖励，先处理涨满进度条并发奖的表现，之后处理进度条再增长一段的表现
            else
            {
                _progressBar.RegisterNotify(_OnProgressAnimPlayEnd);
                _progressBar.SetProgress(_cacheProgressMaxNum);
            }
        }
        
        //当进度条动画播完时
        private void _OnProgressAnimPlayEnd()
        {
            _progressBar.UnregisterNotify(_OnProgressAnimPlayEnd);
            //发奖
            if (_cacheProgressReward != null)
                UIFlyUtility.FlyReward(_cacheProgressReward, _reward.transform.position);
            _rewardAnim.ResetTrigger("Punch");
            _rewardAnim.SetTrigger("Punch");
            //进度条重置 刷新新的奖励
            _RefreshProgress(0);
            _RefreshProgressReward();
            //如果进度条已经完成了 就不再播动画了
            if (!pack.CheckProgressFinish())
            {
                //将剩下的进度值跑完
                _progressBar.SetProgress(_cacheFinalProgressNum);
            }
            _ClearProgressCache();
            //如果播进度条动画过程中触发关闭界面，则此时关
            if (_isWillEnd)
                Close();
        }
        
        private IEnumerator _CoPlayTokenAnim()
        {
            yield return new WaitForSeconds(delayPlayAnimTime);
            _tokenAnim.Stop();
            _tokenAnim.Play();
        }
        
        private void _ResetFlyCoroutine()
        {
            if (_flyCoroutine != null)
                StopCoroutine(_flyCoroutine);
            _flyCoroutine = null;
        }

        #endregion
    }
}