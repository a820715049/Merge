/*
 * @Author: tang.yan
 * @Description: 通用道具icon脚本 
 * @Date: 2024-03-27 18:03:05
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT {
    public class UICommonItem : MonoBehaviour {
        [SerializeField] private UIImageRes itemIcon;
        [SerializeField] private Button itemBtn;
        [SerializeField] private GameObject itemTips;
        [SerializeField] private TMP_Text itemCount;
        //当前正在展示的物品id
        private int _curShowItemId;
        //tips偏移值 默认为4
        private int _tipsOffset = 4;
        //新加字段 用于判断是否setup 避免外部忘记调用
        private bool _isSetup = false;
        
        //初始化OnCreate时或者从池中取出时调用
        public void Setup()
        {
            if (_isSetup) return;
            _isSetup = true;
            itemBtn.onClick.AddListener(_OnBtnClick);
        }

        //放入池中时调用
        public void Clear()
        {
            _curShowItemId = 0;
            _isSetup = false;
            itemBtn.onClick.RemoveAllListeners();
        }

        public void Refresh(int itemId, string count = "")
        {
            var valid = itemId > 0;
            gameObject.SetActive(valid);
            if (!valid) return;
            _Refresh(itemId, count);
        }
        
        //界面刷新时调用 可传入配置信息
        public void Refresh(Config.RewardConfig config)
        {
            var valid = config != null;
            gameObject.SetActive(valid);
            if (!valid) return;
            _Refresh(config.Id, config.Count.ToString());
        }

        //界面刷新时调用 可传入物品id和数量
        public void Refresh(int itemId, int count = 1)
        {
            var valid = itemId > 0;
            gameObject.SetActive(valid);
            if (!valid) return;
            _Refresh(itemId, count.ToString());
        }

        //countFontStyle 物品数量文本样式序号
        public void Refresh(Config.RewardConfig config, int countFontStyle)
        {
            var valid = config != null;
            gameObject.SetActive(valid);
            if (!valid) return;
            _Refresh(config.Id, config.Count.ToString(), countFontStyle);
        }

        public void ExtendTipsForMergeItem(int itemId)
        {
            if (itemTips == null || itemTips.activeSelf) return;
            var showTips = Game.Manager.objectMan.IsType(itemId, ObjConfigType.MergeItem);
            itemTips.SetActive(showTips);
            itemBtn.interactable = showTips;
        }

        public void SetGray(bool isGray)
        {
            if (!isGray)
            {
                GameUIUtility.SetDefaultShader(itemIcon.image);
            }
            else
            {
                GameUIUtility.SetGrayShader(itemIcon.image);
            }
        }

        private void _Refresh(int itemId, string countString = "", int countFontStyle = -1)
        {
            Setup();    //刷新时尝试走一遍Setup
            var countInt = countString.ConvertToInt();
            var conf = Game.Manager.objectMan.GetBasicConfig(itemId);
            if (conf == null) return;
            _curShowItemId = itemId;
            //刷新icon
            itemIcon.SetImage(conf.Icon);
            //刷新tips按钮
            bool showTips = UIItemUtility.ItemTipsInfoValid(itemId);
            itemTips.SetActive(showTips);
            //若刷新时image为空 则尝试直接获取一下
            if (itemIcon.image == null)
            {
                if (itemIcon.TryGetComponent<Image>(out var img))
                {
                    img.raycastTarget = showTips;
                }
            }
            else
            {
                itemIcon.image.raycastTarget = showTips;
            }
            if (itemBtn != null)
                itemBtn.interactable = showTips;
            if (itemCount != null)
            {
                //刷新数量
                itemCount.text = UIUtility.SpecialCountText(itemId, countInt, out var countStr) ? countStr : countString;
                //刷新数量文本样式
                var config = FontMaterialRes.Instance.GetFontMatResConf(countFontStyle);
                config?.ApplyFontMatResConfig(itemCount);
            }
        }
        
        private void _OnBtnClick()
        {
            if (_curShowItemId <= 0)
                return;
            var root = itemIcon.image.rectTransform;
            UIItemUtility.ShowItemTipsInfo(_curShowItemId, root.position, _tipsOffset + root.rect.size.y * 0.5f);
        }
    }
}