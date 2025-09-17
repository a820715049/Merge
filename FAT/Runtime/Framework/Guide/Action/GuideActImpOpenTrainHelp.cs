using fat.rawdata;

namespace FAT
{
    public class GuideActImpOpenTrainHelp : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            var train = (TrainMissionActivity)Game.Manager.activity.LookupAny(EventType.TrainMission);
            var mainConfig = train.VisualMain.res.ActiveR ?? UIConfig.UITrainMissionMain;
            var ui = UIManager.Instance.TryGetUI(mainConfig);
            if (ui != null && ui is UITrainMissionMain main)
            {
                UIManager.Instance.OpenWindow(train.VisualHelp.res.ActiveR, train);
            }
            
            mIsWaiting = false;
        }
    }
}
