/*
 * @Author: qun.chao
 * @Date: 2021-07-23 17:53:24
 */
using UnityEngine;

namespace FAT
{
    public class MBFlyTarget : MonoBehaviour
    {
        public FlyTypeEnum flyType = FlyType.None;

        private void OnEnable()
        {
            UIFlyFactory.RegisterFlyTarget(flyType, _GetPos);
        }

        private void OnDisable()
        {
            UIFlyFactory.UnregisterFlyTarget(flyType, _GetPos);
        }

        private Vector3 _GetPos()
        {
            return transform.position;
        }
    }
}