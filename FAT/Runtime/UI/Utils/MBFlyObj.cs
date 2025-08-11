/*
*@Author:chaoran.zhang
*@Desc:
*@Created Time:2024.06.06 星期四 19:54:39
*/
using UnityEngine;

namespace FAT.Runtime.UI.Utils
{
    public class MBFlyObj : MonoBehaviour
    {
        public FlyTypeEnum flyType = FlyType.None;

        private void OnEnable()
        {
            UIFlyFactory.RegisterFlyObj(flyType, gameObject);
        }

        private void OnDisable()
        {
            UIFlyFactory.UnRegisterFlyObj(flyType, gameObject);
        }
        
    }
}