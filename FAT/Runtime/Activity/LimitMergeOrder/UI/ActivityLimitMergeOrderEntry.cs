
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class ActivityLimitMergeOrderEntry : MonoBehaviour
    {
        [SerializeField] private Button btn;

        private void Awake()
        {
            btn.onClick.AddListener(_OnBtnGoToActivity);
        }
        private void _OnBtnGoToActivity()
        {
            var act = (ActivityLimitMergeOrder)Game.Manager.activity.LookupAny(fat.rawdata.EventType.LimitMerge);
            act?.Open();
        }
    }
}
