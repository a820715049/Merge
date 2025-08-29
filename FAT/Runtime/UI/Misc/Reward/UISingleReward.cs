/*
 * @Author: ange.shentu
 * @Description: 单一Token奖励界面
 * @Date: 2025-08-11 16:11:15
 */
using System;
using System.Collections;
using UnityEngine;
using EL;
using Config;
using Spine;
using Spine.Unity;

namespace FAT
{
    public class UISingleReward : UIBase
    {
        [SerializeField] private GameObject boxGo;
        [SerializeField] private Animator boxAnim;
        [SerializeField] private UIImageRes boxImage;

        private RandomBoxMan.RandomBoxData _curRandomBoxData;
        private Coroutine _coroutine = null;
        private GameObject _spinePrefab = null;
        private SkeletonGraphic _skeleton = null;
        private bool _isCurShowSpine = false;
        private bool _isPlaySpineAnim = false;
        private Coroutine _coShowEffect;
        private bool _useSpine = false;   // 当前是否使用Spine流程

        protected override void OnCreate()
        {
            transform.AddButton("Mask", _OnBtnClaim);
        }

        protected override void OnParse(params object[] items) { }

        protected override void OnPreOpen()
        {
            _curRandomBoxData = Game.Manager.randomBoxMan.TryGetCanClaimBoxData();
            _PreparePresentation();
        }

        protected override void OnPostOpen()
        {
            if (_useSpine)
            {
                _ShowSpine();
            }
            else
            {
                _ShowImage();
            }
        }

        protected override void OnPreClose()
        {
            // 这个UI和随机宝箱不同，不会有飞行奖励。所以要有个触发后续流程的通知
            var tokenId = 0;
            if (_curRandomBoxData != null && _curRandomBoxData.Reward != null && _curRandomBoxData.Reward.Count > 0)
            {
                tokenId = _curRandomBoxData.Reward[0].rewardId;
            }
            MessageCenter.Get<MSG.UI_SINGLE_REWARD_CLOSE_FEEDBACK>().Dispatch(tokenId);
        }

        protected override void OnPostClose()
        {
            _ClearBoxSpine();
            Game.Manager.specialRewardMan.TryDisplaySpecialReward();
        }

        // 统一入口：根据配置决定使用Spine或图片
        private void _PreparePresentation()
        {
            boxGo.SetActive(true);
            boxAnim.SetTrigger("Hide");
            _useSpine = false;
            if (_curRandomBoxData == null) return;

            var chestConf = Game.Manager.objectMan.GetRandomBoxConfig(_curRandomBoxData.RandomBoxId);
            var spineRes = chestConf?.Spine.ConvertToAssetConfig();

            if (!string.IsNullOrEmpty(spineRes.Asset))
            {
                _InitSpineFlow(spineRes);
                _useSpine = true;
            }
            else
            {
                _InitImageFlow();
            }
        }

        // Spine 流程初始化：异步加载Spine，隐藏图片
        private void _InitSpineFlow(AssetConfig spinePrefab)
        {
            boxImage.gameObject.SetActive(false);
            _coroutine = StartCoroutine(_CoLoadRes(spinePrefab, boxGo.transform));
        }

        // 图片流程初始化：同步设置图片，隐藏Spine
        private void _InitImageFlow()
        {
            // 清理遗留Spine（防御）
            if (_spinePrefab != null)
            {
                Destroy(_spinePrefab);
                _spinePrefab = null;
                _skeleton = null;
            }

            var basic = Game.Manager.objectMan.GetBasicConfig(_curRandomBoxData.RandomBoxId);
            var iconKey = basic != null ? basic.Icon : null;
            boxImage.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(iconKey)) boxImage.SetImage(iconKey);
            else boxImage.Clear();
        }

        // Spine 显示阶段
        private void _ShowSpine()
        {
            if (_spinePrefab == null || IsOpening() || _isCurShowSpine)
                return;
            _isCurShowSpine = true;
            _spinePrefab.SetActive(true);
            if (_skeleton != null)
            {
                _isPlaySpineAnim = true;
                _skeleton.AnimationState.SetAnimation(0, "box_show", false)
                    .Complete += delegate (TrackEntry entry) { _isPlaySpineAnim = false; };
                _skeleton.AnimationState.AddAnimation(0, "box_idle", true, 0f);
                boxAnim.SetTrigger("Show");
                // 宝箱出现
                Game.Manager.audioMan.TriggerSound("ChestAppear");
            }
        }

        // 图片显示阶段（与Spine对齐演出要点）
        private void _ShowImage()
        {
            if (IsOpening()) return;
            boxAnim.SetTrigger("Show");
            // 宝箱出现音效保持一致
            Game.Manager.audioMan.TriggerSound("ChestAppear");
        }

        private void _ClearBoxSpine()
        {
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

        private void _OnBtnClaim()
        {
            if (_curRandomBoxData == null)
            {
                Close();
                return;
            }
            if (_isPlaySpineAnim)
                return;
            _isPlaySpineAnim = true;

            if (_skeleton != null)
            {
                _skeleton.AnimationState.ClearTracks();
                _skeleton.AnimationState.SetAnimation(0, "box_open", false)
                    .Complete += delegate (TrackEntry entry)
                {
                    boxGo.SetActive(false);
                    Game.Manager.randomBoxMan.TryFinishRandomBoxData(_curRandomBoxData);
                    Close();
                };
                // 宝箱打开
                Game.Manager.audioMan.TriggerSound("ChestOpen");
            }
            else
            {
                Game.Manager.audioMan.TriggerSound("ChestOpen");
                Game.Manager.randomBoxMan.TryFinishRandomBoxData(_curRandomBoxData);
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
                _ShowSpine();
            }
            else
            {
                DebugEx.Error($"UISingleReward Spine missing {res.Group}@{res.Asset}");
            }
        }

    }
}
