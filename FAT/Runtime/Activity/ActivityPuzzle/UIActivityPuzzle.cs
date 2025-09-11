/**
 * @Author: zhangpengjian
 * @Date: 2025/8/7 17:40:19
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/8/7 17:40:19
 * Description: 拼图活动
 */

using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityPuzzle : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnPlay;
        [SerializeField] private Button btnGo;
        [SerializeField] private Button[] btnList;
        [SerializeField] private Button[] btnList2;
        [SerializeField] private UIImageRes[] imgList;
        [SerializeField] private UIImageRes[] imgList2;
        [SerializeField] private Transform[] imgListRoot;
        [SerializeField] private UIImageRes puzzleBg;
        [SerializeField] private UIImageRes puzzleBg2;
        [SerializeField] private TextMeshProUGUI tokenNum;
        [SerializeField] private TextMeshProUGUI milestoneNum;
        [SerializeField] private TextMeshProUGUI btnText;
        [SerializeField] private Animator milestoneNumAnim;
        [SerializeField] private Animator tipAnim;
        [SerializeField] private Animator nextRoundAnim;
        [SerializeField] private Animator bigRewardAnim;
        [SerializeField] private TextMeshProUGUI bigRewardNum;
        [SerializeField] private UIImageRes bigRewardIcon;
        [SerializeField] private Button bigRewardBtn;
        [SerializeField] private Transform bigRewardFinish;
        [SerializeField] private TextMeshProUGUI cd;
        [SerializeField] private GameObject milestoneObj;
        [SerializeField] private GameObject block;
        [SerializeField] private GameObject efx;
        [SerializeField] private Transform milestoneRoot;
        [SerializeField] private MBRewardProgress progress;
        [SerializeField] private float efxDuration;

        private ActivityPuzzle _activity;
        private List<GameObject> _milestoneCellList = new();
        private List<int> _availablePositions = new();
        
        // 添加停留5秒随机显示拼图相关变量
        private float _stayTime = 0f;
        private const float RANDOM_PUZZLE_DELAY = 6f;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(OnClickClose);
            btnPlay.onClick.AddListener(OnClickPlay);
            bigRewardBtn.onClick.AddListener(OnClickBigReward);
            btnGo.onClick.AddListener(OnClickGo);
            for (int i = 0; i < btnList.Length; i++)
            {
                int index = i;
                btnList[i].onClick.AddListener(() => OnClickBtn(index));
            }
            for (int i = 0; i < btnList2.Length; i++)
            {
                int index = i;
                btnList2[i].onClick.AddListener(() => OnClickBtn(index));
            }
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.PUZZLE_MILESTONE_CELL, milestoneObj);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as ActivityPuzzle;
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(OnOneSecondDriver);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().AddListener(OnPostCommitReward);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().RemoveListener(OnPostCommitReward);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnOneSecondDriver);
        }

        private void OnPostCommitReward(RewardCommitData data)
        {
            RefreshToken();
        }

        private void OnOneSecondDriver()
        {
            if (_activity == null) return;
            var t = Game.Instance.GetTimestampSeconds();
            var diff = (long)Mathf.Max(0, _activity.endTS - t);
            cd.SetCountDown(diff);
            if (UIManager.Instance.IsOpen(UIConfig.UIGuide)) return;
            // 停留时间计时，每5秒随机显示一张拼图
            _stayTime += 1f;
            if (_stayTime >= RANDOM_PUZZLE_DELAY)
            {
                ShowRandomPuzzle();
                _stayTime = 0f; // 重置计时器，准备下一次显示
            }
        }

        protected override void OnPreOpen()
        {
            btnText.text = I18N.Text("#SysComDesc1329");
            nextRoundAnim.SetTrigger("Before");
            efx.SetActive(false);
            for (int i = 0; i < imgListRoot.Length; i++)
            {
                imgListRoot[i].gameObject.SetActive(false);
            }
            RefreshPuzzles();
            RefreshMilestone();
            RefreshBigReward();
            RefreshToken();
            OnOneSecondDriver();
            
            // 决定是否显示 Go 按钮
            bool shouldShowGo;
            if (_activity.conf.IsCycle)
            {
                // 循环模式：只要完成当前轮就显示 Go 按钮
                shouldShowGo = _activity.PuzzleProgress >= _activity.MaxProgress;
            }
            else
            {
                // 非循环模式：完成当前轮且不是最后一轮才显示
                shouldShowGo = _activity.PuzzleProgress >= _activity.MaxProgress && _activity.Round < _activity.conf.NormalRoundId.Count - 1;
            }
            btnGo.gameObject.SetActive(shouldShowGo);
            
            // 重置停留时间计时器
            _stayTime = 0f;
        }

        private void RefreshToken()
        {
            tokenNum.text = _activity.TokenNum.ToString();
            btnPlay.gameObject.SetActive(_activity.TokenNum <= 0);
        }

        private void OnClickPlay()
        {
            if (Game.Manager.mapSceneMan.scene.Active)
            {
                GameProcedure.SceneToMerge();
            }
            Close();
        }

        private void OnClickBigReward()
        {
            var reward = fat.conf.EventPuzzleRewardsVisitor.GetOneByFilter(x => x.Id == _activity.confD.RewardId[^1].ConvertToInt());
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips,
                    bigRewardIcon.transform.position,
                    bigRewardIcon.transform.GetComponent<RectTransform>().rect.size.y * 0.5f,
                    reward.RewardId);   
        }

        private void OnClickGo()
        {
            _activity.MoveToNextRound();
            nextRoundAnim.SetTrigger("Transition");
            progress.Refresh(_activity.PuzzleProgress, _activity.MaxProgress);
            RefreshMilestone();
            RefreshBigReward();
            for (int i = 0; i < _milestoneCellList.Count; i++)
            {
                _milestoneCellList[i].GetComponent<PuzzleMilestoneCell>().SetUnFinish();
            }
            bigRewardAnim.SetTrigger("Num");
            btnGo.gameObject.SetActive(false);
            btnPlay.gameObject.SetActive(true);
        }

        private void OnClickClose()
        {
            Close();
        }

        private void OnClickBtn(int index)
        {
            if (_activity.HasPutPuzzle(index))
            {
                return;
            }
            var success = _activity.TryPutPuzzle(index);
            if (success)
            {
                imgListRoot[index].gameObject.SetActive(false);
                _stayTime = 0f;
                block.SetActive(true);
                imgList[index].gameObject.SetActive(true);
                imgList[index].transform.parent.SetAsLastSibling();
                imgList[index].transform.parent.GetComponent<Animator>().SetTrigger("Punch");
                Game.Manager.audioMan.TriggerSound("PuzzleClick");
                RefreshToken();
                StartCoroutine(DelayBlock());
            }
            else
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.NoPieces);
            }
        }

        private IEnumerator DelayBlock()
        {
            yield return new WaitForSeconds(0.5f);
            progress.Refresh(_activity.PuzzleProgress, _activity.MaxProgress, 0.5f);
            milestoneNum.text = _activity.PuzzleProgress.ToString();
            milestoneNumAnim.SetTrigger("Punch");
            yield return new WaitForSeconds(0.5f);
            if (_activity.RewardCommitList.Count > 0)
            {
                if (_activity.PuzzleProgress >= _activity.MaxProgress)
                {
                    bigRewardAnim.SetTrigger("Punch");
                    yield return new WaitForSeconds(1f);
                    efx.SetActive(true);
                    Game.Manager.audioMan.TriggerSound("PuzzleComplete");
                    // 决定是否显示 Go 按钮
                    bool shouldShowGo;
                    if (_activity.conf.IsCycle)
                    {
                        // 循环模式：只要完成当前轮就显示 Go 按钮
                        shouldShowGo = _activity.PuzzleProgress >= _activity.MaxProgress;
                    }
                    else
                    {
                        // 非循环模式：完成当前轮且不是最后一轮才显示
                        shouldShowGo = _activity.PuzzleProgress >= _activity.MaxProgress && _activity.Round < _activity.conf.NormalRoundId.Count - 1;
                    }
                    btnGo.gameObject.SetActive(shouldShowGo);
                    
                    yield return new WaitForSeconds(efxDuration);
                    UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, bigRewardIcon.transform.position, _activity.RewardCommitList, fat.conf.EventPuzzleRewardsVisitor.GetOneByFilter(x => x.Id == _activity.confD.RewardId[^1].ConvertToInt()).Image, I18N.Text("#SysComDesc726"));
                    block.SetActive(false);
                    
                    // 非循环模式且到达最后一轮时显示"结束"文本
                    if (!_activity.conf.IsCycle && _activity.Round >= _activity.conf.NormalRoundId.Count - 1)
                    {
                        btnText.text = I18N.Text("#SysComBtn6");
                    }
                }
                else
                {
                    _milestoneCellList[_activity.MilestoneIndex].GetComponent<PuzzleMilestoneCell>().SetFinish();
                    yield return new WaitForSeconds(1f);
                    UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, _milestoneCellList[_activity.MilestoneIndex].transform.position, _activity.RewardCommitList, fat.conf.EventPuzzleRewardsVisitor.GetOneByFilter(x => x.Id == _activity.confD.RewardId[_activity.MilestoneIndex].ConvertToInt()).Image, I18N.Text("#SysComDesc726"));
                    block.SetActive(false);
                }
            }
            else
            {
                block.SetActive(false);
            }
        }

        /// <summary>
        /// 随机显示一张未放置的拼图
        /// </summary>
        private void ShowRandomPuzzle()
        {
            if (_activity == null || imgList == null || imgList.Length == 0 || _activity.TokenNum <= 0) return;
            
            // 收集所有未放置的拼图位置
            _availablePositions.Clear();
            for (int i = 0; i < imgList.Length; i++)
            {
                if (!_activity.HasPutPuzzle(i))
                {
                    _availablePositions.Add(i);
                }
            }

            for (int i = 0; i < imgListRoot.Length; i++)
            {
                imgListRoot[i].gameObject.SetActive(false);
            }
            
            // 如果有可用的位置，随机选择一个并显示
            if (_availablePositions.Count > 0)
            {
                int randomIndex = Random.Range(0, _availablePositions.Count);
                int selectedPosition = _availablePositions[randomIndex];
                
                // 显示选中的拼图
                imgListRoot[selectedPosition].gameObject.SetActive(true);
                tipAnim.SetTrigger("Tip");
            }
        }

        private void RefreshPuzzles()
        {
            puzzleBg.SetImage(_activity.confR.BgImage);
            var bundleAndName = _activity.confR.BgImage.Split(".");
            var name = bundleAndName[0].Split(":");
            for (int i = 0; i < imgList.Length; i++)
            {
                var image = $"{name[0]}:{name[1]}_p{i + 1}.png";
                imgList[i].SetImage(image);
                imgList[i].gameObject.SetActive(_activity.HasPutPuzzle(i));
                imgList[i].transform.parent.transform.GetChild(1).gameObject.SetActive(!_activity.HasPutPuzzle(i));
                imgList[i].transform.parent.transform.GetComponent<Animator>().enabled = !_activity.HasPutPuzzle(i);
            }
            for (int i = 0; i < imgList2.Length; i++)
            {
                imgList2[i].gameObject.SetActive(false);
            }
            
            // 显示下一轮拼图预览
            if (_activity.conf.NormalRoundId.Count > 1)
            {
                bool shouldShowNext;
                int nextRoundId;
                
                if (_activity.conf.IsCycle)
                {
                    // 循环模式：永远显示下一轮预览
                    shouldShowNext = true;
                    int nextRoundIndex = (_activity.Round + 1) % _activity.conf.NormalRoundId.Count;
                    nextRoundId = _activity.conf.NormalRoundId[nextRoundIndex];
                }
                else
                {
                    // 非循环模式：只有不是最后一轮才显示下一轮预览
                    shouldShowNext = _activity.Round + 1 < _activity.conf.NormalRoundId.Count;
                    nextRoundId = shouldShowNext ? _activity.conf.NormalRoundId[_activity.Round + 1] : 0;
                }
                
                if (shouldShowNext)
                {
                    var conf = fat.conf.EventPuzzleRoundVisitor.GetOneByFilter(x => x.Id == nextRoundId);
                    puzzleBg2.SetImage(conf.BgImage);
                }
            }
        }

        private void RefreshBigReward()
        {
            var reward = fat.conf.EventPuzzleRewardsVisitor.GetOneByFilter(x => x.Id == _activity.confD.RewardId[^1].ConvertToInt());
            bigRewardNum.text = _activity.MaxProgress.ToString();
            bigRewardIcon.SetImage(reward.Image);
            bigRewardFinish.gameObject.SetActive(_activity.PuzzleProgress >= _activity.MaxProgress);
            bigRewardNum.gameObject.SetActive(_activity.PuzzleProgress < _activity.MaxProgress);
            bigRewardAnim.SetTrigger("Num");
        }

        protected override void OnPostClose()
        {
            foreach (var cell in _milestoneCellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PUZZLE_MILESTONE_CELL, cell);
            }
            _milestoneCellList.Clear();
        }

        private void RefreshMilestone()
        {
            progress.Refresh(_activity.PuzzleProgress, _activity.MaxProgress);
            milestoneNum.text = _activity.PuzzleProgress.ToString();
            var milestone = _activity.confD.RewardCount;
            foreach (var cell in _milestoneCellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.PUZZLE_MILESTONE_CELL, cell);
            }
            _milestoneCellList.Clear();
            // 创建里程碑cell（不包括最后一个，因为最后一个固定在最右边）
            for (int i = 0; i < milestone.Count - 1; i++)
            {
                var cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.PUZZLE_MILESTONE_CELL, milestoneRoot);
                var milestoneCell = cell.GetComponent<PuzzleMilestoneCell>();
                milestoneCell.SetData(_activity, milestone[i], i);
                _milestoneCellList.Add(cell);
            }
        }
    }
}