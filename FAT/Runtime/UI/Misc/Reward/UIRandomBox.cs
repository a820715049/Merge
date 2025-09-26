/*
 * @Author: tang.yan
 * @Description: 随机宝箱界面
 * @Date: 2023-11-30 16:11:15
 */
using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Coffee.UIExtensions;
using TMPro;
using EL;
using Config;
using Spine;
using Spine.Unity;
using fat.rawdata;

namespace FAT
{
    public class UIRandomBox : UIBase
    {
        [Serializable]
        private class UIRandomBoxReward
        {
            [SerializeField] public UICommonItem reward;
            [SerializeField] public UIParticle particle;
        }

        [SerializeField]
        [Tooltip("宝箱开启特效延迟出现时间")]
        private float effectShowDelayTime;
        [SerializeField] private GameObject boxGo;
        [SerializeField] private Animator boxAnim;
        [SerializeField] private GameObject rewardGo;
        [SerializeField] private GameObject rewardEffectGo;
        [SerializeField] private List<UIRandomBoxReward> rewardGroup;
        [SerializeField] private TMP_Text tipsText;

        private RandomBoxMan.RandomBoxData _curRandomBoxData;
        private int _curShowStage = 0;
        private Coroutine _coroutine = null;
        private GameObject _spinePrefab = null;
        private SkeletonGraphic _skeleton = null;
        private bool _isCurShowSpine = false;
        private bool _isPlaySpineAnim = false;
        private Coroutine _coShowEffect;
        //策划配置——是否允许跳过随机宝箱“开启动画”
        private bool _canSkipOpenAnim;
        //奖励展示至少停留的时间（秒）
        private float _minRewardShowSeconds = 0.8f;
        //到这个时间点后才允许在 stage=1 进行领取/关闭
        private float _stage1UnlockTime = 0f;
        
        protected override void OnCreate()
        {
            foreach (var r in rewardGroup)
            {
                r.reward.Setup();
            }
            transform.AddButton("Content/ClaimBtn", _OnBtnClaim);
        }

        protected override void OnParse(params object[] items) { }

        protected override void OnPreOpen()
        {
            //根据配置决定是否允许跳过开启动画
            _canSkipOpenAnim = Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureSkipRandomChest);
            _stage1UnlockTime = 0f;
            _curRandomBoxData = Game.Manager.randomBoxMan.TryGetCanClaimBoxData();
            _curShowStage = 0;
            _CreateBoxSpine();
            _RefreshReward();
            _RefreshTips();
        }

        protected override void OnPostOpen()
        {
            _TryShowBoxSpine();
        }

        protected override void OnPreClose()
        {
            tipsText.gameObject.SetActive(false);
        }

        protected override void OnPostClose()
        {
            _ClearBoxSpine();
            Game.Manager.specialRewardMan.OnSpecialRewardUIClosed(ObjConfigType.RandomBox, _curRandomBoxData?.RandomBoxId ?? 0);
        }

        private void _CreateBoxSpine()
        {
            boxGo.SetActive(true);
            boxAnim.SetTrigger("Hide");
            if (_curRandomBoxData == null)
                return;
            var spinePrefab = Game.Manager.objectMan.GetRandomBoxConfig(_curRandomBoxData.RandomBoxId)?.Spine.ConvertToAssetConfig();
            if (spinePrefab != null)
            {
                _coroutine = StartCoroutine(_CoLoadRes(spinePrefab, boxGo.transform));
            }
        }

        //在PostOpen
        private void _TryShowBoxSpine()
        {
            if (_spinePrefab == null || IsOpening() || _isCurShowSpine)
                return;
            _isCurShowSpine = true;
            _spinePrefab.SetActive(true);
            if (_skeleton != null)
            {
                if (_canSkipOpenAnim)
                {
                    //允许跳过时 随机宝箱的展示动画可以跳过
                    _skeleton.AnimationState.SetAnimation(0, "box_show", false);
                }
                else
                {
                    //不允许跳过时 等待动画播完
                    _isPlaySpineAnim = true;
                    _skeleton.AnimationState.SetAnimation(0, "box_show", false)
                        .Complete += delegate (TrackEntry entry) { _isPlaySpineAnim = false; };
                }
                _skeleton.AnimationState.AddAnimation(0, "box_idle", true, 0f);
                boxAnim.SetTrigger("Show");
                // 宝箱出现
                Game.Manager.audioMan.TriggerSound("ChestAppear");
            }
        }

        private void _ClearBoxSpine()
        {
            rewardEffectGo.SetActive(false);
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            if (_coShowEffect != null)
            {
                StopCoroutine(_coShowEffect);
                _coShowEffect = null;
            }
            if (_spinePrefab != null)
            {
                Destroy(_spinePrefab);
                _spinePrefab = null;
            }
            if (_skeleton != null)
            {
                _skeleton.AnimationState.ClearTracks();
                _skeleton = null;
            }
            _isCurShowSpine = false;
            _isPlaySpineAnim = false;
        }

        private void _RefreshReward()
        {
            rewardGo.SetActive(false);
            if (_curRandomBoxData == null)
                return;
            var rewardList = _curRandomBoxData.Reward;
            //判断第一个奖励是否是万能卡，是的话则不显示
            var firstRewardId = rewardList.Count > 0 ? rewardList[0].rewardId : 0;
            int index = Game.Manager.objectMan.IsType(firstRewardId, ObjConfigType.CardJoker) ? 1 : 0;
            foreach (var uiReward in rewardGroup)
            {
                uiReward.particle.gameObject.SetActive(false);
                if (index < rewardList.Count)
                {
                    uiReward.reward.gameObject.SetActive(true);
                    uiReward.reward.Refresh(rewardList[index].rewardId, rewardList[index].rewardCount);
                }
                else
                {
                    uiReward.reward.gameObject.SetActive(false);
                }
                index++;
            }
        }

        private void _RefreshTips()
        {
            tipsText.gameObject.SetActive(true);
            string tips = "";
            if (_curShowStage == 0)
                tips = I18N.Text("#SysComDesc110");
            else if (_curShowStage == 1)
                tips = I18N.Text("#SysComDesc111");
            tipsText.text = tips;
        }

        private void _OnBtnClaim()
        {
            if (_curRandomBoxData == null)
            {
                Close();
                return;
            }
            
            if (_curShowStage == 0)
            {
                //宝箱没配置spine动画时 直接跳过阶段0 并报错
                if (_skeleton == null)
                {
                    DebugEx.FormatError("UIRandomBox._OnBtnClaim : random box spine is null, RandomBoxId = {0}", _curRandomBoxData.RandomBoxId);
                    _SkipOpenAnim();
                    return;
                }
                // 如果正在“开启动画”且允许跳过，则执行跳过；否则保持原有拦截逻辑
                if (_isPlaySpineAnim)
                {
                    if (_coShowEffect != null && _canSkipOpenAnim)
                    {
                        _SkipOpenAnim();
                    }
                    return;
                }
                _isPlaySpineAnim = true;
                _coShowEffect = StartCoroutine(_CoShowOpenEffect());
                _skeleton.AnimationState.ClearTracks();
                _skeleton.AnimationState.SetAnimation(0, "box_open", false)
                    .Complete += delegate(TrackEntry entry)
                {
                    boxGo.SetActive(false);
                    _curShowStage = 1;
                    //只有允许跳过时才记录最早可点击时间
                    _stage1UnlockTime = _canSkipOpenAnim ? (Time.unscaledTime + _minRewardShowSeconds) : 0f;
                    _RefreshTips();
                };
                // 宝箱打开
                Game.Manager.audioMan.TriggerSound("ChestOpen");
            }
            else if (_curShowStage == 1)
            {
                if (_canSkipOpenAnim)
                {
                    //允许跳过时 奖励至少展示 _minRewardShowSeconds 秒后才允许领取/关闭
                    if (Time.unscaledTime < _stage1UnlockTime)
                        return;
                }
                else if (_isPlaySpineAnim)
                {
                    //不允许跳过时 等待动画播完
                    return;
                }
                //先领取宝箱中的奖励
                int index = 0;
                foreach (var reward in _curRandomBoxData.Reward)
                {
                    if (index < rewardGroup.Count)
                    {
                        UIFlyUtility.FlyReward(reward, rewardGroup[index].reward.transform.position);
                    }
                    else
                    {
                        UIFlyUtility.FlyReward(reward, rewardGo.transform.position);
                    }
                    index++;
                }
                //在领取宝箱本身
                Game.Manager.randomBoxMan.TryFinishRandomBoxData(_curRandomBoxData);
                //再关界面
                Close();
            }
        }

        private IEnumerator _CoLoadRes(AssetConfig res, Transform anchor)
        {
            var req = EL.Resource.ResManager.LoadAsset(res.Group, res.Asset);
            yield return req;
            if (req.isSuccess && req.asset != null)
            {
                var obj = Instantiate(req.asset) as GameObject;
                obj.transform.SetParent(anchor);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localScale = Vector3.one;
                if (_spinePrefab != null)
                {
                    DestroyImmediate(_spinePrefab);
                    _spinePrefab = null;
                }
                _spinePrefab = obj;
                _skeleton = _spinePrefab.GetComponent<SkeletonGraphic>();
                _spinePrefab.SetActive(false);
                _TryShowBoxSpine();
            }
            else
            {
                DebugEx.Error($"UIRandomBox Spine missing {res.Group}@{res.Asset}");
            }
        }

        private IEnumerator _CoShowOpenEffect()
        {
            yield return new WaitForSeconds(effectShowDelayTime);
            rewardEffectGo.SetActive(true);
            rewardGo.SetActive(true);
            //等一帧再显示特效
            yield return new WaitForSeconds(0.3f);
            foreach (var uiReward in rewardGroup)
            {
                uiReward.particle.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
            //等宝箱打开特效播完才允许进行下一步
            yield return new WaitForSeconds(0.5f);
            _isPlaySpineAnim = false;
        }
        
        private void _SkipOpenAnim()
        {
            if (_curShowStage != 0) 
                return;
            // 1) 停掉开启动画链路（仅当处于开启动画阶段有意义）
            if (_coShowEffect != null)
            {
                StopCoroutine(_coShowEffect);
                _coShowEffect = null;
            }

            // 2) 立即展示开箱后的奖励与特效（跳过等待与渐进）
            rewardEffectGo.SetActive(true);
            rewardGo.SetActive(true);
            foreach (var uiReward in rewardGroup)
            {
                uiReward.particle.gameObject.SetActive(true);
            }

            // 3) 终止Spine当前播放并隐藏宝箱外观（直接进入开箱后的状态）
            if (_skeleton != null)
            {
                _skeleton.AnimationState.ClearTracks();
            }
            boxGo.SetActive(false);

            // 4) 切换到可领取阶段
            _isPlaySpineAnim = false;
            _curShowStage = 1;
            //只有允许跳过时才记录最早可点击时间
            _stage1UnlockTime = _canSkipOpenAnim ? (Time.unscaledTime + _minRewardShowSeconds) : 0f;
            _RefreshTips();
        }
    }
}
