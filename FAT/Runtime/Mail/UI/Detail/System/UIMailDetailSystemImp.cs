/*
 * @Author: qun.chao
 * @Date: 2021-07-20 19:35:47
 */
using UnityEngine;
using fat.gamekitdata;

namespace FAT
{
    public class UIMailDetailSystemImp : MonoBehaviour
    {
        [SerializeField] private GameObject goTitle;
        [SerializeField] private GameObject goDesc;
        [SerializeField] private UIImageRes preImg;
        [SerializeField] private UIImageRes postImg;

        public void SetData(Mail mail)
        {
            if (mail.IsRaw)
            {
                // title
                MBI18NText.SetPlainText(goTitle, mail.Title);
                // desc
                MBI18NText.SetPlainText(goDesc, mail.Content);
            }
            else
            {
                // title
                MBI18NText.SetKey(goTitle, mail.Title);
                // desc
                MBI18NText.SetKey(goDesc, mail.Content);
            }

            preImg.gameObject.SetActive(false);
            postImg.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(mail.ImageUrl))
            {
                if (mail.ImagePosition == MailImagePositionType.MailImagePositionTop)
                {
                    preImg.gameObject.SetActive(true);
                    preImg.SetUrl(mail.ImageUrl);
                }
                else if (mail.ImagePosition == MailImagePositionType.MailImagePositionBottom)
                {
                    postImg.gameObject.SetActive(true);
                    postImg.SetUrl(mail.ImageUrl);
                }
            }
        }

        public void ClearData()
        { }
    }
}