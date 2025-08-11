using System;
using System.Collections;
using DG.Tweening;
using FAT.Merge;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMiniBoardMultiFly : UIBase
    {
        public Transform keyNode;
        public UIImageRes key;
        public GameObject effectBack;
        public GameObject effectOnce;
        public Transform centerNode;
        public Transform Circle;
        public Transform circleMask;
        public UIGuideMask mask;

        public float ShowTime;
        public float StopTime;
        public float DisappearTime;

        private Vector3 from;
        private Vector3 target;
        private Action callback;

        protected override void OnPreOpen()
        {
            effectBack.SetActive(false);
            effectOnce.SetActive(false);
            keyNode.gameObject.SetActive(true);
            key.transform.localScale = Vector3.one;
            key.SetImage(Game.Manager.miniBoardMultiMan.GetCurInfoConfig()?.KeyIcon.ConvertToAssetConfig());
            mask.GetComponent<RawImage>().material.SetFloat("_AlphaAnim", 0f);
            Game.Instance.StartCoroutineGlobal(PlayAnim());
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 3) return;
            var item = items[0] as Item;
            target = BoardUtility.GetWorldPosByCoord(item.coord);
            Circle.position = target;
            circleMask.position = centerNode.position;
            from = (Vector3)items[1];
            callback = items[2] as Action;
            mask.Show(BoardViewManager.Instance.GetItemView(item.id)?.transform);
        }

        private IEnumerator PlayAnim()
        {
            var seq = DOTween.Sequence();
            seq.Append(keyNode.DOMove(centerNode.position, ShowTime).SetEase(Ease.OutSine).From(from));
            seq.Join(key.transform.DOScale(Vector3.one * 2f, ShowTime).SetEase(Ease.OutSine).From(Vector3.one));
            Game.Manager.audioMan.TriggerSound("MiniboardMultiGetKey");
            yield return new WaitForSeconds(ShowTime);
            effectBack.SetActive(true);
            yield return new WaitForSeconds(StopTime);
            seq.Append(keyNode.DOMove(target, DisappearTime).SetEase(Ease.InQuad));
            seq.Join(key.transform.DOScale(Vector3.zero, DisappearTime).SetEase(Ease.InQuad));
            Game.Manager.audioMan.TriggerSound("MiniboardMultiOpenDoor");
            yield return new WaitForSeconds(DisappearTime);
            effectOnce.SetActive(true);
            callback?.Invoke();
            yield return new WaitForSeconds(1f);
            Close();
        }

        protected override void OnPostClose()
        {
            effectOnce.SetActive(false);
            key.transform.localScale = Vector3.zero;
        }
    }
}