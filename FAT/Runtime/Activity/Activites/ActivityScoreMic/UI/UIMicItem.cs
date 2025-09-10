using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMicItem : MonoBehaviour
    {
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

        private ActivityScore.Node _node;
        
        public void UpdateContent(ActivityScore.Node node)
        {
            _node = node;
            UpdateContentByValue(node.isCur, node.isDone, node.isGoal);
        }
        
        public void UpdateContentByValue(bool isCur, bool isDone, bool isGoal)
        {
            item.Refresh(_node.reward, isCur ? 17 : 50);
            num.text.text = _node.showNum.ToString();
            num.Select(isCur ? 1 : 0);
            goal.gameObject.SetActive(isGoal);
            goalLock.gameObject.SetActive(isGoal);
            prime.gameObject.SetActive(_node.isPrime);
            done.gameObject.SetActive(isDone || isCur);
            bg1.gameObject.SetActive(!isCur && !_node.isPrime && (isGoal || isDone));
            bg2.gameObject.SetActive(isCur && !_node.isPrime);
            bg3.gameObject.SetActive(!isCur && _node.isPrime);
            bg4.gameObject.SetActive(isCur && _node.isPrime);
            sweep.gameObject.SetActive((isCur && _node.isPrime) || (!isCur && _node.isPrime));
            up.gameObject.SetActive(true);
            down.gameObject.SetActive(_node.showNum > 1);
            complete.gameObject.SetActive(_node.isComplete && !isCur && !isGoal);
            item.gameObject.SetActive(!(_node.isComplete && !isCur && !isGoal));
            
            RectTransform upRect = upV.GetComponent<RectTransform>();
            upRect.sizeDelta = new Vector2(isDone && !isCur && !isGoal ? 0 : -99, 0);
            RectTransform downRect = downV.GetComponent<RectTransform>();
            downRect.sizeDelta = new Vector2(isCur || isDone ? 0 : -80.5f, 0);
        }
    
        public IEnumerator ProcessToNext()
        {
            RectTransform rect = upV.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(-99, 0);
            rect.DOKill();
            var t = rect.DOSizeDelta(Vector2.zero, UIScore_mic.ToNextTime).SetLink(gameObject).SetEase(Ease.Linear);
            yield return t.WaitForCompletion();
            UpdateContentByValue(false, true, false);
        }
    
        public IEnumerator ProcessToThis()
        {
            RectTransform rect = downV.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(-80.5f, 0);
            rect.DOKill();
            var t = rect.DOSizeDelta(Vector2.zero, UIScore_mic.ToThisTime).SetLink(gameObject).SetEase(Ease.Linear);
            yield return t.WaitForCompletion();

            UpdateContentByValue(true, false, false);
        }
    }
}