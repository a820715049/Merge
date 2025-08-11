/*
 * @Author: tang.yan
 * @Description: 背包界面 
 * @Date: 2023-11-01 10:11:09
 */

using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using EL;
using fat.rawdata;

namespace FAT
{
    public class UIBag : UIBase
    {
        public float scale;
        public float duration;

        [SerializeField] private GameObject tabGo;
        [SerializeField] private GameObject tabEmptyGo;
        [SerializeField] private List<UISimpleToggle> tabToggleList;
        [SerializeField] private UIBagGirdGroupScrollRect girdGroupRect;
        [SerializeField] private GameObject ProducerRedPoint;

        private int _curSelectTabIndex = 0;
        private int _curShowBagId = 1;
        private List<List<BagMan.BagGirdData>> _gridGroupList = new List<List<BagMan.BagGirdData>>();

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/BtnClose/Btn", base.Close);
            _InitToggle();
            girdGroupRect.InitLayout();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                int type = (int)(BagMan.BagType)items[0];
                _curSelectTabIndex = type - 1;
                _curShowBagId = _curSelectTabIndex + 1;
            }
        }

        protected override void OnPreOpen()
        {
            _RefreshTabToggle();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_BAG_ITEM_INFO_CHANGE>().AddListener(_RefreshUI);
        }

        protected override void OnRefresh()
        {
            _RefreshTabToggle();
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_BAG_ITEM_INFO_CHANGE>().RemoveListener(_RefreshUI);
        }

        protected override void OnPostClose()
        {
            BoardViewManager.Instance.OnInventoryClose();
        }

        private void _InitToggle()
        {
            //注册点击事件
            int tempIndex = 0;
            foreach (var toggle in tabToggleList)
            {
                int index = tempIndex;
                toggle.onValueChanged.AddListener(isSelect => _OnToggleSelect(index, isSelect));
                tempIndex++;
            }
        }

        private void _OnToggleSelect(int index, bool isSelect)
        {
            if (isSelect && _curSelectTabIndex != index)
            {
                //如果当前在主场景 则只能查看工具背包
                if (Game.Manager.mapSceneMan.scene.Active)
                {
                    tabToggleList[_curSelectTabIndex].SetIsOnWithoutNotify(true);
                    var type = (BagMan.BagType)(index + 1);
                    if (type == BagMan.BagType.Item)
                    {
                        Game.Manager.commonTipsMan.ShowPopTips(Toast.UseItemBagInMeta);
                        return;
                    }
                    if (type == BagMan.BagType.Producer)
                    {
                        Game.Manager.commonTipsMan.ShowPopTips(Toast.UseProdBagInMeta);
                        return;
                    }
                }
                _curSelectTabIndex = index;
                _curShowBagId = _curSelectTabIndex + 1;
                tabToggleList[_curSelectTabIndex].transform.Find("Icon").GetComponent<Animator>().SetTrigger("Punch");
                var rect = transform.Find("Content/Root/Panel/Root/ScrollRect/Content");
                rect.localScale = new Vector3(scale, scale, scale);
                rect.DOScale(Vector3.one, duration);
                _RefreshUI();
                if ((BagMan.BagType)(index + 1) == BagMan.BagType.Producer)
                {
                    Game.Manager.mainMergeMan.world.inventory.GetBagByType(BagMan.BagType.Producer).TryClearRedPoint();
                }
            }
        }

        private void _RefreshTabToggle()
        {
            var bagMan = Game.Manager.bagMan;
            int tempIndex = 1;
            int showTabCount = 0;
            foreach (var toggle in tabToggleList)
            {
                bool temp = bagMan.CheckBagUnlock((BagMan.BagType)tempIndex);
                if (temp)
                    showTabCount++;
                toggle.gameObject.SetActive(temp);
                tempIndex++;
            }
            //要隐藏toggle的时候不切换页签
            if (showTabCount > 1)
                tabToggleList[_curSelectTabIndex].SetIsOnWithoutNotify(true);
            //显示的标签数小于等于1个的时候 整个toggle不显示
            tabGo.SetActive(showTabCount > 1);
            tabEmptyGo.SetActive(showTabCount > 1);
            if (bagMan.CheckBagUnlock(BagMan.BagType.Producer))
            {
                ProducerRedPoint.SetActive(Game.Manager.mainMergeMan.world.inventory.GetBagByType(BagMan.BagType.Producer).NeedRedPoint());
            }
            _RefreshUI();
        }

        private void _RefreshUI()
        {
            _gridGroupList.Clear();
            var gidDataList = Game.Manager.bagMan.GetBagGirdDataList(_curShowBagId);
            List<BagMan.BagGirdData> tempList = new List<BagMan.BagGirdData>();
            if (gidDataList.Count < 4)
            {
                _gridGroupList.Add(new List<BagMan.BagGirdData>(gidDataList));
            }
            else
            {
                foreach (var girdData in gidDataList)
                {
                    int girdIndex = girdData.GirdIndex;
                    //将数据四个为一组划分
                    if (girdIndex % 4 < 3)
                    {
                        tempList.Add(girdData);
                    }
                    else
                    {
                        tempList.Add(girdData);
                        _gridGroupList.Add(new List<BagMan.BagGirdData>(tempList));
                        tempList.Clear();
                    }
                }
                //添加最后的末尾部分
                if (tempList.Count > 0)
                {
                    _gridGroupList.Add(new List<BagMan.BagGirdData>(tempList));
                    tempList.Clear();
                }
            }
            //生成器最下面要有文字提示
            if (_curShowBagId == (int)BagMan.BagType.Producer)
            {
                _gridGroupList.Add(tempList);
            }
            girdGroupRect.UpdateData(_gridGroupList);
        }
    }

}