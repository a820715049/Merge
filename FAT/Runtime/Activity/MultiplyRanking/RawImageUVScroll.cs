/*
 * @Author: yanfuxing
 * @Date: 2025-07-31 10:00:00
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class RawImageUVScroll : MonoBehaviour
    {
        public RawImage rawImage;
        private Vector2 _scrollSpeed = new Vector2(0.02f, 0);
        //开放接口供动画自己调整滚动速度
        public Vector2 scrollSpeed1 = new Vector2(0.02f, 0);
        public Vector2 scrollSpeed2 = new Vector2(0.03f, 0);
        public Vector2 scrollSpeed3 = new Vector2(0.04f, 0);

        //档位点3
        private const  int SlotNumPoint = 3;
        //档位点4
        private const  int SlotNumPoint2 = 4;

        void Update()
        {
            var uv = rawImage.uvRect;
            uv.position += _scrollSpeed * Time.deltaTime;
            rawImage.uvRect = uv;
        }

        /// <summary>
        /// 根据档位设置滚动速度 1 2 3 4 5
        /// </summary>
        /// <param name="multiplierNum"></param>
        public void SetMultiplierNum(int multiplierNum)
        {
            if (multiplierNum < SlotNumPoint)
            {
                _scrollSpeed = scrollSpeed1;
            }
            else if (multiplierNum == SlotNumPoint || multiplierNum == SlotNumPoint2)
            {
                _scrollSpeed = scrollSpeed2;
            }
            else 
            {
                //其他档位都用3档位速度
                _scrollSpeed = scrollSpeed3;
            }
        }
    }
}