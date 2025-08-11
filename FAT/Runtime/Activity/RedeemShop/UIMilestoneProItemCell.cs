/*
 * @Author: yanfuxing
 * @Date: 2025-05-16 16:03:05
 */
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIMilestoneProItemCell : MonoBehaviour
    {
        private UIImageRes _itemIcon;
        private TextMeshProUGUI _itemCount;
        private Transform _finishObj;


        private void Awake()
        {
            _itemIcon = transform.Access<UIImageRes>("Image");
            _itemCount = transform.Access<TextMeshProUGUI>("CountText");
            _finishObj = transform.Find("Finish");
        }

        public void SetData(string imageName, int count)
        {
            _itemCount.text = count.ToString();
            _itemIcon.SetImage(imageName);
        }

        public void SetFinish(bool isFinish)
        {
            if (_finishObj == null) return;
            _finishObj.gameObject.SetActive(isFinish);
            _itemIcon.gameObject.SetActive(!isFinish);
            _itemCount.gameObject.SetActive(!isFinish);
        }
    }
}

