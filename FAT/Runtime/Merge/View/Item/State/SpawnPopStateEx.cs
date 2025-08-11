/*
 * @Author: qun.chao
 * @Date: 2024-09-14 10:54:02
 */
using UnityEngine;
using DG.Tweening;
using FAT.Merge;
using EL;

namespace FAT
{
    public class MergeItemSpawnPopStateEx : MergeItemBaseState
    {
        private bool isTweenFinished;
        private Sequence seq;
        private bool effectShowed;

        public MergeItemSpawnPopStateEx(MBItemView v) : base(v)
        { }

        public override void OnEnter()
        {
            base.OnEnter();

            view.gameObject.SetActive(true);
            BoardViewManager.Instance.ReAnchorItemForMove(view.transform);
            var trans = view.transform as RectTransform;

            var midOffsetY = BoardUtility.spawnPopParam.flyMidOffsetY;
            var endOffsetDist = BoardUtility.spawnPopParam.flyEndOffsetDist;

            var (p0, p1, p2, pEnd) = BoardUtility.CalcBezierControlPosForSpawnByCoord(view.spawnContext.spawner.coord, view.data.coord, midOffsetY, endOffsetDist);
            trans.anchoredPosition = p0;

            p0 = BoardUtility.CalcItemLocalPosInMoveRoot(p0);
            p1 = BoardUtility.CalcItemLocalPosInMoveRoot(p1);
            p2 = BoardUtility.CalcItemLocalPosInMoveRoot(p2);
            pEnd = BoardUtility.CalcItemLocalPosInMoveRoot(pEnd);

            var startDelay = BoardUtility.spawnPopParam.startDelay; //延迟启动时间
            var curveDuration = (p2 - p0).magnitude / 100f * BoardUtility.spawnPopParam.flyDurationPer100 + BoardUtility.spawnPopParam.flyDurationExtra;
            var spawnAnimDuration = curveDuration - BoardUtility.spawnPopParam.flyEndDropTimeOffset;
            var curveEase = BoardUtility.spawnPopParam.flyEase;
            var moveDuration = BoardUtility.spawnPopParam.moveDuration;
            var moveEase = BoardUtility.spawnPopParam.moveEase;

            var curveLifeTime = 0f;
            isTweenFinished = false;
            effectShowed = false;
            view.transform.localScale = Vector3.zero;

            // mix产出时 多等待0.5s 棋子被扣除后再播放spawn动画
            if (view.spawnContext.type == ItemSpawnContext.SpawnType.MixSource ||
                view.spawnContext.type == ItemSpawnContext.SpawnType.TrigAutoSource )
            {
                //如果棋子带有MBResHolderTrig则使用其配置的延迟启动时间, 否则默认+0.5s
                var resHolderTrig = view.GetResHolder() as MBResHolderTrig;
                if (resHolderTrig != null)
                {
                    startDelay += resHolderTrig.SpawnDelayTime;
                }
                else
                {
                    startDelay += 0.5f;
                }
            }
            var _seq = DOTween.Sequence();
            _seq.AppendInterval(startDelay);
            _seq.AppendCallback(() =>
            {
                view.transform.localScale = Vector3.one;
                view.PlaySpawn();
            });
            _seq.Append(DOTween.To(() => curveLifeTime, x => curveLifeTime = x, 1f, curveDuration).
                SetEase(curveEase).
                OnUpdate(() => { trans.anchoredPosition = BoardUtility.CalculateBezierPoint(curveLifeTime, p0, p1, p2); }));
            _seq.InsertCallback(startDelay + spawnAnimDuration, () => view.PlayDropToGround());
            _seq.AppendCallback(_OnShowOnBoardEffect);
            _seq.Append(trans.DOAnchorPos(pEnd, moveDuration).SetEase(moveEase));
            _seq.Play().OnComplete(_OnTweenFinished);
            seq = _seq;

            // 开场即播放toast
            _ShowToast();
        }

        public override void OnLeave()
        {
            base.OnLeave();
            if (seq != null)
            {
                seq.Kill();
                seq = null;
            }
            isTweenFinished = true;
            view.transform.localScale = Vector3.one;
            if (!effectShowed)
            {
                if (view.spawnContext.toastType == fat.rawdata.Toast.Max)
                    view.AddOnBoardEffect4X();
                else
                    view.AddOnBoardEffect();
            }
        }

        public override ItemLifecycle Update(float dt)
        {
            if (isTweenFinished)
            {
                return ItemLifecycle.Move;
            }
            return base.Update(dt);
        }

        private bool _shouldShowToast => view.spawnContext.toastType != fat.rawdata.Toast.Empty &&
                                        Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(fat.rawdata.FeatureEntry.FeatureOutputToast);

        private void _OnShowOnBoardEffect()
        {
            if (!view.hasNewTip && _shouldShowToast)
            {
                if (view.spawnContext.toastType == fat.rawdata.Toast.Max)
                {
                    Game.Manager.audioMan.TriggerSound("MaxToast");
                }
                else
                {
                    Game.Manager.audioMan.TriggerSound("BoardLegendItem");
                }
                VibrationManager.VibrateHeavy();
            }
            effectShowed = true;
            if (view.spawnContext.toastType == fat.rawdata.Toast.Max)
                view.AddOnBoardEffect4X();
            else
                view.AddOnBoardEffect();
        }

        private void _ShowToast()
        {
            // 新物品提示优先级高
            if (view.hasNewTip)
                view.TryResolveNewItemTip();
            else if (_shouldShowToast)
                Game.Manager.commonTipsMan.ShowPopTips(view.spawnContext.toastType, BoardUtility.GetWorldPosByCoord(view.data.coord));
        }

        private void _OnTweenFinished()
        {
            isTweenFinished = true;
        }
    }
}