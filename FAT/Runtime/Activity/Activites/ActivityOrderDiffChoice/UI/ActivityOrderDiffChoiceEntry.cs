using System;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class ActivityOrderDiffChoiceEntry : MonoBehaviour
    {
        [SerializeField] private Image roloImage;
        [SerializeField] private Sprite[] roloSprites;
        [SerializeField] private UITextState[] uiTextStates;
        [SerializeField] private UIImageState[] uiImageStates;

        private void OnEnable()
        {
            if (Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderDiffChoice) is not ActivityOrderDiffChoice act) return;
            
            var dif = act.ChoiceType;
            foreach (var uiTextState in uiTextStates)
            {
                uiTextState.Setup((int)dif - 1);
            }
            foreach (var uiTextState in uiImageStates)
            {
                uiTextState.Setup((int)dif - 1);
            }
            roloImage.sprite = roloSprites[(int)dif - 1];
            roloImage.SetNativeSize();
        }
    }
}