/*
 * @Author: qun.chao
 * @Date: 2025-03-05 17:11:22
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using FAT.Merge;

namespace FAT
{
    public class MBBingoSpawner : MonoBehaviour
    {
        [SerializeField] private Transform itemRoot;

        private UIBingoMain uiMain;
        private ActivityBingo actInst => uiMain.ActInst;

        public void InitOnPreOpen(UIBingoMain main)
        {
            uiMain = main;
        }
        public void CleanupOnPostClose()
        {
            uiMain = null;
        }

        public void Refresh()
        {
            var conf = ItemBingoUtility.GetBoardConfig(actInst.ConfBoardID);
            var categoryIds = conf.ConnectSpawner;
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var child = itemRoot.GetChild(i);
                if (i < categoryIds.Count)
                {
                    child.gameObject.SetActive(true);
                    RefreshItem(child, categoryIds[i]);
                    RefreshItemAnim(child, categoryIds[i], true);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshItem(Transform trans, int catId)
        {
            var itemId = ItemBingoUtility.GetHighestLevelItemIdInCategory(catId);
            if (itemId <= 0)
            {
                trans.gameObject.SetActive(false);
                return;
            }
            trans.gameObject.SetActive(true);
            var cfg = Env.Instance.GetItemConfig(itemId);
            trans.Access<UIImageRes>("Root/Icon").SetImage(cfg.Icon);
            var hasItemInBoard = ItemBingoUtility.HasActiveItemInMainBoard(itemId);
            // 对勾
            trans.Find("Root/Check").gameObject.SetActive(hasItemInBoard);
            // 按钮
            var btn = trans.Access<Button>("Root/Btn");
            // 物品已经在场上则不需要有取出按钮
            btn.gameObject.SetActive(!hasItemInBoard);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                actInst.TryTakeOutItem(catId);
                RefreshItem(trans, catId);
                RefreshItemAnim(trans, catId, false);
            });
        }

        private void RefreshItemAnim(Transform trans, int catId, bool skip)
        {
            var itemId = ItemBingoUtility.GetHighestLevelItemIdInCategory(catId);
            var hasItemInBoard = ItemBingoUtility.HasActiveItemInMainBoard(itemId);
            // skip情况下直接切换到最终状态
            // 非skip时需要播放切换
            var trigger = skip ?
                        (hasItemInBoard ? "Normal" : "Highlight") :
                        (hasItemInBoard ? "Hide" : "Show");
            trans.Access<Animator>().SetTrigger(trigger);
        }
    }
}