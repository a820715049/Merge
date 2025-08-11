/**
 * @Author: zhangpengjian
 * @Date: 2024/8/19 10:27:47
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/19 10:27:47
 * Description: 挖沙活动主界面
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

namespace FAT
{
    public class UIDiggingMain : UIBase
    {
        [SerializeField] private RectTransform boardBg;
        [SerializeField] private int cellSize;
        [SerializeField] private GameObject cellSand;
        [SerializeField] private GameObject cellSandBg;
        [SerializeField] private GameObject itemImage;
        [SerializeField] private GameObject progressReward;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Transform sandCellRoot;
        [SerializeField] private Transform sandCellBgRoot;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private Transform progressRewardRoot;
        [SerializeField] private Button btnClose;
        [SerializeField] private TMP_Text keyNum;
        [SerializeField] private GameObject keyGift;
        [SerializeField] private Button btnKey;
        [SerializeField] private Button btnInfo;
        [SerializeField] private List<UICommonItem> rewardList;
        [SerializeField] private GridLayoutGroup row1;
        [SerializeField] private GridLayoutGroup row2;
        [SerializeField] private GridLayoutGroup itemRow1;
        [SerializeField] private GridLayoutGroup itemRow2;
        [SerializeField] private List<GameObject> itemObjList1;
        [SerializeField] private List<GameObject> itemObjList2;
        [SerializeField] private TMP_Text countDown;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text levelNum;
        [SerializeField] private GameObject goBlock;
        [SerializeField] private GameObject noKeyRoot;
        [SerializeField] private GameObject hasKeyRoot;
        [SerializeField] private Button gotoBoardBtn;
        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private AnimationCurve shakeCurve;
        [SerializeField] private float shakeDuration;
        [SerializeField] private float sandHideDuration;
        [SerializeField] private List<float> boardOffset;
        [SerializeField] private RectTransform bg;
        [SerializeField] private bool isNew;
        [SerializeField] private float playSoundDelay;
        [SerializeField] private Transform randomRewardEfx;

        private Action WhenTick;
        private ActivityDigging _activity;
        private Dictionary<(int, int), GameObject> itemDic = new();
        private Dictionary<(int, int), GameObject> itemBgDic = new();
        private List<GameObject> sandList = new();
        private Dictionary<(int, int), GameObject> sandBgList = new();
        private List<GameObject> progressRewardList = new();
        private List<RewardCommitData> rewardCommits = new();
        private Dictionary<int, ActivityDigging.DiggingItem> bombDict = new();
        private HashSet<int> processedCells = new();
        private int curLevelIdx;
        private int totalLevelCount;
        private List<ActivityDigging.DiggingItem> items;
        private bool blockClickCell = false;
        private string cellLockKey;
        private string itemKey;
        private string itemBgKey;
        private string progressRewardKey;
        private string cellBgKey;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(OnBtnQuit);
            btnInfo.onClick.AddListener(OnClickInfo);
            btnKey.onClick.AddListener(OnBtnKey);
            gotoBoardBtn.onClick.AddListener(OnClickPlay);
        }

        private void OnClickPlay()
        {
            UIDiggingUtility.SetEnterFrom(true);
            UIDiggingUtility.LeaveActivity();
        }

        protected override void OnAddListener()
        {
            WhenTick ??= _RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            MessageCenter.Get<MSG.DIGGING_KEY_UPDATE>().AddListener(OnKeyRefresh);
            MessageCenter.Get<MSG.DIGGING_LEVE_CLEAR>().AddListener(OnLevelClear);
            MessageCenter.Get<MSG.DIGGING_LEVE_ROUND_CLEAR>().AddListener(OnLevelRoundClear);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().AddListener(OnLevelClear);
            MessageCenter.Get<MSG.DIGGING_PROGRESS_REFRESH>().AddListener(OnProgressAnim);
            MessageCenter.Get<MSG.DIGGING_REWARD_FLY_FEEDBACK>().AddListener(OnFlyFeedback);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().AddListener(OnPostCommitReward);
        }

        private void OnFlyFeedback(FlyType t)
        {
            if (t == FlyType.DiggingShovel)
            {
                RefreshKeyNum();
                OnFlyComplete();
            }
        }

        private void OnPostCommitReward(RewardCommitData data)
        {
            if (_activity == null)
            {
                return;
            }
            // 此处忽略掉活动玩法中开出的钥匙
            if (data.rewardId == _activity.diggingConfig.TokenId && data.reason != ReasonString.digging_level_reward)
            {
                RefreshKeyNum();
            }
        }

        private void OnLevelRoundClear()
        {
            if (_activity.HasNextRound())
            {
                UIDiggingUtility.MoveToNextLevel();
            }
            else
            {
                UIDiggingUtility.LeaveActivity();
            }
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            MessageCenter.Get<MSG.DIGGING_KEY_UPDATE>().RemoveListener(OnKeyRefresh);
            MessageCenter.Get<MSG.DIGGING_LEVE_CLEAR>().RemoveListener(OnLevelClear);
            MessageCenter.Get<MSG.DIGGING_LEVE_ROUND_CLEAR>().RemoveListener(OnLevelRoundClear);
            MessageCenter.Get<MSG.UI_SPECIAL_REWARD_FINISH>().RemoveListener(OnLevelClear);
            MessageCenter.Get<MSG.DIGGING_PROGRESS_REFRESH>().RemoveListener(OnProgressAnim);
            MessageCenter.Get<MSG.DIGGING_REWARD_FLY_FEEDBACK>().RemoveListener(OnFlyFeedback);
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().RemoveListener(OnPostCommitReward);
        }

        private void OnLevelClear()
        {
            (UIManager.Instance.TryGetUI(UIConfig.UICommonShowRes) as UICommonShowRes)?.ShowOtherRes();
            StartCoroutine(_CoLevelClear());
        }

        private IEnumerator _CoLevelClear()
        {
            UIDiggingUtility.SetBlock(true);

            // 等待飞奖励
            yield return new WaitForSeconds(1.5f);

            UIDiggingUtility.SetBlock(false);

            if (!_activity.HasNextRound())
            {
                UIDiggingUtility.LeaveActivity();
                yield break;
            }

            var tar = curLevelIdx + 1;
            if (tar >= totalLevelCount)
            {
                // 关卡组完成
                _activity.NewRoundRes.ActiveR.Open();
            }
            else
            {
                UIDiggingUtility.MoveToNextLevel();
            }
        }

        private void OnKeyRefresh(int keyNum)
        {
            if (keyNum < 0)
            {
                RefreshKeyNum();
                noKeyRoot.gameObject.SetActive(_activity.GetKeyNum() <= 0);
                hasKeyRoot.gameObject.SetActive(_activity.GetKeyNum() > 0);
            }
        }

        private void OnClickInfo()
        {
            _activity.HelpRes.ActiveR.Open();
        }

        private void OnBtnQuit()
        {
            UIDiggingUtility.LeaveActivity();
        }

        private void OnBtnKey()
        {
            UIDiggingUtility.TryOpenGiftShop();
        }

        private void TryOpenGift()
        {
            if (_activity.GetKeyNum() == 0 && _activity.PackValid && _activity.pack.BuyCount < _activity.pack.StockTotal)
            {
                var popup = Game.Manager.screenPopup;
                _activity.TryPopupGift(popup, fat.rawdata.PopupType.TreasureEnterNoKey);
            }
        }

        protected override void OnParse(params object[] items)
        {
        }

        protected override void OnPreOpen()
        {
            if (!UIDiggingUtility.TryGetEventInst(out _activity)) return;
            UIDiggingUtility.InstallBlock(goBlock);
            var theme = _activity.diggingConfig.DiggingTheme;
            cellLockKey = $"{PoolItemType.DIGGING_BOARD_CELL_LOCK}_{theme}";
            itemKey = $"{PoolItemType.DIGGING_BOARD_ITEM}_{theme}";
            itemBgKey = $"{PoolItemType.DIGGING_BOARD_ITEM_BG}_{theme}";
            progressRewardKey = $"{PoolItemType.DIGGING_PROGRESS_REWARD}_{theme}";
            cellBgKey = $"{PoolItemType.DIGGING_BOARD_CELL_BG}_{theme}";
            GameObjectPoolManager.Instance.PreparePool(cellLockKey, cellSand);
            GameObjectPoolManager.Instance.PreparePool(itemKey, itemImage);
            GameObjectPoolManager.Instance.PreparePool(itemBgKey, itemImage);
            GameObjectPoolManager.Instance.PreparePool(progressRewardKey, progressReward);
            GameObjectPoolManager.Instance.PreparePool(cellBgKey, cellSandBg);
            blockClickCell = false;
            var (levelIdx, levelCount) = _activity.GetLevelInfo();
            curLevelIdx = levelIdx;
            totalLevelCount = levelCount;
            UIDiggingUtility.SetBlock(false);
            UIManager.Instance.OpenWindow(UIConfig.UICommonShowRes);
            RefreshBg();
            RefreshBoard();
            RefreshKeyNum();
            RefreshLevelReward();
            items = _activity.GetCurrentLevelAllItems();
            RefreshItem();
            RefreshLevelProgress();
            title.SetText(I18N.Text(_activity.diggingConfig.Name));
            UIUtility.CountDownFormat(countDown, _activity.Countdown);
#if UNITY_EDITOR
            transform.FindEx<Button>("Content/GM1").WithClickScale().FixPivot().onClick.AddListener(() =>
            {
                sandCellRoot.gameObject.SetActive(true);
            });
            transform.FindEx<Button>("Content/GM2").WithClickScale().FixPivot().onClick.AddListener(() =>
            {
                sandCellRoot.gameObject.SetActive(false);
            });
            transform.FindEx<Button>("Content/GM3").WithClickScale().FixPivot().onClick.AddListener(() =>
            {
                _activity.DebugLevelMap();
            });
#else
            transform.Find("Content/GM1").gameObject.SetActive(false);
            transform.Find("Content/GM2").gameObject.SetActive(false);
            transform.Find("Content/GM3").gameObject.SetActive(false);
#endif
        }

        private void RefreshBg()
        {
            var container = bg.parent as RectTransform;
            var screenW = container.rect.width;
            var screenH = container.rect.height;
            var bgScale = 1f;
            var adjustScaleX = screenW / bg.rect.width;
            var adjustScaleY = screenH / bg.rect.height;
            if (adjustScaleX > 1 || adjustScaleY > 1)
            {
                // bg和宝箱等比缩放后未能铺满全屏
                // 需要对bg额外缩放
                bgScale *= Mathf.Max(adjustScaleX, adjustScaleY);
            }
            bg.localScale = Vector3.one * bgScale;
        }

        private void RefreshLevelProgress()
        {
            var progressOffset = 20;
            levelNum.SetText(_activity.GetShowLevelNum().ToString());
            //实例每个宝箱
            var allLevel = _activity.detailConfig.Includelevel;
            //设置进度条宽度 间隔+宝箱宽度
            scroll.content.sizeDelta = new Vector2((52 + 98) * allLevel.Count + progressOffset, 0);

            var currentLevel = Mathf.Clamp(_activity.GetCurrentLevel().ShowNum, 1, allLevel.Count);
            float levelWidth = scroll.content.rect.width / allLevel.Count;
            float minScrollPosition = Mathf.Clamp01((float)(currentLevel - 1) / (allLevel.Count - 4));
            float maxScrollPosition = Mathf.Clamp01((float)(currentLevel - 4) / (allLevel.Count - 4));
            if (scroll != null)
            {
                scroll.horizontalNormalizedPosition = Mathf.Lerp(minScrollPosition, maxScrollPosition, 0.5f);
            }

            for (int i = 0; i < allLevel.Count; i++)
            {
                var levelNum = i;
                var progressReward = GameObjectPoolManager.Instance.CreateObject(progressRewardKey, progressRewardRoot);
                progressReward.GetComponent<Button>().onClick.AddListener(() => OnClickProgressReward(allLevel[levelNum], progressReward.transform));
                progressReward.GetComponent<UIImageRes>().SetImage(_activity.GetLevelConfig(allLevel[i]).LevelRewardIcon1);
                progressReward.transform.GetChild(0).gameObject.SetActive(i < curLevelIdx);
                progressRewardList.Add(progressReward);
            }

            string formatter(long cur, long tar)
            {
                return $"{cur / 100}/{tar / 100}";
            }
            // 进度条x100 用于表现动画
            var (cur, max) = _activity.GetLevelInfo();
            progressBar.SetFormatter(formatter);
            progressBar.ForceSetup(0, max * 100, cur * 100);

            curLevelIdx = cur;
            totalLevelCount = max;
        }

        private void OnClickProgressReward(int levelId, Transform t)
        {
            //查看奖励详情
            var reward = _activity.GetLevelRewards(levelId);
            UIManager.Instance.OpenWindow(UIConfig.UICommonRewardTips, t.position, 0f, reward);
        }

        private void RefreshItem()
        {
            itemRow2.gameObject.SetActive(items.Count > 4);
            //说明只有一行
            if (items.Count <= 3)
            {
                itemRow1.cellSize = new Vector2(170, 170);
                itemRow1.spacing = new Vector2(20, 0);
            }
            else
            {
                itemRow1.cellSize = new Vector2(130, 130);
                itemRow1.spacing = new Vector2(26, 0);
                itemRow2.cellSize = new Vector2(130, 130);
                itemRow2.spacing = new Vector2(26, 0);
            }
            foreach (var item in itemObjList1)
            {
                item.gameObject.SetActive(false);
            }
            foreach (var item in itemObjList2)
            {
                item.gameObject.SetActive(false);
            }
            //只有5个和6个时 是3个一行
            if (items.Count != 5 && items.Count != 6)
            {
                RefreshItemLayout(itemObjList1, items);
            }
            else
            {
                RefreshItemLayout(itemObjList2, items);
            }
        }

        private void RefreshItemLayout(List<GameObject> itemObjList, List<ActivityDigging.DiggingItem> items)
        {
            float targetWidth = items.Count > 3 ? 130 : 170;
            float targetHeight = items.Count > 3 ? 130 : 170;
            for (int i = 0; i < itemObjList.Count; i++)
            {
                if (i < items.Count)
                {
                    itemObjList[i].gameObject.SetActive(true);
                    var item = items[i].item;
                    var darkImg = itemObjList[i].gameObject.transform.GetChild(0).GetComponent<UIImageRes>();
                    var img = itemObjList[i].gameObject.transform.GetChild(1);
                    darkImg.SetImage(item.DarkImg.ConvertToAssetConfig());
                    var hasGot = _activity.HasGot(items[i].x, items[i].y, item.ColSize, item.RowSize);
                    itemObjList[i].transform.localRotation = Quaternion.Euler(0, 0, -item.DarkImgTurn);
                    img.gameObject.SetActive(hasGot);
                    var rt = img.GetComponent<RectTransform>();
                    float originalWidth = item.ColSize * 136;
                    float originalHeight = item.RowSize * 136;
                    if (item.Levelturn == 90 || item.Levelturn == 270)
                    {
                        originalWidth = item.RowSize * 136;
                        originalHeight = item.ColSize * 136;
                    }

                    float aspectRatio = (float)originalWidth / originalHeight;
                    float targetAspectRatio = (float)targetWidth / targetHeight;
                    float newWidth, newHeight;

                    if (aspectRatio > targetAspectRatio)
                    {
                        // 宽比高长，基于宽度缩放
                        newWidth = targetWidth;
                        newHeight = Mathf.RoundToInt(targetWidth / aspectRatio);
                    }
                    else
                    {
                        // 高比宽长，基于高度缩放
                        newWidth = Mathf.RoundToInt(targetHeight * aspectRatio);
                        newHeight = targetHeight;
                    }
                    // 设置sizeDelta以保持比例并缩放到目标尺寸
                    rt.sizeDelta = new Vector2(newWidth, newHeight);
                    img.transform.localPosition = Vector3.zero;
                    img.transform.localScale = item.ImgZoom > 0 ? new Vector3(item.ImgZoom, item.ImgZoom, 0) : Vector3.one;
                    img.GetComponent<UIImageRes>().SetImage(items[i].item.Image.ConvertToAssetConfig());
                    if (isNew)
                    {
                        darkImg.gameObject.SetActive(isNew);
                    }
                    else
                    {
                        darkImg.gameObject.SetActive(!hasGot);
                    }
                }
                else
                {
                    itemObjList[i].gameObject.SetActive(false);
                }
            }
        }

        private void RefreshLevelReward()
        {
            foreach (var item in rewardList)
            {
                item.transform.parent.gameObject.SetActive(false);
            }
            var r = _activity.GetCurrentLevel().LevelReward;
            row2.gameObject.SetActive(r.Count > 2);
            for (int i = 0; i < r.Count; i++)
            {
                var reward = rewardList[i];
                if (i < r.Count)
                {
                    reward.transform.parent.gameObject.SetActive(true);
                    reward.Refresh(r[i].ConvertToRewardConfig());
                }
                else
                {
                    reward.transform.parent.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshKeyNum()
        {
            keyNum.text = _activity.GetKeyNum().ToString();
            keyGift.SetActive(_activity.PackValid);
            noKeyRoot.gameObject.SetActive(_activity.GetKeyNum() <= 0);
            hasKeyRoot.gameObject.SetActive(_activity.GetKeyNum() > 0);
        }

        private void RefreshBoard()
        {
            var random = new System.Random(_activity.GetSeed());
            //初始棋盘大小
            var (r, c) = _activity.GetBoardSize();
            var maxNum = r * c;
            boardBg.sizeDelta = new Vector2(c * cellSize, r * cellSize);
            if (UIDiggingUtility.boardOriginPos.y == 0)
            {
                UIDiggingUtility.boardOriginPos = boardBg.position;
            }
            boardBg.position = new Vector3(boardBg.position.x, UIDiggingUtility.boardOriginPos.y + boardOffset[r - 3], 0);//棋盘大小3*3 - 7*7
            Vector3 initialPosition = new Vector3(-((c - 1) * cellSize / 2), (r - 1) * cellSize / 2, 0);
            //初始未挖掘过的格子
            for (int row = 0; row < r; row++)
            {
                for (int col = 0; col < c; col++)
                {
                    var idx = row * c + col;
                    // 实例化Prefab
                    var cellSand = GameObjectPoolManager.Instance.CreateObject(cellLockKey, sandCellRoot);
                    cellSand.GetComponent<Button>().onClick.AddListener(() => OnClickCell(idx));
                    // 计算每个格子的位置
                    var gridPosition = initialPosition + new Vector3(col * cellSize, -row * cellSize, 0);
                    cellSand.transform.localPosition = gridPosition;
                    cellSand.transform.localScale = Vector3.one;
                    cellSand.transform.Find("Ani").gameObject.SetActive(false);
                    cellSand.transform.Find("CellsandBomb_transverse").gameObject.SetActive(false);
                    cellSand.transform.Find("CellsandBomb_longitudinal").gameObject.SetActive(false);
                    cellSand.transform.Find("UIDiggingBomb").gameObject.SetActive(false);
                    cellSand.GetComponent<Image>().enabled = true;
                    var n = random.Next(0, 2);
                    if (n == 0)
                    {
                        _activity.Visual.Refresh(cellSand.GetComponent<UIImageRes>(), "sand1");
                        _activity.Visual.Refresh(cellSand.transform.Find("Ani").Find("Sand").GetComponent<UIImageRes>(), "sand1");
                    }
                    else
                    {
                        _activity.Visual.Refresh(cellSand.GetComponent<UIImageRes>(), "sand2");
                        _activity.Visual.Refresh(cellSand.transform.Find("Ani").Find("Sand").GetComponent<UIImageRes>(), "sand2");
                    }
                    sandList.Add(cellSand);
                    cellSand.gameObject.SetActive(!_activity.HasDug(idx));
                    cellSand.transform.Find("a").gameObject.SetActive(false);
                    cellSand.transform.Find("b").gameObject.SetActive(false);
                    cellSand.transform.Find("c").gameObject.SetActive(false);
                    cellSand.transform.Find("d").gameObject.SetActive(false);
                    if (!_activity.HasDug(idx))
                    {
                        cellSand.transform.Find("a").gameObject.SetActive(HasNeighber(col, row, 1));
                        cellSand.transform.Find("b").gameObject.SetActive(HasNeighber(col, row, 2));
                        cellSand.transform.Find("c").gameObject.SetActive(HasNeighber(col, row, 3));
                        cellSand.transform.Find("d").gameObject.SetActive(HasNeighber(col, row, 4));
                    }
                }
            }
            var items = _activity.GetCurrentLevelAllItems();

            foreach (var item in items)
            {
                if (_activity.HasGot(item.x, item.y, item.item.ColSize, item.item.RowSize))
                {
                    continue;
                }
                for (int row = item.y - 1; row < item.item.RowSize + item.y - 1; row++)
                {
                    for (int col = item.x - 1; col < item.item.ColSize + item.x - 1; col++)
                    {
                        var itemBg = GameObjectPoolManager.Instance.CreateObject(cellBgKey, sandCellBgRoot);
                        itemBg.SetActive(true);
                        var gridPosition = initialPosition + new Vector3(col * cellSize, -row * cellSize, 0);
                        itemBg.transform.localPosition = gridPosition;
                        itemBg.transform.localScale = Vector3.one;
                        sandBgList.Add((col, row), itemBg);
                    }
                }

            }
            if (isNew)
            {
                foreach (var item in items)
                {
                    if (_activity.HasGot(item.x, item.y, item.item.ColSize, item.item.RowSize))
                    {
                        continue;
                    }
                    var turn = item.item.Levelturn;
                    var itemObj = GameObjectPoolManager.Instance.CreateObject(itemBgKey, itemRoot);
                    itemObj.SetActive(true);
                    var asset = item.item.DarkImgBig.ConvertToAssetConfig();
                    itemObj.GetComponent<UIImageRes>().SetImage(asset.Group, asset.Asset);
                    var width = cellSize * item.item.ColSize;
                    var height = cellSize * item.item.RowSize;
                    if (turn == 90 || turn == 270)
                    {
                        itemObj.GetComponent<RectTransform>().sizeDelta = new Vector2(height, width);
                    }
                    else
                    {
                        itemObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
                    }
                    var gridPosition = initialPosition + new Vector3((item.x - 1) * cellSize, -(item.y - 1) * cellSize, 0);
                    var centerX = gridPosition.x + (width - cellSize) / 2.0f;
                    var centerY = gridPosition.y - (height - cellSize) / 2.0f;
                    itemObj.transform.localPosition = new Vector3(centerX, centerY, 0);
                    itemObj.transform.localScale = Vector3.one;
                    itemObj.transform.localRotation = Quaternion.Euler(0, 0, turn);
                    var idx = item.y * r + item.x;
                    if (isNew)
                    {
                        itemBgDic.Add((item.x, item.y), itemObj);
                    }
                }
            }
            //初始所有海马等 待挖掘物的位置
            foreach (var item in items)
            {
                if (_activity.HasGot(item.x, item.y, item.item.ColSize, item.item.RowSize))
                {
                    continue;
                }
                var turn = item.item.Levelturn;
                var itemObj = GameObjectPoolManager.Instance.CreateObject(itemKey, itemRoot);
                itemObj.SetActive(true);
                var asset = item.item.Image.ConvertToAssetConfig();
                itemObj.GetComponent<UIImageRes>().SetImage(asset.Group, asset.Asset);
                var width = cellSize * item.item.ColSize;
                var height = cellSize * item.item.RowSize;
                if (turn == 90 || turn == 270)
                {
                    itemObj.GetComponent<RectTransform>().sizeDelta = new Vector2(height, width);
                }
                else
                {
                    itemObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
                }
                var gridPosition = initialPosition + new Vector3((item.x - 1) * cellSize, -(item.y - 1) * cellSize, 0);
                var centerX = gridPosition.x + (width - cellSize) / 2.0f;
                var centerY = gridPosition.y - (height - cellSize) / 2.0f;
                itemObj.transform.localPosition = new Vector3(centerX, centerY, 0);
                itemObj.transform.localScale = Vector3.one;
                itemObj.transform.localRotation = Quaternion.Euler(0, 0, turn);
                var idx = item.y * r + item.x;
                itemDic.Add((item.x, item.y), itemObj);
            }
        }

        //1左上 顺时针
        private bool HasNeighber(int x, int y, int dir)
        {
            var (r, c) = _activity.GetBoardSize();
            var (idx1, idx2, idx3) = (-1, -1, -1);
            if (dir == 1)
            {
                if (y - 1 >= 0 && y - 1 < r)
                {
                    idx1 = (y - 1) * r + x;
                }
                if (x - 1 >= 0 && x - 1 < c)
                {
                    idx2 = y * r + (x - 1);
                }
                if (x - 1 >= 0 && x - 1 < c && y - 1 >= 0 && y - 1 < r)
                {
                    idx3 = (y - 1) * r + (x - 1);
                }
            }
            else if (dir == 2)
            {
                if (y - 1 >= 0 && y - 1 < r)
                {
                    idx1 = (y - 1) * r + x;
                }
                if (x + 1 >= 0 && x + 1 < c)
                {
                    idx2 = y * r + (x + 1);
                }
                if (x + 1 >= 0 && x + 1 < c && y - 1 >= 0 && y - 1 < r)
                {
                    idx3 = (y - 1) * r + (x + 1);
                }
            }
            else if (dir == 3)
            {
                if (y + 1 >= 0 && y + 1 < r)
                {
                    idx1 = (y + 1) * r + x;
                }
                if (x + 1 >= 0 && x + 1 < c)
                {
                    idx2 = y * r + (x + 1);
                }
                if (x + 1 >= 0 && x + 1 < c && y + 1 >= 0 && y + 1 < r)
                {
                    idx3 = (y + 1) * r + (x + 1);
                }
            }
            else if (dir == 4)
            {
                if (y + 1 >= 0 && y + 1 < r)
                {
                    idx1 = (y + 1) * r + x;
                }
                if (x - 1 >= 0 && x - 1 < c)
                {
                    idx2 = y * r + (x - 1);
                }
                if (x - 1 >= 0 && x - 1 < c && y + 1 >= 0 && y + 1 < r)
                {
                    idx3 = (y + 1) * r + (x - 1);
                }
            }
            return (idx1 != -1 && !_activity.HasDug(idx1)) || (idx2 != -1 && !_activity.HasDug(idx2)) || (idx3 != -1 && !_activity.HasDug(idx3));
        }

        private void OnClickCell(int index)
        {
            if (blockClickCell)
            {
                return;
            }
            rewardCommits.Clear();
            var (r, c) = _activity.GetBoardSize();
            var level = _activity.GetCurrentLevel();
            var success = _activity.TryDiggingCell(index, rewardCommits, out ActivityDigging.DiggingCellState state, out ActivityDigging.DiggingResult result);
            if (!success)
            {
                if (state == ActivityDigging.DiggingCellState.KeyNotEnough)
                {
                    UIDiggingUtility.OnKeyNotEnough(sandList[index].transform.position);
                }
                return;
            }
            if (state == ActivityDigging.DiggingCellState.HasDug)
            {
                Debug.LogError($"HasDug.. index = {index}");
                return;
            }
            if (state != ActivityDigging.DiggingCellState.GetRandom && state != ActivityDigging.DiggingCellState.Fail)
            {
                blockClickCell = true;
            }
            if (isNew)
            {
                StartCoroutine(CoPlayDigSound());
            }
            else
            {
                UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.Digging);
            }

            // 处理炸弹效果
            if (state == ActivityDigging.DiggingCellState.Bomb || 
                state == ActivityDigging.DiggingCellState.BombAndGet || 
                state == ActivityDigging.DiggingCellState.BombAndGetAll)
            {
                StartCoroutine(CoPlayBombAndShowItems(result.bombItems, result.explodedCells, result.obtainedItems));
            }
            else if (result.obtainedItems.Count > 0)
            {
                foreach (var item in result.obtainedItems)
                {
                    StartCoroutine(CoShowItemAnim(item));
                }
            }
            
            StartCoroutine(CoHideSandCorner(r, c, state, index));
            sandList[index].transform.Find("Ani").gameObject.SetActive(true);
            sandList[index].GetComponent<Image>().enabled = false;
            sandList[index].transform.SetParent(transform.Find("Content/Board"));
            sandList[index].transform.SetAsLastSibling();
            if (state == ActivityDigging.DiggingCellState.GetRandom)
            {
                foreach (var reward in rewardCommits)
                {
                    if (Game.Manager.objectMan.IsType(reward.rewardId, ObjConfigType.Coin) || reward.rewardId == Constant.kMergeEnergyObjId)
                    {
                        (UIManager.Instance.TryGetUI(UIConfig.UICommonShowRes) as UICommonShowRes)?.ShowOtherRes();
                        break;
                    }
                }
            }
            StartCoroutine(UIDiggingUtility.CoOnCellDigging(state, sandList[index].transform, rewardCommits, level, OnFlyComplete));
        }

        private void OnFlyComplete()
        {
            randomRewardEfx.gameObject.SetActive(false);
            randomRewardEfx.gameObject.SetActive(true);
            Game.Manager.audioMan.TriggerSound("BingoLight");
        }

        private IEnumerator CoPlayBombAndShowItems(List<ActivityDigging.DiggingItem> bombItems, List<int> explodedCells, List<ActivityDigging.DiggingItem> obtainedItems)
        {
            yield return StartCoroutine(CoPlayBombEffects(bombItems, explodedCells));
            
            // 炸弹效果完全结束后，开始展示获得的物品
            foreach (var item in obtainedItems)
            {
                StartCoroutine(CoShowItemAnim(item));
            }
        }

        private IEnumerator CoPlayBombEffects(List<ActivityDigging.DiggingItem> bombItems, List<int> explodedCells)
        {
            // 等待初始挖掘动画完成
            yield return new WaitForSeconds(0.5f);
            
            var (row, col) = _activity.GetBoardSize();
            bombDict.Clear();
            foreach (var bombItem in bombItems)
            {
                var index = (bombItem.y - 1) * col + (bombItem.x - 1);
                bombDict[index] = bombItem;
            }

            // 记录已经处理过的格子
            processedCells.Clear();
            
            // 开始处理第一个炸弹
            var firstBombItem = bombItems[0];
            var firstBombIndex = (firstBombItem.y - 1) * col + (firstBombItem.x - 1);
            
            // 启动第一个炸弹的动画
            yield return StartCoroutine(PlaySingleBombAnimation(firstBombItem, firstBombIndex));
            
            // 处理第一个炸弹影响的格子
            var affectedCells = new List<(int index, float distance)>();
            for (int i = 0; i < explodedCells.Count; i++)
            {
                var cellIndex = explodedCells[i];
                if (processedCells.Contains(cellIndex))
                {
                    continue;
                }

                // 检查格子是否在第一个炸弹的影响范围内
                bool isAffectedByFirst = false;
                var cellRow = cellIndex / col;
                var cellCol = cellIndex % col;
                var bombRow = firstBombItem.y - 1;
                var bombCol = firstBombItem.x - 1;
                
                if (firstBombItem.item.BoomType == 1) // 横向炸弹
                {
                    isAffectedByFirst = cellRow == bombRow;
                    if (isAffectedByFirst)
                    {
                        float distance = Mathf.Abs(cellCol - bombCol);
                        affectedCells.Add((cellIndex, distance));
                    }
                }
                else // 纵向炸弹
                {
                    isAffectedByFirst = cellCol == bombCol;
                    if (isAffectedByFirst)
                    {
                        float distance = Mathf.Abs(cellRow - bombRow);
                        affectedCells.Add((cellIndex, distance));
                    }
                }
            }

            // 按距离排序
            affectedCells.Sort((a, b) => a.distance.CompareTo(b.distance));
            
            // 按距离顺序处理格子
            foreach (var (cellIndex, _) in affectedCells)
            {
                processedCells.Add(cellIndex);
                
                var explodedCell = sandList[cellIndex];
                explodedCell.GetComponent<Image>().enabled = false;
                explodedCell.transform.SetParent(transform.Find("Content/Board"));
                explodedCell.transform.SetAsLastSibling();
                
                var explodedEffect = explodedCell.transform.Find("UIDiggingBomb").gameObject;
                explodedEffect.SetActive(true);
                
                StartCoroutine(CoHideSandCorner(row, col, ActivityDigging.DiggingCellState.Bomb, cellIndex, false));
                
                // 如果这个格子是炸弹
                if (bombDict.TryGetValue(cellIndex, out var nextBombItem) && cellIndex != firstBombIndex)
                {
                    var bombCell = sandList[cellIndex];
                    bombCell.transform.Find("CellsandBomb_transverse").gameObject.SetActive(false);
                    bombCell.transform.Find("CellsandBomb_longitudinal").gameObject.SetActive(false);
                    
                    StartCoroutine(PlayDelayedBombEffects(nextBombItem, cellIndex, explodedCells));
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            yield return new WaitForSeconds(0.3f);
            blockClickCell = false;
        }

        private IEnumerator PlayDelayedBombEffects(ActivityDigging.DiggingItem bombItem, int bombIndex, List<int> allExplodedCells)
        {
            yield return new WaitForSeconds(0.3f);
            
            yield return StartCoroutine(PlaySingleBombAnimation(bombItem, bombIndex));
            var (row, col) = _activity.GetBoardSize();
            
            // 收集并排序受影响的格子
            var affectedCells = new List<(int index, float distance)>();
            foreach (var cellIndex in allExplodedCells)
            {
                if (processedCells.Contains(cellIndex) || cellIndex == bombIndex)
                {
                    continue;
                }
                
                var cellRow = cellIndex / col;
                var cellCol = cellIndex % col;
                var bombRow = bombItem.y - 1;
                var bombCol = bombItem.x - 1;
                
                if (bombItem.item.BoomType == 1) // 横向炸弹
                {
                    if (cellRow == bombRow)
                    {
                        float distance = Mathf.Abs(cellCol - bombCol);
                        affectedCells.Add((cellIndex, distance));
                    }
                }
                else // 纵向炸弹
                {
                    if (cellCol == bombCol)
                    {
                        float distance = Mathf.Abs(cellRow - bombRow);
                        affectedCells.Add((cellIndex, distance));
                    }
                }
            }
            
            // 按距离排序
            affectedCells.Sort((a, b) => a.distance.CompareTo(b.distance));
            
            // 按距离顺序处理格子
            foreach (var (cellIndex, _) in affectedCells)
            {
                processedCells.Add(cellIndex);
                
                var explodedCell = sandList[cellIndex];
                explodedCell.GetComponent<Image>().enabled = false;
                explodedCell.transform.SetParent(transform.Find("Content/Board"));
                explodedCell.transform.SetAsLastSibling();
                
                var explodedEffect = explodedCell.transform.Find("UIDiggingBomb").gameObject;
                explodedEffect.SetActive(true);
                
                StartCoroutine(CoHideSandCorner(row, col, ActivityDigging.DiggingCellState.Bomb, cellIndex, false));
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator PlaySingleBombAnimation(ActivityDigging.DiggingItem bombItem, int bombIndex)
        {
            var bombCell = sandList[bombIndex];
            var horizontalBomb = bombCell.transform.Find("CellsandBomb_transverse").gameObject;
            var verticalBomb = bombCell.transform.Find("CellsandBomb_longitudinal").gameObject;
            horizontalBomb.SetActive(bombItem.item.BoomType == 1);
            verticalBomb.SetActive(bombItem.item.BoomType == 2);
            // 播放炸弹音效
            Game.Manager.audioMan.TriggerSound("DiggingBoom");
            yield return new WaitForSeconds(0.7f);
        }

        private IEnumerator CoPlayDigSound()
        {
            yield return new WaitForSeconds(playSoundDelay);
            UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.Digging_dig2);
        }

        private IEnumerator CoHideSandCorner(int r, int c, ActivityDigging.DiggingCellState s, int index, bool willDelay = true)
        {
            if (willDelay)
            {
                yield return new WaitForSeconds(sandHideDuration);
            }
            sandList[index].transform.Find("a").gameObject.SetActive(false);
            sandList[index].transform.Find("b").gameObject.SetActive(false);
            sandList[index].transform.Find("c").gameObject.SetActive(false);
            sandList[index].transform.Find("d").gameObject.SetActive(false);
            RefreshSand(r, c, s);
            if (s != ActivityDigging.DiggingCellState.Get && s != ActivityDigging.DiggingCellState.GetAll)
            {
                blockClickCell = false;
            }
        }

        private IEnumerator CoShowItemAnim(ActivityDigging.DiggingItem diggingItem)
        {
            //等挖格子动画播放完
            yield return new WaitForSeconds(0.5f);
            UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.DiggingShow);
            var trans = itemDic[(diggingItem.x, diggingItem.y)];
            var itemIdx = 0;
            var targetWidth = items.Count > 3 ? 130 : 170;
            var targetHeight = items.Count > 3 ? 130 : 170;
            
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].x == diggingItem.x && items[i].y == diggingItem.y)
                {
                    itemIdx = i;
                    break;
                }
            }
            
            var item = diggingItem.item;
            float originalWidth = item.ColSize * 136;
            float originalHeight = item.RowSize * 136;
            if (item.Levelturn == 90 || item.Levelturn == 270)
            {
                originalWidth = item.RowSize * 136;
                originalHeight = item.ColSize * 136;
            }
            float aspectRatio = (float)originalWidth / originalHeight;
            float targetAspectRatio = (float)targetWidth / targetHeight;
            float newWidth, newHeight;
            if (aspectRatio > targetAspectRatio)
            {
                // 宽比高长，基于宽度缩放
                newWidth = targetWidth;
                newHeight = Mathf.RoundToInt(targetWidth / aspectRatio);
            }
            else
            {
                // 高比宽长，基于高度缩放
                newWidth = Mathf.RoundToInt(targetHeight * aspectRatio);
                newHeight = targetHeight;
            }
            Vector3 to;
            if (items.Count != 5 && items.Count != 6)
            {
                to = itemObjList1[itemIdx].transform.position;
            }
            else
            {
                to = itemObjList2[itemIdx].transform.position;
            }

            for (int row = diggingItem.y - 1; row < item.RowSize + diggingItem.y - 1; row++)
            {
                for (int col = diggingItem.x - 1; col < item.ColSize + diggingItem.x - 1; col++)
                {
                    sandBgList[(col, row)].gameObject.SetActive(false);
                }
            }

            trans.transform.SetParent(transform.Find("Content"));
            trans.transform.SetAsLastSibling();
            var position = trans.transform.position;
            var rt = trans.transform as RectTransform;
            var seq = DOTween.Sequence();
            seq.Append(trans.transform.DOScale(1.25f, 0.16f));
            seq.Append(trans.transform.DORotate(new Vector3(0, 0, 20), shakeDuration, RotateMode.FastBeyond360).SetRelative().SetOptions(true).SetEase(shakeCurve));
            seq.Append(trans.transform.DOMove(new Vector3(position.x - 50, position.y - 150, 0), 0.2f));
            seq.Append(trans.transform.DOMove(to, 0.5f).SetEase(Ease.InSine));
            seq.Join(trans.transform.DORotate(new Vector3(0, 0, -item.ImgTurn), 0.5f).SetEase(Ease.OutQuad));
            seq.Join(trans.transform.DOScale(item.ImgZoom > 0 ? new Vector3(item.ImgZoom, item.ImgZoom, 0) : Vector3.one, 0.5f));
            seq.Join(rt.DOSizeDelta(new Vector2(newWidth, newHeight), 0.6f).SetEase(Ease.OutQuad));
            if (isNew)
            {
                itemBgDic[(diggingItem.x, diggingItem.y)].gameObject.SetActive(false);
            }
            seq.Play().OnComplete(() =>
            {
                if (!isNew)
                {
                    if (items.Count != 5 && items.Count != 6)
                    {
                        itemObjList1[itemIdx].transform.GetChild(0).gameObject.SetActive(false);
                    }
                    else
                    {
                        itemObjList2[itemIdx].transform.GetChild(0).gameObject.SetActive(false);
                    }
                }
                blockClickCell = false;
                UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.DiggingCollect);
            });
        }

        private void RefreshSand(int r, int c, ActivityDigging.DiggingCellState s)
        {
            if (s != ActivityDigging.DiggingCellState.GetAll)
            {
                for (int row = 0; row < r; row++)
                {
                    for (int col = 0; col < c; col++)
                    {
                        var idx = row * c + col;
                        if (!_activity.HasDug(idx))
                        {
                            sandList[idx].transform.Find("a").gameObject.SetActive(HasNeighber(col, row, 1));
                            sandList[idx].transform.Find("b").gameObject.SetActive(HasNeighber(col, row, 2));
                            sandList[idx].transform.Find("c").gameObject.SetActive(HasNeighber(col, row, 3));
                            sandList[idx].transform.Find("d").gameObject.SetActive(HasNeighber(col, row, 4));
                        }
                    }
                }
            }
            else
            {
                for (int row = 0; row < r; row++)
                {
                    for (int col = 0; col < c; col++)
                    {
                        var idx = row * c + col;
                        if (!_activity.HasDug(idx))
                        {
                            sandList[idx].transform.Find("a").gameObject.SetActive(false);
                            sandList[idx].transform.Find("b").gameObject.SetActive(false);
                            sandList[idx].transform.Find("c").gameObject.SetActive(false);
                            sandList[idx].transform.Find("d").gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(countDown, _activity.Countdown);
            if (!UIDiggingUtility.IsEventActive())
            {
                UIDiggingUtility.TryEndActivity();
            }
        }

        protected override void OnPreClose()
        {
            UIManager.Instance.CloseWindow(UIConfig.UICommonShowRes);
            bombDict.Clear();
            processedCells.Clear();
        }

        protected override void OnPostClose()
        {
            foreach (var item in sandList)
            {
                item.GetComponent<Button>().onClick.RemoveAllListeners();
                GameObjectPoolManager.Instance.ReleaseObject(cellLockKey, item);
            }
            foreach (var item in progressRewardList)
            {
                item.GetComponent<Button>().onClick.RemoveAllListeners();
                GameObjectPoolManager.Instance.ReleaseObject(progressRewardKey, item);
            }
            foreach (var item in itemDic)
            {
                GameObjectPoolManager.Instance.ReleaseObject(itemKey, item.Value);
            }
            foreach (var item in itemBgDic)
            {
                GameObjectPoolManager.Instance.ReleaseObject(itemBgKey, item.Value);
            }
            foreach (var item in sandBgList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(cellBgKey, item.Value);
            }
            sandList.Clear();
            itemDic.Clear();
            itemBgDic.Clear();
            progressRewardList.Clear();
            sandBgList.Clear();
#if UNITY_EDITOR
            transform.FindEx<Button>("Content/GM3").WithClickScale().FixPivot().onClick.RemoveAllListeners();
#endif
        }

        private void OnProgressAnim()
        {
            // 进度条加100
            var tar = curLevelIdx + 1;
            progressBar.SetProgress(tar * 100);
            // UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.TreasureProgressGrowth);
        }
    }

}