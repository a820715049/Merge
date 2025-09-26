using Cysharp.Text;
using EL;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBSevenDayLabel : MonoBehaviour
    {
        public int index;
        public TextMeshProUGUI textUnchoose;
        public TextMeshProUGUI textChosen;
        public GameObject lockUnchoose;
        public GameObject lockChosen;
        public Animation animation;
        public GameObject unChosen;
        public GameObject chosen;
        public GameObject completeChosen;
        public GameObject completeUnchosen;

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (Application.isPlaying) return;
            textUnchoose = transform.Find("Unchose/DayText").GetComponent<TextMeshProUGUI>();
            textChosen = transform.Find("Choosen/DayText").GetComponent<TextMeshProUGUI>();
            lockUnchoose = transform.Find("Unchose/Lock").gameObject;
            lockChosen = transform.Find("Choosen/Lock").gameObject;
        }
#endif

        public void UnChoen()
        {
            unChosen.gameObject.SetActive(true);
            chosen.gameObject.SetActive(false);
        }

        public void Choose()
        {
            chosen.gameObject.SetActive(true);
            animation.Play();
        }

        public void SetText()
        {
            textUnchoose.text = ZString.Format(I18N.Text("#SysComDesc1704"), index + 1);
            textChosen.text = ZString.Format(I18N.Text("#SysComDesc1704"), index + 1);
        }

        public void SetLock(bool unlock_)
        {
            lockUnchoose.SetActive(!unlock_);
            lockChosen.SetActive(!unlock_);
        }

        public void SetComplete(bool complete_)
        {
            completeChosen.SetActive(complete_);
            completeUnchosen.SetActive(complete_);
        }

        public void SetRedPoint(int num, bool unlock_)
        {
            if (num == 0 || !unlock_)
            {
                transform.Find("Unchose/Red").gameObject.SetActive(false);
                transform.Find("Choosen/Red").gameObject.SetActive(false);
                return;
            }
            else
            {
                transform.Find("Unchose/Red").gameObject.SetActive(true);
                transform.Find("Choosen/Red").gameObject.SetActive(true);
                transform.Find("Unchose/Red/Text").GetComponent<TextMeshProUGUI>().text = num.ToString();
                transform.Find("Choosen/Red/Text").GetComponent<TextMeshProUGUI>().text = num.ToString();
            }
        }
    }
}