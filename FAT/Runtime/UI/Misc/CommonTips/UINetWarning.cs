/*
 * @Author: tang.yan
 * @Description: 弱网提示界面 无具体逻辑 只供外部开关显示即可
 * @Date: 2024-05-08 16:05:48
 */
using EL;

namespace FAT
{
    public class UINetWarning : UIBase
    {
        private int _quitTime;
        protected override void OnPreOpen()
        {
            _quitTime = Game.Manager.configMan.globalConfig.NoNetQuitTime;
        }
        
        protected override void OnPreClose()
        {
            _quitTime = 0;
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_OnOneSecondDriver);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_OnOneSecondDriver);
        }
        
        private void _OnOneSecondDriver()
        {
            var now = Game.Instance.GetTimestampSeconds();
            if (Game.Manager.networkMan.NetWeakStartTime + _quitTime <= now)
            {
                Game.Manager.networkMan.OnEnterNetworkWeak();
                Close();
            }
        }
    }
}