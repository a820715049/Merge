/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏-选关界面 cell
 * @Date: 2024-10-05 11:35:41
 */

using EL;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using TMPro;

namespace MiniGame
{
    public class UIBeadsSelectCell : FancyGridViewCell<(int, int), UICommonScrollGridDefaultContext>
    {
        //common
        [SerializeField] public UIImageState bgImageState;
        [SerializeField] public TMP_Text levelName;
        //unlock
        [SerializeField] public GameObject unlockGo;
        [SerializeField] public UIImageState levelIcon;
        [SerializeField] public GameObject redPointGo;
        [SerializeField] public GameObject finishGo;
        [SerializeField] public Button playBtn;
        [SerializeField] public UIImageState btnImageState;
        [SerializeField] public UITextState btnTextState;
        //lock
        [SerializeField] public GameObject lockGo;
        [SerializeField] public TMP_Text lockText;
        
        private int _curLevelIndex; //当前关卡index
        private int _curLevelId;    //当前关卡id

        public override void Initialize()
        {
            playBtn.WithClickScale().FixPivot().onClick.AddListener(_OnClickBtnPlay);
        }

        public override void UpdateContent((int, int) info)
        {
            _curLevelIndex = info.Item1;    //从0开始
            _curLevelId = info.Item2;
            if (_curLevelIndex < 0 || _curLevelId <= 0) return;
            var levelConf = MiniGameConfig.Instance.GetBeadsLevelConf(_curLevelId);
            if (levelConf == null) return;
            //是否等级解锁
            var isUnlock = MiniGameManager.Instance.IsLevelUnlock(MiniGameType.MiniGameBeads, _curLevelIndex);
            //前置关卡是否通关
            var preLevelIndex = _curLevelIndex - 1;
            var isPreComplete = preLevelIndex < 0 || MiniGameManager.Instance.IsLevelComplete(MiniGameType.MiniGameBeads, preLevelIndex);
            //当前关卡是否通关
            var isComplete = MiniGameManager.Instance.IsLevelComplete(MiniGameType.MiniGameBeads, _curLevelIndex);
            //当前关卡是否有红点
            var hasRP = MiniGameManager.Instance.CheckLevelHasRP(MiniGameType.MiniGameBeads, _curLevelIndex);
            levelName.text = I18N.FormatText("#SysComDesc606", _curLevelIndex + 1);
            var isRealUnlock = isUnlock && isPreComplete;
            bgImageState.Enabled(isRealUnlock);
            unlockGo.SetActive(isRealUnlock);
            levelIcon.Enabled(!isComplete);
            redPointGo.SetActive(hasRP);
            finishGo.SetActive(isComplete);
            btnImageState.Enabled(!isComplete);
            btnTextState.Enabled(!isComplete);
            lockGo.SetActive(!isRealUnlock);
            if (!isRealUnlock)
            {
                //关卡没解锁有两种情况：1、等级不够 2、等级够了但前置关卡没通关
                var str = !isUnlock
                    ? I18N.FormatText("#SysComDesc616", levelConf.ActiveLv)
                    : I18N.FormatText("#SysComDesc610", preLevelIndex + 1);
                lockText.text = str;
            }
        }

        private void _OnClickBtnPlay()
        {
            if (_curLevelIndex < 0)
                return;
            MiniGameManager.Instance.TryStartMiniGame(MiniGameType.MiniGameBeads, _curLevelIndex);
            MiniGameManager.Instance.ClearLevelRP(MiniGameType.MiniGameBeads);
        }
    }
}