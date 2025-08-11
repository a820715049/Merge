/*
 * @Author: qun.chao
 * @Date: 2023-10-26 17:27:09
 */

using UnityEngine;

namespace FAT
{
    public class UIMergeBoardMainContext
    {
        public UIMergeBoardMain entry { get; private set; }
        public MBBoardItemDetail detailCtrl { get; private set; }
        public MBMergeCompBoardOrder orderCtrl { get; private set; }
        public MBMergeCompReward rewardCtrl { get; private set; }
        public MBBoardInventoryEntry inventoryCtrl { get; private set; }
        public MBBoardFly boardFlyCtrl { get; private set; }
        public MBBoardMisc boardMisc { get; private set; }
        public MBOrderBagTips orderBagTips { get; private set; }
        public MBBoardMoveRoot boardDragRoot { get; private set; }

        public void RegisterModuleEntry(UIMergeBoardMain inst)
        {
            entry = inst;
        }

        public void RegisterModuleItemDetail(Transform transform, string path)
        {
            detailCtrl = transform.Find(path).GetComponent<MBBoardItemDetail>();
        }

        public void RegisterModuleOrder(Transform transform, string path)
        {
            orderCtrl = transform.Find(path).GetComponent<MBMergeCompBoardOrder>();
        }

        public void RegisterModuleReward(Transform transform, string path)
        {
            rewardCtrl = transform.Find(path).GetComponent<MBMergeCompReward>();
        }

        public void RegisterModuleInventoryEntry(Transform transform, string path)
        {
            inventoryCtrl = transform.Find(path).GetComponent<MBBoardInventoryEntry>();
        }

        public void RegisterModuleBoardFly(Transform transform, string path)
        {
            boardFlyCtrl = transform.Find(path).GetComponent<MBBoardFly>();
        }

        public void RegisterModuleMisc(Transform transform, string path)
        {
            boardMisc = transform.Find(path).GetComponent<MBBoardMisc>();
        }

        public void RegisterOrderBagTips(Transform transform, string path)
        {
            orderBagTips = transform.Find(path).GetComponent<MBOrderBagTips>();
        }

        public void RegisterModuleDragRoot(Transform transform, string path)
        {
            boardDragRoot = transform.Find(path).GetComponent<MBBoardMoveRoot>();
        }

        public void Install()
        {
            entry.Setup();
            detailCtrl.Setup();
            orderCtrl.Setup();
            rewardCtrl.Setup();
            inventoryCtrl.Setup();
            boardFlyCtrl.Setup();
            boardMisc.Setup();
            orderBagTips.Setup();
            boardDragRoot.Setup();
        }

        public void InitOnPreOpen()
        {
            detailCtrl.InitOnPreOpen();
            orderCtrl.InitOnPreOpen();
            rewardCtrl.InitOnPreOpen();
            inventoryCtrl.InitOnPreOpen();
            boardFlyCtrl.InitOnPreOpen();
            boardMisc.InitOnPreOpen();
            orderBagTips.InitOnPreOpen();
            boardDragRoot.InitOnPreOpen();
        }

        public void CleanupOnPostClose()
        {
            detailCtrl.CleanupOnPostClose();
            orderCtrl.CleanupOnPostClose();
            rewardCtrl.CleanupOnPostClose();
            inventoryCtrl.CleanupOnPostClose();
            boardFlyCtrl.CleanupOnPostClose();
            boardMisc.CleanupOnPostClose();
            orderBagTips.CleanupOnPostClose();
            boardDragRoot.CleanupOnPostClose();
        }
    }
}