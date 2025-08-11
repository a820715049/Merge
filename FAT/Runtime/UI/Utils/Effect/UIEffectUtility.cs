/*
 * @Author: qun.chao
 * @Date: 2022-03-01 11:53:11
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Config;

namespace FAT
{
    public class UIEffectUtility : MonoSingleton<UIEffectUtility>
    {
        // public enum ParticleEffectType
        // {
        //     Exp,
        //     EventExp,
        //     Energy,
        //     Coin,
        //     Gem,
        //     EnergyBuff,
        //     Max
        // }

        // private PoolItemType mItemType = PoolItemType.COMMON_EFFECT_FLY_ICON;

        // public void Initialize()
        // {
        //     _Prepare();
        // }

        // private void _Prepare()
        // {
        //     var _go = new GameObject("_fly_icon_item_", typeof(RectTransform));
        //     _go.transform.SetParent(transform, false);
        //     _go.transform.localPosition = Vector3.zero;
        //     var _child = new GameObject("icon", typeof(RectTransform));
        //     _child.transform.SetParent(_go.transform, false);
        //     _child.transform.localPosition = Vector3.zero;
        //     var img = _child.AddComponent<Image>();
        //     img.raycastTarget = false;
        //     var res = _child.AddComponent<UIImageRes>();
        //     res.SetUseNativeSize(false);
        //     _go.AddComponent<UIEffectFlyIcon>();

        //     GameObjectPoolManager.Instance.PreparePool(mItemType, _go);
        //     GameObjectPoolManager.Instance.ReleaseObject(mItemType, _go);
        // }

        // private IEnumerator _CoLoadRes()
        // {
        //     var loadRequest = EL.Resource.ResManager.LoadAsset(kResGroupName, kResFilePath);
        //     yield return loadRequest;
        //     if (loadRequest.isSuccess && loadRequest.asset != null)
        //     {
        //         if (mEffectHolder != null)
        //         {
        //             GameObject.Destroy(mEffectHolder);
        //             mEffectHolder = null;
        //         }
        //         mEffectHolder = GameObject.Instantiate(loadRequest.asset as GameObject, UIManager.Instance.GetLayerRootByType(UILayer.Effect));
        //         var trans = mEffectHolder.transform;
        //         trans.localPosition = Vector3.zero;
        //         for (int i = 0; i < (int)ParticleEffectType.Max; ++i)
        //         {
        //             var ph = trans.Find(((ParticleEffectType)i).ToString());
        //             if (ph == null)
        //             {
        //                 throw new System.NotImplementedException();
        //             }
        //             mHolder[i] = ph.GetComponent<UIEffectParticleHolder>();
        //         }
        //     }
        //     else
        //     {
        //         EL.DebugEx.FormatError("UIEffectUtility._CoLoadRes failed -> {0}", loadRequest.error);
        //     }
        // }

        // public void RegisterAttractorHandler(ParticleEffectType et, Action cb)
        // {
        //     mHolder[(int)et].onAttractedHandler = cb;
        // }

        // public void UnRegisterAttractorHandler(ParticleEffectType et)
        // {
        //     mHolder[(int)et].onAttractedHandler = null;
        // }

        // public void FlyRewardParticle(ParticleEffectType et, Vector3 from, Vector3 to, int particleNum)
        // {
        //     var holder = mHolder[(int)et];
        //     holder.SetAttractorPos(to);
        //     holder.Emit(from, particleNum);
        // }

        // public UIEffectFlyIcon CreateIcon()
        // {
        //     var go = GameObjectPoolManager.Instance.CreateObject(mItemType, UIManager.Instance.GetLayerRootByType(UILayer.Effect));
        //     return go.GetComponent<UIEffectFlyIcon>();
        // }

        // public void ReleaseIcon(GameObject go)
        // {
        //     go.GetComponent<UIClearableItem>().Clear();
        //     GameObjectPoolManager.Instance.ReleaseObject(mItemType, go);
        // }

        // public void FlyIconWithReward(RewardCommitData reward, int iconId, Vector3 from, Vector3 to, float size, bool needFlyIcon = true)
        // {
        //     if (needFlyIcon)
        //     {
        //         var res = Game.Manager.rewardMan.GetRewardIcon(iconId, 1);
        //         var snd = Game.Manager.rewardMan.GetRewardFlySound(reward.rewardId, reward.rewardCount);
        //         FlyIcon(res, from, to, size, snd, () =>
        //         {
        //             Game.Manager.rewardMan.CommitReward(reward);
        //         });
        //     }
        //     else
        //     {
        //         Game.Manager.rewardMan.CommitReward(reward);
        //     }
        // }

        // public void FlyIconByReward(RewardCommitData reward, Vector3 from, Vector3 to, float size, bool needFlyIcon = true)
        // {
        //     if (needFlyIcon)
        //     {
        //         var res = Game.Manager.rewardMan.GetRewardIcon(reward.rewardId, reward.rewardCount);
        //         var snd = Game.Manager.rewardMan.GetRewardFlySound(reward.rewardId, reward.rewardCount);
        //         FlyIcon(res, from, to, size, snd, () => { Game.Manager.rewardMan.CommitReward(reward); });
        //     }
        //     else
        //     {
        //         Game.Manager.rewardMan.CommitReward(reward);
        //     }
        // }

        // public void FlyIcon(AssetConfig res, Vector3 from, Vector3 to, float size, RewardFlySound sound, Action cb = null)
        // {
        //     var effIcon = CreateIcon();
        //     effIcon.Setup(res, size);
        //     effIcon.holder.position = to;
        //     effIcon.icon.position = from;
        //     effIcon.icon.localScale = Vector3.one;
        //     var seq = DOTween.Sequence();
        //     seq.Append(effIcon.icon.DOPunchScale(Vector3.one * 0.4f, 0.2f).SetEase(Ease.OutBack));
        //     seq.Append(effIcon.icon.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.InCubic));
        //     seq.OnComplete(() =>
        //     {
        //         ReleaseIcon(effIcon.gameObject);
        //         cb?.Invoke();
        //     });
        //     Game.Manager.audioMan.TriggerSound(sound.startSndEvent);
        //     effIcon.Apply(seq);
        // }
        
        // public void FlyCustomItemIcon(int itemId, float size, Vector3 from, Vector3 to, Action cb = null)
        // {
        //     var effIcon = CreateIcon();
        //     var res = Merge.Env.Instance.GetItemConfig(itemId).Icon.ConvertToAssetConfig();
        //     effIcon.Setup(res, size);
        //     effIcon.holder.position = to;
        //     effIcon.icon.position = from;
        //     effIcon.icon.localScale = Vector3.one;
        //     var seq = DOTween.Sequence();
        //     seq.Append(effIcon.icon.DOPunchScale(Vector3.one * 0.4f, 0.2f).SetEase(Ease.OutBack));
        //     seq.Append(effIcon.icon.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.InCubic));
        //     seq.OnComplete(() =>
        //     {
        //         ReleaseIcon(effIcon.gameObject);
        //         cb?.Invoke();
        //     });
        //     effIcon.Apply(seq);
        // }

        // public void FlyItemWhenOrderFinish(int itemId, float size, Transform target, Vector3 from, Action cb)
        // {
        //     var effIcon = CreateIcon();
        //     var res = Merge.Env.Instance.GetItemConfig(itemId).Icon.ConvertToAssetConfig();
        //     effIcon.Setup(res, size, target.transform);
        //     effIcon.holder.position = target.transform.position;
        //     effIcon.icon.position = from;
        //     effIcon.icon.localScale = Vector3.one;
        //     var seq = DOTween.Sequence();
        //     seq.Append(effIcon.icon.DOPunchScale(Vector3.one * 0.4f, 0.2f).SetEase(Ease.OutBack));
        //     seq.Append(effIcon.icon.DOAnchorPos(Vector2.zero, 0.6f).SetEase(Ease.InCubic));
        //     seq.OnComplete(() =>
        //     {
        //         ReleaseIcon(effIcon.gameObject);
        //         cb?.Invoke();
        //     });
        //     effIcon.Apply(seq);
        // }

        // public void FlyOrderStar(int itemId, float size, Transform target, Vector3 from, Action cb)
        // {
        //     var effIcon = CreateIcon();
        //     var res = Merge.Env.Instance.GetItemConfig(itemId).Icon.ConvertToAssetConfig();
        //     effIcon.Setup(res, size, target.transform);
        //     effIcon.holder.position = target.transform.position;
        //     effIcon.icon.position = from;
        //     effIcon.icon.localScale = Vector3.one;
        //     var path = new Vector3[] { Vector3.zero, effIcon.icon.localPosition / 2 + Vector3.up * 100f, Vector3.zero };
        //     var tween = effIcon.icon.DOLocalPath(path, .3f, PathType.CubicBezier, PathMode.TopDown2D).OnComplete(() =>
        //     {
        //         ReleaseIcon(effIcon.gameObject);
        //         cb?.Invoke();
        //     });
        //     effIcon.Apply(tween);
        // }
    }
}