/*
 * @Author: qun.chao
 * @Date: 2021-07-23 11:59:31
 */

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
重新统一管理
*/
namespace FAT
{
    public static class UIFlyUtility
    {
        #region api

        public static void FlyRewardList(List<RewardCommitData> rewards, Vector3 from,
            Action finish = null, float size = 0)
        {
            foreach (var reward in rewards) FlyReward(reward, from, finish, size);
        }

        public static void FlyReward(RewardCommitData reward, Vector3 from, Action finish = null, float size = 0)
        {
            if (UIFlyFactory.CheckNeedFlyIcon(reward.rewardId))
            {
                var ft = UIFlyFactory.ResolveFlyType(reward.rewardId);
                var to = UIFlyFactory.ResolveFlyTarget(ft);
                var item = _CreateItemFly(reward.rewardId, reward.rewardCount, from, to, FlyStyle.Reward, ft, size);
                item.Reward = reward;
                _FlyReward(item, finish);
            }
            else
            {
                Game.Manager.rewardMan.CommitReward(reward);
            }
        }

        public static void FlyRewardSetType(RewardCommitData reward, Vector3 from, FlyType type, Action finish = null,
            float size = 0)
        {
            if (UIFlyFactory.CheckNeedFlyIcon(reward.rewardId))
            {
                var to = UIFlyFactory.ResolveFlyTarget(type);

                if (to.x < 0 && (type == FlyType.MiniBoard || type == FlyType.MiniBoardMulti)) to.x = -10;

                var offset = type switch
                {
                    FlyType.Handbook => Vector3.zero,
                    _ => Vector3.up * 120
                };
                var item = _CreateItemFly(reward.rewardId, reward.rewardCount, from + offset, to,
                    FlyStyle.Reward, type, size);
                item.Reward = reward;
                if (type == FlyType.MiniBoard || type == FlyType.MiniBoardMulti || type == FlyType.WishBoardToken)
                {
                    var icon = _CreateItemFly(reward.rewardId, reward.rewardCount, from, from + new Vector3(0, 120, 0),
                        FlyStyle.Score, type, 136f);
                    _FlyScore(icon, item);
                }
                else
                {
                    _FlyReward(item, finish);
                }
            }
            else
            {
                Game.Manager.rewardMan.CommitReward(reward);
            }
        }

        private static void _FlyScore(FlyableItemSlice icon, FlyableItemSlice item)
        {
            var trans = _CreateFlyObj(icon);
            icon.FlyType = FlyType.None;
            trans.GetComponent<Image>().color = new Color(255, 255, 255, 0);
            trans.localScale = 0.8f * Vector3.one;
            var seq = DOTween.Sequence();
            seq.Pause();
            seq.Append(trans.DOMove(icon.WorldTo, 0.6f));
            seq.Join(trans.GetComponent<Image>().DOFade(1, 0.2f));
            seq.Join(trans.DOScale(Vector3.one, 0.2f));
            seq.OnComplete(() =>
            {
                UIFlyManager.Instance.OnCollected(icon, trans as RectTransform);
                _FlyReward(item, null);
            });
            seq.Play();
        }

        public static void FlyCost(int id, int count, Vector3 to, float size = 0f, Action finish = null)
        {
            if (!UIFlyFactory.CheckNeedFlyIcon(id))
                return;

            var ft = UIFlyFactory.ResolveFlyType(id);
            var from = UIFlyFactory.ResolveFlyTarget(ft);
            var item = _CreateItemFly(id, count, from, to, FlyStyle.Cost, ft, size);
            _FlyCost(item, finish);
        }

        public static void FlyCustom(int id, int count, Vector3 from, Vector3 to, FlyStyle style,
            FlyType type,
            Action finish = null,
            Action<Transform> tween = null, float size = 0, int split = 0)
        {
            var item = _CreateItemFly(id, count, from, to, style, type, size, split);
            item.Style = style;
            //item.OnCollectedPartially = it => UIFlyManager.Instance.TryFeedbackFlyable(it);
            item.OnCollectedWholly = it =>
            {
                //UIFlyManager.Instance.TryFeedbackFlyable(it);
                UIFlyManager.Instance.TryFeedbackOnClaimResource(it);
                finish?.Invoke();
            };
            _CreateFLyTween(item);
        }

        public static void FlyCompleteOrder(int id, int count, Vector3 from, float wait, Func<Vector3> target,
            Action finish = null)
        {
            var item = _CreateItemFly(id, count, from, Vector3.zero, FlyStyle.Cost, FlyType.MergeItemFlyTarget, 0);
            item.OnCollectedWholly = it =>
            {
                UIFlyManager.Instance.TryFeedbackOnClaimResource(it);
                finish?.Invoke();
            };
            MessageCenter.Get<MSG.FLY_ICON_START>().Dispatch(item);
            var trans = _CreateFlyObj(item);
            Game.Instance.StartCoroutineGlobal(_TweenOrder(item, wait, target, trans));
        }

        #endregion

        private static FlyableItemSlice _CreateItemFly(int id, int num, Vector3 from, Vector3 to, FlyStyle style,
            FlyType type, float size, int split = 0)
        {
            var item = UIFlyManager.Instance.Alloc();
            item.Init(id, num, from, to, style, type, size, split);
            return item;
        }

        private static void _FlyReward(FlyableItemSlice item, Action finish)
        {
            item.OnCollectedPartially = it => UIFlyManager.Instance.TryCollectReward(it);
            item.OnCollectedWholly = (it) =>
            {
                UIFlyManager.Instance.TryCollectReward(it);
                finish?.Invoke();
            };
            _CreateFLyTween(item);
            switch (item.FlyType)
            {
                case FlyType.EventScore:
                    break;
                case FlyType.Handbook:
                    Game.Manager.audioMan.TriggerSound("AddGemShort");
                    break;
                case FlyType.MineScore:
                    Game.Manager.audioMan.TriggerSound("MineGetMilestoneToken");
                    break;
                default:
                    _TryPlaySound(item.ID, item.Amount);
                    break;
            }
        }

        private static void _FlyCost(FlyableItemSlice item, Action finish)
        {
            item.OnCollectedPartially = it => UIFlyManager.Instance.TryFeedbackOnClaimResource(it);
            item.OnCollectedWholly = (it) =>
            {
                UIFlyManager.Instance.TryFeedbackOnClaimResource(it);
                finish?.Invoke();
            };
            _CreateFLyTween(item);
            _TryPlaySound(item.ID, item.Amount);
        }

        private static void _TryPlaySound(int id, int count)
        {
            var snd = Game.Manager.rewardMan.GetRewardFlySound(id, count);
            Game.Manager.audioMan.TriggerSound(snd.startSndEvent);
        }

        private static void _CreateFLyTween(FlyableItemSlice item)
        {
            MessageCenter.Get<MSG.FLY_ICON_START>().Dispatch(item);
            for (var i = 0; i < item.SplitNum; i++)
            {
                var trans = _CreateFlyObj(item);
                trans.eulerAngles = Vector3.zero;
                switch (item.Style)
                {
                    case FlyStyle.Cost:
                        {
                            _TweenCost(item, trans, i);
                            break;
                        }
                    case FlyStyle.Show:
                        {
                            _TweenShowReward(item, trans);
                            break;
                        }
                    default:
                        {
                            if (item.Reason != FlyReason.CoinChange &&
                                item.Reason != FlyReason.ExpChange &&
                                item.FlyType != FlyType.MiniBoard && item.FlyType != FlyType.MiniBoardMulti &&
                                item.FlyType != FlyType.GuessMilestone && item.FlyType != FlyType.OrderLikeToken &&
                                item.FlyType != FlyType.MineToken && item.FlyType != FlyType.MineScore &&
                                item.FlyType != FlyType.FarmToken &&
                                item.FlyType != FlyType.DuelToken && item.FlyType != FlyType.DuelMilestone && item.FlyType != FlyType.FightBoardMonster
                                && item.FlyType != FlyType.FightBoardTreasure
                                && item.FlyType != FlyType.WishBoardScore)
                                _TweenRewardSingle(item, trans);
                            else if (item.FlyType == FlyType.Inventory)
                                _TweenRewardSingle(item, trans);
                            else if (item.FlyType == FlyType.FightBoardMonster || item.FlyType == FlyType.FightBoardTreasure)
                                _TweenAttack(item, trans, i);
                            else
                                _TweenRewardScatter(item, trans, i);

                            break;
                        }
                }
            }

            if (item.FlyType == FlyType.DecorateToken)
                _TweenTarget(item.FlyType, item);
            if (item.FlyType == FlyType.MineScore || item.FlyType == FlyType.WishBoardScore)
            {
                var trans = _CreateFlyNum(item);
                _TweenNnum(item, trans);
            }
        }

        private static Transform _CreateFlyObj(FlyableItemSlice item)
        {
            var trans = GameObjectPoolManager.Instance.CreateObject(UIFlyManager.ItemType,
                UIManager.Instance.GetLayerRootByType(UILayer.Effect)).transform as RectTransform;
            var resIcon = Game.Manager.rewardMan.GetRewardIcon(item.ID, item.Amount);
            trans.GetComponent<UIImageRes>().SetImage(resIcon);
            trans.sizeDelta = Vector2.one * item.Size;
            item.Transforms.Add(trans);
            trans.SetPositionAndRotation(item.WorldFrom, Quaternion.Euler(new Vector3(0, 0, 0)));
            if (item.Style == FlyStyle.Show)
            {
                trans.Find("Num").gameObject.SetActive(true);
                trans.Find("Num1").gameObject.SetActive(false);
                trans.Find("Num2").gameObject.SetActive(false);
                trans.Find("Num").GetComponent<TextMeshProUGUI>().text =
                    UIUtility.SpecialCountText(item.ID, item.Amount, out var countStr)
                        ? countStr
                        : item.Amount.ToString();
            }
            else if (item.Style == FlyStyle.Score)
            {
                trans.Find("Num").gameObject.SetActive(false);
                trans.Find("Num1").gameObject.SetActive(true);
                trans.Find("Num2").gameObject.SetActive(false);
                trans.Find("Num1").GetComponent<TextMeshProUGUI>().text =
                    UIUtility.SpecialCountText(item.ID, item.Amount, out var countStr)
                        ? countStr
                        : item.Amount.ToString();
            }
            else
            {
                trans.Find("Num").gameObject.SetActive(false);
                trans.Find("Num1").gameObject.SetActive(false);
                trans.Find("Num2").gameObject.SetActive(false);
            }

            return trans;
        }

        private static Transform _CreateFlyNum(FlyableItemSlice item)
        {

            var trans = GameObjectPoolManager.Instance.CreateObject(UIFlyManager.ItemType,
                UIManager.Instance.GetLayerRootByType(UILayer.Effect)).transform as RectTransform;
            trans.GetComponent<UIImageRes>().Clear();
            trans.sizeDelta = Vector2.one * item.Size;
            trans.SetPositionAndRotation(item.WorldFrom, Quaternion.Euler(new Vector3(0, 0, 0)));
            trans.Find("Num").gameObject.SetActive(false);
            trans.Find("Num1").gameObject.SetActive(false);
            trans.Find("Num2").gameObject.SetActive(true);
            trans.Find("Num2").GetComponent<TextMeshProUGUI>().text = item.Amount.ToString();
            return trans;
        }


        private static void _TweenCost(FlyableItemSlice item, Transform trans, int idx)
        {
            var seq = DOTween.Sequence();
            seq.Pause();
            if (item.Reason == FlyReason.CoinChange)
            {
                UIFlyFactory.CreateStopTween(seq, trans, idx * UIFlyConfig.Instance.durationAdd);
                UIFlyFactory.CreateStraightTween(seq, trans, item.WorldTo);
                seq.OnComplete(() => UIFlyManager.Instance.OnCollected(item, trans as RectTransform));
                seq.Play();
            }
            else
            {
                UIFlyFactory.CreateElasticTween(seq, trans, true, UIFlyConfig.Instance.scaleElasticStartTo,
                    UIFlyConfig.Instance.durationElasticStart, UIFlyConfig.Instance.curveElasticStart);
                UIFlyFactory.CreateStraightTween(seq, trans, item.WorldTo);
                UIFlyFactory.CreateElasticTween(seq, trans, false, UIFlyConfig.Instance.scaleElasticEnd,
                    UIFlyConfig.Instance.durationFly, UIFlyConfig.Instance.curveElasticEnd);
                seq.OnComplete(() => UIFlyManager.Instance.OnCollected(item, trans as RectTransform));
                seq.Play();
            }
        }

        private static IEnumerator _TweenOrder(FlyableItemSlice item, float wait, Func<Vector3> target, Transform trans)
        {
            yield return new WaitForSeconds(wait);
            var seq = DOTween.Sequence();
            seq.Pause();
            UIFlyFactory.CreateElasticTween(seq, trans, true, UIFlyConfig.Instance.scaleElasticStartTo,
                UIFlyConfig.Instance.durationElasticStart, UIFlyConfig.Instance.curveElasticStart);
            UIFlyFactory.CreateStraightTween(seq, trans, target());
            UIFlyFactory.CreateElasticTween(seq, trans, false, UIFlyConfig.Instance.scaleElasticEnd,
                UIFlyConfig.Instance.durationFly, UIFlyConfig.Instance.curveElasticEnd);
            seq.OnComplete(() => UIFlyManager.Instance.OnCollected(item, trans as RectTransform));
            seq.Play();
        }

        private static void _TweenRewardScatter(FlyableItemSlice item, Transform trans, int idx)
        {
            var seq = DOTween.Sequence();
            seq.Pause();
            trans.localScale = Vector3.zero;
            UIFlyFactory.CreateScatterTween(seq, trans);
            UIFlyFactory.CreateStopTween(seq, trans,
                UIFlyConfig.Instance.durationStop + idx * UIFlyConfig.Instance.durationAdd);
            if (item.Reason == FlyReason.ExpChange || item.FlyType == FlyType.EventScore ||
                item.FlyType == FlyType.RaceToken || item.FlyType == FlyType.MiniBoard || item.FlyType == FlyType.MiniBoardMulti
                || item.FlyType == FlyType.EndlessToken || item.FlyType == FlyType.EndlessThreeToken
                || item.FlyType == FlyType.GuessMilestone || item.FlyType == FlyType.DuelToken)
                UIFlyFactory.CreateCurveTween(seq, trans, item.WorldTo);
            else
                UIFlyFactory.CreateStraightTween(seq, trans, item.WorldTo);

            seq.OnComplete(() => UIFlyManager.Instance.OnCollected(item, trans as RectTransform));
            seq.Play();
        }

        private static void _TweenAttack(FlyableItemSlice item, Transform trans, int idx)
        {
            var seq = DOTween.Sequence();
            seq.Pause();
            trans.localScale = Vector3.zero;
            UIFlyFactory.CreateScatterTween(seq, trans, UIFlyConfig.Instance.attackScatter);
            UIFlyFactory.CreateStopTween(seq, trans,
                UIFlyConfig.Instance.attackInterval + idx * UIFlyConfig.Instance.durationAdd);
            var stopTime = UIFlyConfig.Instance.attackInterval + idx * UIFlyConfig.Instance.durationAdd;
            seq.Join(DOTween.To(() => trans.eulerAngles, x => trans.eulerAngles = x, new Vector3(0, 0, UIFlyConfig.Instance.rotateSpeed * stopTime), stopTime));
            UIFlyFactory.CreateStraightTween(seq, trans, item.WorldTo, 0.3f);
            var startRotation = trans.eulerAngles;
            seq.Join(DOTween.To(() => trans.eulerAngles, x => trans.eulerAngles = startRotation + x, new Vector3(0, 0, UIFlyConfig.Instance.rotateSpeed * 0.3f), 0.3f));
            seq.OnComplete(() =>
            {
                UIFlyManager.Instance.OnCollected(item, trans as RectTransform);
            });
            seq.Play();
        }

        //获得单独一个棋子时，表现效果和和通常的获得奖励效果差异较大，因此单独实现
        private static void _TweenRewardSingle(FlyableItemSlice item, Transform trans)
        {
            var seq = DOTween.Sequence();
            seq.Pause();
            trans.localScale = Vector3.one * UIFlyConfig.Instance.scaleRewardElasticStart;
            UIFlyFactory.CreateElasticTween(seq, trans, true, UIFlyConfig.Instance.scaleRewardElasticStartTo,
                UIFlyConfig.Instance.durationRewardElasticStart, UIFlyConfig.Instance.curveRewardElasticStart);
            UIFlyFactory.CreateStraightTween(seq, trans, item.WorldTo);
            UIFlyFactory.CreateElasticTween(seq, trans, false, UIFlyConfig.Instance.scaleRewardElasticEnd,
                UIFlyConfig.Instance.durationFly, UIFlyConfig.Instance.curveRewardElasticEnd);
            seq.OnComplete(() => UIFlyManager.Instance.OnCollected(item, trans as RectTransform));
            seq.Play();
        }

        private static void _TweenShowReward(FlyableItemSlice item, Transform trans)
        {
            var seq = DOTween.Sequence();
            seq.Pause();
            trans.localScale = Vector3.one * UIFlyConfig.Instance.scaleShowRewardStart;
            UIFlyFactory.CreateShowRewardTween(seq, trans, 1f, UIFlyConfig.Instance.durationShowRewardStart);
            UIFlyFactory.CreateStopTween(seq, trans, UIFlyConfig.Instance.durationShowReward);
            UIFlyFactory.CreateEndShowRewardTween(seq, trans, item.WorldTo, UIFlyConfig.Instance.scaleShowRewardEnd,
                UIFlyConfig.Instance.durationShowRewardEnd);
            seq.OnComplete(() => UIFlyManager.Instance.OnCollected(item, trans as RectTransform));
            seq.Play();
        }

        private static void _TweenTarget(FlyType ft, FlyableItemSlice item)
        {
            var obj = UIFlyFactory.TryGetObj(ft);
            if (obj == null)
                return;
            var res = obj.GetComponent<UIImageRes>();
            if (res != null)
                switch (ft)
                {
                    case FlyType.DecorateToken:
                        {
                            Game.Manager.decorateMan.Activity?.StartRemindVisual.Refresh(res, "flytarget");
                            break;
                        }
                }

            var startPos = obj.transform.position;
            var seq = DOTween.Sequence();
            seq.Append(obj.GetComponent<Image>().DOFade(1, 0.3f));
            seq.Join(obj.transform.DOMove(item.WorldTo, 0.3f));
            UIFlyFactory.CreateStopTween(seq, obj.transform, 2f);
            seq.Append(obj.transform.DOMove(startPos, 0.3f));
            seq.Join(obj.GetComponent<Image>().DOFade(0, 0.3f));
        }

        private static void _TweenNnum(FlyableItemSlice item, Transform trans)
        {
            var seq = DOTween.Sequence();
            seq.Pause();
            seq.Append(trans.DOScale(Vector3.one, 0.7f).From(Vector3.zero));
            seq.Join(trans.DOLocalMoveY(trans.localPosition.y + 100f, 0.7f));
            seq.OnComplete(() =>
            {
                GameObjectPoolManager.Instance.ReleaseObject(UIFlyManager.ItemType, trans.gameObject);
            });
            seq.Play();
        }
    }
}
