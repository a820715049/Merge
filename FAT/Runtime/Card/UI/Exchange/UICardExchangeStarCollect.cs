/*
 *@Author:chaoran.zhang
 *@Desc:星星收集UI
 *@Created Time:2024.09.19 星期四 10:19:34
 */

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FAT
{
    public class UICardExchangeStarCollect : UIBase
    {
        public List<int> clipNum = new();
        public List<int> rankList = new();
        public float size;
        public float width;
        public float height;
        public float minWidth;
        public float minHeight;
        public float stopTime;
        public float delay;
        public Sprite star;
        private Transform _album;
        private Action _callBack;

        protected override void OnCreate()
        {
            transform.Access("Content/Album", out _album);
        }

        protected override void OnParse(params object[] items)
        {
            var from = (Vector3)items[0];
            var num = (int)items[1];
            _callBack = (Action)items[2];
            StartFly(from, num);
        }

        /// <summary>
        /// 播放星星飞行动画
        /// </summary>
        /// <param name="from">飞行起始点</param>
        /// <param name="num">获得的星星数量</param>
        private void StartFly(Vector3 from, int num)
        {
            var clip = MatchNum(num);
            for (var i = 0; i < clip; i++)
            {
                var trans = CreateFlyItem(from);
                CreateTween(trans, i);
            }

            Game.Instance.StartCoroutineGlobal(WaitClose(clip));
        }

        /// <summary>
        /// 计算飞行图标数量
        /// </summary>
        /// <param name="num">获得的星星数量</param>
        /// <returns>需要创建的飞图标星星数量</returns>
        private int MatchNum(int num)
        {
            var rank = rankList.Count - 1;
            for (var i = 0; i < rankList.Count; ++i)
            {
                if (num > rankList[i]) continue;
                return i >= clipNum.Count ? clipNum.FindLast(t => t > 0) : clipNum[i];
            }

            return clipNum.FindLast(t => t > 0);
        }

        /// <summary>
        /// 创建飞图标gameObject
        /// </summary>
        /// <param name="from">起始点</param>
        /// <returns>物体Transform</returns>
        private Transform CreateFlyItem(Vector3 from)
        {
            var trans = GameObjectPoolManager.Instance.CreateObject(UIFlyManager.ItemType,
                UIManager.Instance.GetLayerRootByType(UILayer.Effect)).transform as RectTransform;
            trans.GetComponent<UIImageRes>().SetImage(star);
            trans.sizeDelta = Vector2.one * size;
            trans.localScale = Vector3.zero;
            trans.SetPositionAndRotation(from, Quaternion.Euler(new Vector3(0, 0, Random.Range(0, 360))));
            trans.Find("Num").gameObject.SetActive(false);
            trans.Find("Num1").gameObject.SetActive(false);
            return trans;
        }

        /// <summary>
        /// 创建动画序列
        /// </summary>
        /// <param name="trans">动画对象</param>
        private void CreateTween(Transform trans, int idx)
        {
            var seq = DOTween.Sequence();
            seq.Pause();
            UIFlyFactory.CreateScatterTween(seq, trans, width, height, minWidth, minHeight);
            UIFlyFactory.CreateStopTween(seq, trans,
                UIFlyConfig.Instance.durationStop + stopTime + idx * UIFlyConfig.Instance.durationAdd);
            UIFlyFactory.CreateStraightTween(seq, trans, _album.position);
            seq.OnComplete(() => GameObjectPoolManager.Instance.ReleaseObject(UIFlyManager.ItemType, trans.gameObject));
            seq.Play();
        }

        /// <summary>
        /// 关闭界面回调
        /// </summary>
        /// <param name="num">飞图标总量</param>
        /// <returns></returns>
        private IEnumerator WaitClose(int num)
        {
            yield return new WaitForSeconds(stopTime + UIFlyConfig.Instance.durationStop * 2 + 1.8f);
            _callBack?.Invoke();
            yield return new WaitForSeconds(delay);
            Close();
        }
    }
}