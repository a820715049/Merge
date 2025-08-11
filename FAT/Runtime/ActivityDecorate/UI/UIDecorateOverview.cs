using EL;
using UnityEngine;

namespace FAT
{
    public class UIDecorateOverview : UIBase
    {
        public int areaId;

        protected override void OnCreate()
        {
            // transform.AddButton("Mask", OnClose).FixPivot();
            transform.AddButton("Content/Panel/close", OnClose).FixPivot();
        }

        private void OnClose()
        {
            Close();
            Game.Manager.mapSceneMan.ExitOverview();
            GameProcedure.MergeToSceneArea(Game.Manager.decorateMan.Activity.CurArea);
            UIManager.Instance.OpenWindow(Game.Manager.decorateMan.Panel);
        }
    }
}