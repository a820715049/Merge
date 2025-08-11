/*
 * @Author: qun.chao
 * @Date: 2025-04-24 18:43:18
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FAT;
using fat.rawdata;
using TMPro;
using EL;
using DG.Tweening;

namespace MiniGame.SlideMerge
{
    public class UISlideMergeMain : UIBase
    {
        enum State
        {
            Normal,
            Success,
            Fail,
        }

        [SerializeField] private Button btnClose;
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private TextMeshProUGUI txtLevel;   // 当前关卡
        [SerializeField] private TextMeshProUGUI txtTargetNum;   // 目标数量
        [SerializeField] private UIImageRes imgTarget;   // 目标
        [SerializeField] private UIImageRes imgNext;    // 下一个
        [SerializeField] private Transform itemRoot;    // 链条
        [SerializeField] private GameObject guideA;    // 引导A
        [SerializeField] private GameObject guideB;    // 引导B
        [SerializeField] private GameObject goBlock;
        public float frictionMin = 0.01f;
        public float mergeDiscount = 0.1f;
        public float soundThreshold = 200f;
        public float soundThresholdWall = 50f;
        public float resultDelay = 0.5f;
        public MBSlideMergeBallLauncher launcher;
        public MBSlideMergeStage stage;

        public float friction => _game.LevelConf.Friction; 
        public float restitution => _game.LevelConf.Restitution; 
        public float initSpeed => _game.LevelConf.Speed;
        public int checkLine => _game.StageConf.Removing;   // 删除线距离 (百分比)
        public (int id, int num) winTarget => (_game.LevelConf.TargetItem, _game.LevelConf.TargetNum);
        public int maxLevelItemId => _game.MaxLevelItemId;
        public float mergeDist => _game.LevelConf.AttractDistance;
        public IList<int> initItemList => _game.StageConf.InitialState;
        public IList<string> initItemPosList => _game.StageConf.InitialPlace;
        public string poolKey => "minigame_slidemerge_ball";

        #region debug
        private bool isDebugMode => GameSwitchManager.Instance.isDebugMode;
        private Button btnNext;
        #endregion

        private MiniGameSlideMerge _game;
        private State _state = State.Normal;
        private Queue<Ball> _ballPool = new();

        protected override void OnCreate()
        {
            transform.Access("Content/Next", out btnNext);
            btnNext.onClick.AddListener(_OnBtnNextClick);

            launcher.Setup(this);
            stage.Setup(this);

            btnClose.onClick.AddListener(_OnBtnCloseClick);
            GameObjectPoolManager.Instance.PreparePool(poolKey, ballPrefab);
        }

        protected override void OnParse(params object[] items)
        {
            _game = items[0] as MiniGameSlideMerge;
        }

        protected override void OnPreOpen()
        {
            btnNext.GetComponent<Image>().raycastTarget = isDebugMode;

            Game.Manager.audioMan.PlayBgm("MiniGameSlideMergeBGM");
            _state = State.Normal;
            goBlock.SetActive(false);
            launcher.InitOnPreOpen();
            stage.InitOnPreOpen();
            UpdateParam();
            RefreshLevel();
        }

        protected override void OnPostClose()
        {
            launcher.CleanupOnPostClose();
            stage.CleanupOnPostClose();
            Game.Manager.audioMan.PlayDefaultBgm();
        }

        private void LateUpdate()
        {
            CheckSuccess();
            CheckFail();
        }

        public void LaunchBall(GameObject view, Ball data)
        {
            stage.SpawnBall(view, data);
        }

        public (GameObject view, Ball data) SpawnInitBall(int id)
        {
            var item = MiniGameConfig.Instance.GetMiniGameSlideMergeItem(id);
            var go = CreateBallView(item, stage.BallRoot, false);
            var data = CreateBallData(item);
            return (go, data);
        }

        public (GameObject view, Ball data) SpawnNextBall()
        {
            var item = _game.GetNextItem();
            var go = CreateBallView(item, launcher.SpawnRoot, false);
            var data = CreateBallData(item);

            RefreshNextItem();

            // 显示引导
            RefreshGuide(true);
            return (go, data);
        }

        public (GameObject view, Ball data) SpawnMergedBall(int spawnerId)
        {
            var id = spawnerId + 1;
            var item = MiniGameConfig.Instance.GetMiniGameSlideMergeItem(id);
            var go = CreateBallView(item, stage.BallRoot, true);
            var data = CreateBallData(item);

            // 新合成的棋子缩放
            data.IsNewBorn = true;
            data.RadiusScale = 0.1f;

            return (go, data);
        }

        public void ReleaseBall(GameObject view, Ball data)
        {
            GameObjectPoolManager.Instance.ReleaseObject(poolKey, view);
            _ballPool.Enqueue(data);
        }

        private void RefreshNextItem()
        {
            var nextItem = _game.PreviewId;
            var cfg = MiniGameConfig.Instance.GetMiniGameSlideMergeItem(nextItem);
            imgNext.SetImage(cfg.ItemImage);
        }

        private void RefreshLevel()
        {
            var levelConf = _game.LevelConf;
            var targetItem = MiniGameConfig.Instance.GetMiniGameSlideMergeItem(levelConf.TargetItem);
            var num = levelConf.TargetNum;
            txtLevel.SetText(I18N.FormatText("#SysComDesc1089", levelConf.Id));
            txtTargetNum.text = $"x{num}";
            imgTarget.SetImage(targetItem.ItemImage);

            var stageConf = _game.StageConf;
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var item = itemRoot.GetChild(i);
                if (i < stageConf.ItemList.Count)
                {
                    item.gameObject.SetActive(true);
                    var id = stageConf.ItemList[i];
                    var itemConf = MiniGameConfig.Instance.GetMiniGameSlideMergeItem(id);
                    item.Access<UIImageRes>().SetImage(itemConf.ItemImage);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        public void RefreshGuide(bool show)
        {
            void GuideState(bool a, bool b)
            {
                var isComplete = MiniGameManager.Instance.IsLevelComplete(MiniGameType.MiniGameSlideMerge, _game.Index);
                guideA.SetActive(a && !isComplete);
                guideB.SetActive(b && !isComplete);
            }

            if (!show)
            {
                GuideState(false, false);
                return;
            }

            var stageConf = _game.StageConf;
            var guideLevelId = 1;
            if (stageConf.Id != guideLevelId)
            {
                GuideState(false, false);
                return;
            }
            if (_game.SpawnCount <= 2)
            {
                GuideState(true, false);
            }
            else if (_game.SpawnCount <= 3)
            {
                GuideState(false, true);
            }
            else
            {
                GuideState(false, false);
            }
        }

        private GameObject CreateBallView(MiniGameSlideMergeItem item, Transform parent, bool isMonoShader)
        {
            static void SetTransform(Transform trans, float size, float center)
            {
                var rectTrans = trans as RectTransform;
                rectTrans.sizeDelta = new Vector2(size, size);
                rectTrans.pivot = new Vector2(0.5f, center / 100f);
                rectTrans.localScale = Vector3.one;
            }

            var go = GameObjectPoolManager.Instance.CreateObject(poolKey, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            var itemView = go.GetComponent<MBSlideMergeItem>();
            itemView.Bg.SetImage(item.ItemImage);
            itemView.Icon.SetImage(item.ItemImage);

            SetTransform(itemView.Bg.transform, item.ItemSize, item.ItemCenter);
            SetTransform(itemView.Icon.transform, item.ItemSize, item.ItemCenter);

            if (isMonoShader)
            {
                itemView.Bg.gameObject.SetActive(true);
                GameUIUtility.SetMonoShader(itemView.Bg.image);
                itemView.Bg.image.color = ColorUtility.TryParseHtmlString(item.Color, out var color) ? color : Color.white;
            }
            else
            {
                itemView.Bg.gameObject.SetActive(false);
            }
            return go;
        }

        private Ball CreateBallData(MiniGameSlideMergeItem item)
        {
            Ball ball;
            if (_ballPool.Count > 0)
            {
                ball = _ballPool.Dequeue();
            }
            else
            {
                ball = new Ball();
            }
            ball.Id = item.Id;
            ball.Mass = item.ItemQuality;
            ball.RadiusOrig = item.ItemSize * item.ItemRadius / 100f;
            ball.RadiusScale = 1f;
            ball.Vel = Vector2.up * initSpeed;
            ball.IsNewBorn = false;
            ball.IsDead = false;
            ball.IsAttracting = false;
            return ball;
        }

        private void CheckSuccess()
        {
            if (_state != State.Normal) return;
            var count = 0;
            var (id, num) = winTarget;
            foreach (var ball in stage.Table.Balls)
            {
                if (ball.Id == id && ball.Vel.sqrMagnitude < 10f)
                    count++;
            }
            if (count >= num)
            {
                _state = State.Success;
                ShowResult(true);
            }
        }

        private void CheckFail()
        {
            if (_state != State.Normal) return;
            var hasFail = false;
            foreach (var ball in stage.Table.Balls)
            {
                if (ball.Pos.y < stage.RealCheckLineY && ball.Vel.y < 1f)
                {
                    hasFail = true;
                    break;
                }
            }
            if (hasFail)
            {
                _state = State.Fail;
                ShowResult(false);
            }
        }

        private void ShowResult(bool isSuccess)
        {
            var type = MiniGameType.MiniGameSlideMerge;
            var idx = _game.Index;
            var levelId = _game.LevelID;
            var step = _game.SpawnCount;
            if (isSuccess)
            {
                MiniGameManager.Instance.CompleteLevel(type, idx);
                MessageCenter.Get<MSG.MINIGAME_RESULT>().Dispatch(idx, true);
                MiniGameManager.Instance.TrackGameResult(type, idx, levelId, true, step);
            }
            else
            {
                MessageCenter.Get<MSG.MINIGAME_RESULT>().Dispatch(idx, false);
                MiniGameManager.Instance.TrackGameResult(type, idx, levelId, false, step);
            }
            DelayOpenResult(isSuccess, resultDelay);
        }

        private void DelayOpenResult(bool isSuccess, float delay)
        {
            if (goBlock.activeSelf) return;
            goBlock.SetActive(true);
            DOVirtual.DelayedCall(delay, () =>
            {
                UIConfig.UISlideMergeResult.Open(isSuccess);
            });
        }

        private void _OnBtnCloseClick()
        {
            Game.Manager.commonTipsMan.ShowMessageTips(I18N.Text("#SysComDesc612"), I18N.Text("#SysComDesc611"), null, _OnSureLeave);
        }

        private void _OnBtnNextClick()
        {
            _game.DebugIncreaseId();
            RefreshNextItem();
        }

        private void _OnSureLeave()
        {
            DataTracker.TrackMiniGameLeave(MiniGameType.MiniGameSlideMerge, _game?.Index ?? 0, _game?.LevelID ?? 0);
            Close();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            if (!this.IsOpen())
                return;
            UpdateParam();
        }
#endif

        private void UpdateParam()
        {
            stage.Table.restitution = restitution;
            stage.Table.friction = friction;
            stage.Table.frictionMin = frictionMin;
            stage.Table.soundThreshold = soundThreshold;
            stage.Table.soundThresholdWall = soundThresholdWall;
            stage.attractSpeed = _game.LevelConf.AttractSpeed;
            stage.mergeDiscount = mergeDiscount;
        }
    }
}