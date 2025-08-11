using System.Collections;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBOrderRateReward : MonoBehaviour
    {
        private int _id;
        private UIImageRes _icon;
        private TextMeshProUGUI _count;
        private Animator _animator;
        private int _orderID;

        private void Awake()
        {
            transform.Access("Icon", out _icon);
            transform.Access("Count", out _count);
            _animator = GetComponent<Animator>();
        }

        public void Show()
        {
            _animator.SetTrigger("Show");
        }
        public void Hide()
        {
            _animator.SetTrigger("Hide");
        }

        public void SetIdle(bool idle)
        {
            _animator.SetBool("Punch", idle);
        }
        public void SetReward(int id, int count, bool isOrder = false, int orderID = 0)
        {
            _id = id;
            _orderID = orderID;
            var cfg = Game.Manager.configMan.GetEventOrderRateBoxConfig(id);
            _icon.SetImage(isOrder ? cfg.Image : cfg.OrderInfo);
            if (!isOrder)
            {
                _count.text = count.ToString();
                transform.Find("Icon").GetComponent<Button>().onClick.RemoveAllListeners();
                transform.AddButton("Icon", Click);
            }
            else
            {
                transform.Find("Icon/ClickArea").GetComponent<Button>().onClick.RemoveAllListeners();
                transform.AddButton("Icon/ClickArea", ClickOrder);
            }
        }

        public void ClearReward()
        {
            _id = 0;
            IEnumerator enumerator()
            {
                yield return new WaitForSeconds(1f);
                _icon.Clear();
            }
            Game.Instance.StartCoroutineGlobal(enumerator());
        }
        private void Click()
        {
            UIManager.Instance.OpenWindow(UIConfig.OrderRateTip, transform.position, 50f, _id);
        }
        private void ClickOrder()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIOrderRateInfo, _id, _orderID);
        }
    }
}