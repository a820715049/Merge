/*
 * @Author: qun.chao
 * @Date: 2022-03-07 18:46:45
 */
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIGuideTalk : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI textTalk;
        [SerializeField] private RectTransform anchor;
        [SerializeField] private TextMeshProUGUI textBtn;
        [SerializeField] private GameObject button;
        [SerializeField] private GameObject block;
        [SerializeField] private GameObject talk;
        [SerializeField] private TextMeshProUGUI textTalkWithButton;
        [SerializeField] private TextMeshProUGUI textTalkNext;
        [SerializeField] private TextMeshProUGUI textTalkWithButtonNext;
        [SerializeField] private UIImageRes head;
        [SerializeField] private UIImageRes bg;
        [SerializeField] private Color textColor;

        public void Setup()
        {
        }

        public void InitOnPreOpen()
        {
            Hide();
        }

        public void CleanupOnPostClose()
        {
            Hide();
        }

        public void ShowByCoord(string content, Vector2 coord, bool next = false)
        {
            gameObject.SetActive(true);
            talk.SetActive(true);
            button.SetActive(false);
            block.SetActive(false);
            if (!next)
            {
                textTalk.gameObject.SetActive(true);
                textTalk.text = content;
                textTalkNext.gameObject.SetActive(false);
            }
            else
            {
                textTalk.gameObject.SetActive(false);
                textTalkNext.text = content;
                textTalkNext.gameObject.SetActive(true);
            }
            textTalkWithButton.gameObject.SetActive(false);
            textTalkWithButtonNext.gameObject.SetActive(false);

            Vector2 screenPos = BoardUtility.GetScreenPosByCoord(coord.x, coord.y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(anchor.parent as RectTransform, screenPos, null, out Vector2 localPos);
            anchor.anchoredPosition = localPos;

            Pop();
        }

        public void ShowByCoordWithButton(string content, Vector2 coord, string btntext, Action cb, bool next = false)
        {
            gameObject.SetActive(true);
            textTalk.gameObject.SetActive(false);
            textTalkNext.gameObject.SetActive(false);
            if (!next)
            {
                textTalkWithButton.gameObject.SetActive(true);
                textTalkWithButtonNext.gameObject.SetActive(false);
                textTalkWithButton.text = content;
            }
            else
            {
                textTalkWithButton.gameObject.SetActive(false);
                textTalkWithButtonNext.gameObject.SetActive(true);
                textTalkWithButtonNext.text = content;
            }
            block.SetActive(true);
            button.SetActive(true);
            textBtn.text = btntext;
            button.GetComponent<Button>().onClick.RemoveAllListeners();
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                cb?.Invoke();
                Hide();
            });
            ButtonExt.TryAddClickScale(button.GetComponent<Button>());

            Vector2 screenPos = BoardUtility.GetScreenPosByCoord(coord.x, coord.y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(anchor.parent as RectTransform, screenPos, null, out Vector2 localPos);
            anchor.anchoredPosition = localPos;

            Pop();
        }

        public void SetHeadImage(string head) => this.head.SetImage(head ?? "fat_global:i_s_lead.png");
        public void SetBgImage(string bg) => this.bg.SetImage(bg ?? "fat_global:p_s_lead1.png");

        public void SetTextColor(Color color)
        {
            if (color != Color.clear)
            {
                textTalkWithButton.color = color;
                textTalkWithButtonNext.color = color;
                textTalk.color = color;
                textTalkNext.color = color;
            }
            else
            {
                textTalkWithButton.color = new Color(textColor.r, textColor.g, textColor.b);
                textTalkWithButtonNext.color = new Color(textColor.r, textColor.g, textColor.b);
                textTalk.color = new Color(textColor.r, textColor.g, textColor.b);
                textTalkNext.color = new Color(textColor.r, textColor.g, textColor.b);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Pop()
        {
            var play = textTalk.transform.parent.GetComponent<uTools.PlayTween>();
            if (play != null)
            {
                play.Play();
            }
            Game.Manager.audioMan.TriggerSound("PopupDialog");
        }
    }
}