using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    /// <summary>
    /// 矿车背景管理组件，负责背景图的切换和归位
    /// 上层调用只需要SetPosition即可，图片会自动移形换位
    /// </summary>
    public class MineCartBackground : MonoBehaviour
    {
        [Header("背景图设置")]
        [SerializeField] private List<RectTransform> backgroundImages = new List<RectTransform>(); // 背景图列表
        [SerializeField] private float bgWidth = 1080f;           // 单张背景图的宽度
        [SerializeField] private float moveMultiplier = 1f;       // 移动倍率，传入位移量乘以此值得到真实位移

        //这个参数的目的是适配：假如图片的宽是1080，如果图片一移动到屏幕外就移动到最后，在宽度>1080的手机上就穿帮了
        //所以增加宽容度，让图片多运动一定百分比，例如1080宽度的图片，配置45%，则可以最大支持到1566宽度的手机.
        [Range(0f, 0.49f)]
        [SerializeField] private float tolerance = 0.45f; // 宽容度
        private List<Vector2> backgroundPositions;  // 背景图位置列表
        private float totalOffset = 0f;             // 总偏移量

        private void Awake()
        {
            InitializeBackgrounds();
        }

        /// <summary>
        /// 初始化背景图
        /// </summary>
        private void InitializeBackgrounds()
        {
            backgroundPositions = new List<Vector2>();

            // 初始化背景位置
            if (backgroundImages.Count > 0)
            {
                // 设置初始背景位置 - 简单的从左到右排列
                for (int i = 0; i < backgroundImages.Count; i++)
                {
                    Vector2 pos = Vector3.zero;
                    pos.x += i * bgWidth; // 每张背景图间隔bgWidth距离
                    backgroundPositions.Add(pos);
                    backgroundImages[i].anchoredPosition = pos;
                }
            }
        }

        /// <summary>
        /// 设置背景位置
        /// </summary>
        /// <param name="position">目标位置</param>
        public void SetPosition(float frameOffset)
        {
            // 累积总偏移量
            frameOffset *= moveMultiplier;
            totalOffset += frameOffset; // 现在传入的已经是正确方向的偏移量



            // 更新所有背景图位置
            for (int i = 0; i < backgroundImages.Count; i++)
            {
                Vector2 pos = Vector3.zero;
                pos.x += (i * bgWidth) + totalOffset;
                backgroundPositions[i] = pos;

                if (backgroundImages[i] != null)
                {
                    backgroundImages[i].anchoredPosition = pos;
                }
            }

            // 检查并执行背景图循环
            CheckAndWrapBackgrounds();
        }

        /// <summary>
        /// 检查并尝试替换背景图位置
        /// </summary>
        private void CheckAndWrapBackgrounds()
        {
            if (backgroundImages.Count == 0) return;

            // 计算背景图的总宽度
            float totalWidth = backgroundImages.Count * bgWidth;
            bool hasWrapped = false;

            // 计算实际的宽容距离
            float toleranceDistance = bgWidth * tolerance;

            // 检查每张背景图
            for (int i = 0; i < backgroundImages.Count; i++)
            {
                Vector2 pos = backgroundPositions[i];

                // 如果背景图移出屏幕左侧超过宽容距离，将其移动到右侧
                if (pos.x < -bgWidth - toleranceDistance)
                {
                    pos.x += totalWidth;
                    backgroundPositions[i] = pos;
                    hasWrapped = true;

                    if (backgroundImages[i] != null)
                    {
                        backgroundImages[i].anchoredPosition = pos;
                    }
                }
                // 如果背景图移出屏幕右侧超过宽容距离，将其移动到左侧
                else if (pos.x > totalWidth + toleranceDistance)
                {
                    pos.x -= totalWidth;
                    backgroundPositions[i] = pos;
                    hasWrapped = true;

                    if (backgroundImages[i] != null)
                    {
                        backgroundImages[i].anchoredPosition = pos;
                    }
                }
            }

            // 如果发生了包装，调整totalOffset以保持状态一致
            if (hasWrapped)
            {
                // 当背景完成一次循环时，调整totalOffset
                // 根据移动方向（通常向左移动为负值），调整offset

                // 计算循环调整值
                if (totalOffset < -totalWidth)
                {
                    totalOffset += totalWidth;
                }
                else if (totalOffset > totalWidth)
                {
                    totalOffset -= totalWidth;
                }
            }
        }
    }
}

