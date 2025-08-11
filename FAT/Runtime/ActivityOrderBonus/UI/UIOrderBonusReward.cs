using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIOrderBonusReward : UIBase
    {
        public GameObject goHitEff;
        public Transform rewardNode;
        public Transform moveNode;
        public AnimationCurve speed;
        public AnimationCurve scale;
        public float endScale;
        public float startRotation;
        public float delay;
        public float duration;
        public float close;
        public List<Transform> posList;
        private float _offset;
        private SkeletonGraphic _reward;
        private SkeletonGraphic _rocket;
        private ActivityOrderBonus _act;
        private string hit_res_key => $"{_act.Id}_eff_hit_feedback";

        protected override void OnCreate()
        {
            _offset = (transform as RectTransform).rect.width / 2;
        }

        protected override void OnParse(params object[] items)
        {
            _act = items[0] as ActivityOrderBonus;
            var phase = _act.phase;
            phase--;
            if (phase > 2) phase = 2;
            _rocket = moveNode.GetChild(0).GetChild(phase).GetComponent<SkeletonGraphic>();
            _reward = rewardNode.GetChild(phase).GetComponent<SkeletonGraphic>();
            _rocket.transform.localScale = Vector3.one;
            _reward.transform.localScale = Vector3.one;
        }

        protected override void OnPreOpen()
        {
            InitrocketAnim();
            if (GameObjectPoolManager.Instance.HasPool(hit_res_key))
                return;
            GameObjectPoolManager.Instance.PreparePool(hit_res_key, goHitEff);
        }

        protected override void OnPostOpen()
        {
            _rocket.AnimationState.SetAnimation(0, "idle", false);
            _reward.AnimationState.SetAnimation(0, "idle", false);
            IEnumerator enumerator()
            {
                yield return new WaitForSeconds(close);
                Close();
            }
            Game.Instance.StartCoroutineGlobal(enumerator());
            PlayRocketAnim();
            Game.Manager.audioMan.TriggerSound("OrderBonusFly");
        }

        private void InitrocketAnim()
        {
            moveNode.localPosition = Vector3.zero;
            moveNode.localScale = Vector3.one;
            moveNode.transform.eulerAngles = new Vector3(0, 0, startRotation);
        }

        private void PlayRocketAnim()
        {
            var pos = new List<Transform>();
            pos.AddRange(posList);
            var poslist = pos.Select(p => p.position + new Vector3(_offset, 0)).ToList();
            UIFlyFactory.GetFlyTarget(FlyType.OrderBonus, out var end);
            poslist.Add(end);
            var posArr = poslist.ToArray();
            posArr.Append(end);
            var seq = DOTween.Sequence();
            seq.AppendInterval(delay);
            seq.Append(moveNode.DOPath(posArr, duration, PathType.CatmullRom, PathMode.TopDown2D, 10, Color.white).SetEase(speed).SetLookAt(0.01f));
            seq.Join(moveNode.DOScale(endScale, duration).SetEase(scale));
            seq.OnComplete(() =>
            {

                var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
                var hitEff = GameObjectPoolManager.Instance.CreateObject(hit_res_key, effRoot);
                hitEff.SetActive(false);
                hitEff.transform.position = end;
                hitEff.SetActive(true);
                var script = hitEff.GetOrAddComponent<MBAutoRelease>();
                script.Setup(hit_res_key, 1f);
                Game.Manager.audioMan.TriggerSound("OrderBonusHit");
                EL.MessageCenter.Get<MSG.ROCKET_ANIM_COMPLETE>().Dispatch();
            });
        }

        protected override void OnPostClose()
        {
            for (int i = 0; i < rewardNode.childCount; i++)
            {
                rewardNode.GetChild(i).localScale = Vector3.zero;
            }
            for (int i = 0; i < moveNode.GetChild(0).childCount; i++)
            {
                moveNode.GetChild(0).GetChild(i).localScale = Vector3.zero;
            }
        }
    }
}

