using EL;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBOrderItemBonus : MonoBehaviour
    {
        public UIImageState uIImageState;
        public MBBoardOrder mBBoard;
        public void SetReward(int reward)
        {
            reward--;
            if (reward > 2) reward = 2;
            uIImageState.Select(reward);
        }
    }
}