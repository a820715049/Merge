// ==================================================
// File: MBFarmBoardFarm.cs
// Author: liyueran
// Date: 2025-05-09 17:05:51
// Desc: 农场主棋盘农田
// ==================================================

using EL;
using FAT.MSG;
using fat.rawdata;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class MBFarmBoardFarm : MonoBehaviour
    {
        [SerializeField] protected Animator animator;

        protected static readonly int Punch = Animator.StringToHash("Punch");

        // 活动实例
        protected FarmBoardActivity _activity;

        public virtual void SetUp()
        {
            transform.AddButton($"farmBg", OnClickFarm).WithClickScale().FixPivot();
        }

        public virtual void InitOnPreOpen(FarmBoardActivity act)
        {
            _activity = act;
        }


        public virtual void OnSeedClose()
        {
        }

        // 点击农田生成棋子
        public virtual void OnClickFarm()
        {
        }

        // 根据索引获得指定农田位置
        public virtual Transform GetFarmByIndex(int index)
        {
            return null;
        }
    }
    
}