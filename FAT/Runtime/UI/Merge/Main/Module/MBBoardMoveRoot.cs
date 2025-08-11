/*
 * @Author: qun.chao
 * @Date: 2024-12-23 18:03:37
 */
using UnityEngine;

namespace FAT
{
    public class MBBoardMoveRoot : MonoBehaviour
    {
        public void Setup()
        {
        }

        public void InitOnPreOpen()
        {
            BoardViewManager.Instance.SetMoveRootOverride(transform as RectTransform);
        }

        public void CleanupOnPostClose()
        {
            BoardViewManager.Instance.SetMoveRootOverride(null);
        }
    }
}