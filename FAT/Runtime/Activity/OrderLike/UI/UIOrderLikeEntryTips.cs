/*
 * @Author: qun.chao
 * @Date: 2025-03-28 18:08:31
 */
using UnityEngine;
using TMPro;

namespace FAT
{
    public class UIOrderLikeEntryTips : UITipsBase
    {
        [SerializeField] private float offsetY = 18;
        [SerializeField] private float width = 314;
        [SerializeField] private TextMeshProUGUI txtDesc;

        private string strDesc;
        private float lifeTime;

        protected override void OnParse(params object[] items)
        {
            _SetCurTipsWidth(width);
            // 设置tips位置参数
            _SetTipsPosInfo(items);
            if (items.Length > 2)
            {
                strDesc = items[2] as string;
            }
        }

        protected override void OnPreOpen()
        {
            // 刷新tips位置
            _RefreshTipsPos(offsetY);
            // 设置描述文本
            txtDesc.SetText(strDesc);
            lifeTime = 3f;
        }

        private void Update()
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                lifeTime = 100f;
                Close();
            }
        }
    }
}