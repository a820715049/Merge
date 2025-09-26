using System.Xml.Serialization;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UISevenDayTaskEntry : MonoBehaviour, IActivityBoardEntry
    {
        public TextMeshProUGUI cd;
        public ActivitySevenDayTask activity;
        public TextMeshProUGUI count;
        public GameObject red;
        public int lastNum = 0;

        public void Start()
        {
            transform.AddButton("Root/Bg", () => activity?.Open());
        }

        public void RefreshEntry(ActivityLike activity)
        {
            this.activity = activity as ActivitySevenDayTask;
            WhenUpdate();
        }

        public void OnEnable()
        {
            MessageCenter.Get<MSG.SEVEN_DAY_TASK_UPDATE>().AddListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            lastNum = 0;
            WhenUpdate();
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.SEVEN_DAY_TASK_UPDATE>().RemoveListener(WhenUpdate);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            lastNum = 0;
        }

        public void WhenUpdate()
        {
            var num = activity?.GetCanCompleteTaskCount() ?? 0;
            if (num > 0 && lastNum != num)
            {
                transform.GetComponent<Animator>().SetTrigger("Punch");
                MessageCenter.Get<MSG.BOARD_ORDER_SCROLL_SETTARGET>().Dispatch(this.transform);
            }
            else if (num == 0) { transform.GetComponent<Animator>().Play("UISevenDayTaskEntry_Default"); }
            red.SetActive(num > 0);
            count.text = num.ToString();
            lastNum = num;
        }

        public void RefreshCD()
        {
            cd.text = UIUtility.CountDownFormat(activity?.Countdown ?? 0);
            WhenUpdate();
        }
    }
}