/**
 * @Author: zhangpengjian
 * @Date: 2025/6/19 17:31:37
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/6/19 17:31:37
 * Description: 许愿棋盘云的表现
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EL;
using FAT.Merge;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class MBWishBoardCloudView : MonoBehaviour
    {
        [SerializeField] private UIImageRes _cover;
        [SerializeField] private UIImageRes _cover2;
        [SerializeField] private UIImageRes _cover3;
        [SerializeField] private Image _mask;
        [SerializeField] private Animator cloudAnim;
        private Cloud belongCloud;
        private (int, int) coord;
        private MBWishBoardCloudHolder _cloudHolder;

        public void Init(Cloud cloud, int x, int y)
        {
            var res = BoardViewManager.Instance.boardView.BoardRes;
            transform.AddButton("cover", OnClickCover);
            transform.AddButton("cover2", OnClickCover);
            transform.AddButton("cover3", OnClickCover);
            belongCloud = cloud;
            coord = (x, y);

            if (UIManager.Instance.TryGetUI(UIConfig.UIWishBoardMain) is UIWishBoardMain main)
            {
                _cloudHolder = main.CloudHolder;
            }

            // 基于坐标的伪随机算法选择云朵样式
            SetCloudStyle(x, y);

            RefreshMask();
        }

        /// <summary>
        /// 基于坐标的伪随机算法设置云朵样式
        /// 使用更好的分布策略来确保三种样式更分散
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        private void SetCloudStyle(int x, int y)
        {
            // 使用坐标的哈希值来确保一致性
            int hash = GetCoordinateHash(x, y);
            
            // 使用更大的质数来增加随机性
            // 选择互质的质数来避免周期性模式
            int pattern1 = hash % 17;  // 质数17
            int pattern2 = hash % 19;  // 质数19  
            int pattern3 = hash % 23;  // 质数23
            
            // 使用异或操作来增加随机性，避免简单的加法导致的偏向性
            int combined = pattern1 ^ pattern2 ^ pattern3;
            
            // 添加额外的混合步骤来进一步分散
            combined = combined ^ (combined >> 8);
            combined = combined ^ (combined >> 4);
            
            // 使用更大的模数来获得更好的分布
            int styleIndex = combined % 3;
            
            // 隐藏所有样式
            _cover.gameObject.SetActive(false);
            _cover2.gameObject.SetActive(false);
            _cover3.gameObject.SetActive(false);
            
            // 根据计算结果显示对应样式
            switch (styleIndex)
            {
                case 0:
                    _cover.gameObject.SetActive(true);
                    break;
                case 1:
                    _cover2.gameObject.SetActive(true);
                    break;
                case 2:
                    _cover3.gameObject.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// 获取坐标的哈希值，确保相同坐标总是返回相同的哈希值
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>哈希值</returns>
        private int GetCoordinateHash(int x, int y)
        {
            unchecked
            {
                int hash = 5381;
                hash = ((hash << 5) + hash) + x; // hash * 33 + x
                hash = ((hash << 5) + hash) + y; // hash * 33 + y
                hash = hash ^ (hash >> 16); // 额外的混合步骤
                return Mathf.Abs(hash);
            }
        }

        public void RefreshMask()
        {
            var act = Game.Manager.activity.LookupAny(EventType.WishBoard) as WishBoardActivity;
            if (act == null)
            {
                return;
            }

            var cloud = _cloudHolder.GetNextCloud();

            _mask.raycastTarget = belongCloud != cloud;
        }

        private void OnClickCover()
        {
            List<(int, int)> coords = Enumerable.ToList(belongCloud.CloudArea);
            foreach (var coord in coords)
            {
                _cloudHolder.GetCloudViewByCoord(coord.Item1, coord.Item2, out var view);
                view.PlayPunch();
            }

            var ui = UIManager.Instance.TryGetUI(UIConfig.UIWishBoardMain);
            if (ui != null && ui is UIWishBoardMain main)
            {
                main.LockView.PlayHint();
                Game.Manager.commonTipsMan.ShowPopTips(Toast.FarmCloudLocked, main.LockView.transform.position);
            }
        }

        // 暗->明表现
        public void PlayPunch()
        {
            cloudAnim.SetTrigger("Punch");
        }

        // 解锁表现
        public IEnumerator CoPlayUnlock()
        {
            cloudAnim.SetTrigger("Hide");
            yield return new WaitForSeconds(1.3f);
            _cloudHolder.ReleaseView(coord);
        }
    }
}