using System;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class MBResHolderMiniBoard : MBResHolderBase
    {
        private void OnEnable()
        {
            if (!Game.Manager.miniBoardMultiMan.IsValid) return;
            GetComponent<Animator>().SetBool("open", Game.Manager.miniBoardMultiMan.CheckHasNextBoard() &&
                                                     Game.Manager.miniBoardMultiMan.CheckCanEnterNextRound());
        }

        public void SetOpenState(bool state)
        {
            GetComponent<Animator>().SetBool("open", state);
        }

        public void SetPunchState()
        {
            GetComponent<Animator>().SetTrigger("punch");
        }
    }
}