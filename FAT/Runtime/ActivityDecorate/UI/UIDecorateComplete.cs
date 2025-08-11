using EL;
using UnityEngine;

namespace FAT
{
    public class UIDecorateComplete : UIBase
    {
        protected override void OnCreate() {
            transform.AddButton("Mask", OnClose).FixPivot();
            transform.AddButton("Content/Panel/confirm", OnClose).FixPivot();
        }

        protected override void OnPreOpen() {
            Game.Manager.decorateMan.Activity.SetCompleteState(false);
        }
        
        private void OnClose() {
            Close();
            Game.Manager.decorateMan.AfterComplete();
        }
    }
}