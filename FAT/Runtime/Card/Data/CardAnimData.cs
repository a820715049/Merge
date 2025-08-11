using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace FAT
{
    //开卡动画数据类
    public class CardAnimData
    {
        public Transform Target;
        public float DelayTime;
        public GameObject Card;

        public void SetTarget(Transform target)
        {
            Target = target;
        }

        public void SetDelayTime(float time)
        {
            DelayTime = time;
        }
        
        public void SetStart(Transform start)
        {
            Card.transform.position = start.position;
            Card.transform.localScale = start.localScale;
            Card.transform.eulerAngles = start.eulerAngles;
        }
    }
}