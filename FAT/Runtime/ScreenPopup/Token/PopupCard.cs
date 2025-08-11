/*
 * @Author: tang.yan
 * @Description:  集卡活动-活动相关-弹脸逻辑
 * @Date: 2024-01-16 10:01:06
 */
using fat.rawdata;

namespace FAT {
    public class PopupCard : PopupActivity {
        private long startTs; //允许弹脸的起始时间
        private long endTs;   //允许弹脸的结束时间

        public void Setup(ActivityLike acti_, ActivityVisual visual_, long startTs_ = 0, long endTs_ = 0, UIResAlt ui_ = null) {
            Setup(acti_, visual_, ui_, false);
            startTs = startTs_;
            endTs = endTs_;
        }

        public override bool OpenPopup()
        {
            if (!PopupValid)
                return false;
            //检查是否在允许弹的时间范围内
            var t = Game.Instance.GetTimestampSeconds();
            if (t >= startTs && t < endTs)
            {
                //打开对应窗口
                UIManager.Instance.OpenWindow(PopupRes);
                return true;
            }
            return false;
        }
    }
}