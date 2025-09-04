using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FAT.UI
{
    public class UIMicItem : MonoBehaviour
    {
        [SerializeField] private float animScale = 1;
        [SerializeField] private Transform goal;
        [SerializeField] private Transform prime;
        [SerializeField] private Transform done;
        [SerializeField] private Transform goalLock;
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private Transform bg3;
        [SerializeField] private Transform bg4;
        [SerializeField] private Transform up;
        [SerializeField] private Transform upV;
        [SerializeField] private Transform down;
        [SerializeField] private Transform downV;
        [SerializeField] private UITextState num;
        [SerializeField] private UICommonItem item;
        [SerializeField] private Transform complete;
        [SerializeField] private Transform sweep;

        public void UpdateContent(ActivityScore.Node node)
        {
            item.Refresh(node.reward, node.isCur ? 17 : 50);
            num.text.text = node.showNum.ToString();
            num.Select(node.isCur ? 1 : 0);
            goal.gameObject.SetActive(node.isGoal);
            goalLock.gameObject.SetActive(node.isGoal);
            prime.gameObject.SetActive(node.isPrime);
            done.gameObject.SetActive(node.isDone || node.isCur);
            bg1.gameObject.SetActive(!node.isCur && !node.isPrime && (node.isDone || node.isGoal));
            bg2.gameObject.SetActive(node.isCur && !node.isPrime);
            bg3.gameObject.SetActive(!node.isCur && node.isPrime);
            bg4.gameObject.SetActive(node.isCur && node.isPrime);
            sweep.gameObject.SetActive((node.isCur && node.isPrime) || (!node.isCur && node.isPrime));
            up.gameObject.SetActive(true);
            upV.gameObject.SetActive(node.isDone && !node.isCur && !node.isGoal);
            down.gameObject.SetActive(node.showNum > 1);
            downV.gameObject.SetActive(node.isCur || node.isDone);
            complete.gameObject.SetActive(node.isComplete && !node.isCur && !node.isGoal);
            item.gameObject.SetActive(!(node.isComplete && !node.isCur && !node.isGoal));
        }
    
        public IEnumerator ProcessToNext()
        {
            RectTransform rect = upV.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(-99, 0);
            var t = rect.DOSizeDelta(Vector2.zero, 0.99f * animScale).SetLink(gameObject).SetEase(Ease.Linear);
            yield return t.WaitForCompletion();
        }
    
        public IEnumerator ProcessToThis()
        {
            RectTransform rect = downV.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(80, 0);
            rect.sizeDelta = new Vector2(-80, 0);
            
            rect.DOAnchorPos(Vector2.zero, 0.8f * animScale).SetLink(gameObject).SetEase(Ease.Linear);
            var t = rect.DOSizeDelta(Vector2.zero, 0.8f * animScale).SetLink(gameObject).SetEase(Ease.Linear);
            yield return t.WaitForCompletion();
        }
    }
}