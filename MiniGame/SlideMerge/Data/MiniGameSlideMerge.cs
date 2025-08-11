/*
 * @Author: qun.chao
 * @Date: 2025-04-28 14:49:29
 */
using System.Collections.Generic;
using UnityEngine;
using FAT;
using fat.rawdata;
using EL;
using System.Linq;

namespace MiniGame.SlideMerge
{
    public class MiniGameSlideMerge : MiniGameBase
    {
        public MiniGameSlideMergeLevel LevelConf => _levelConf;
        public MiniGameSlideMergeStage StageConf => _stageConf;
        public int MaxLevelItemId => _maxLevelItemId;
        public int PreviewId => _previewId;
        public int SpawnCount => _spawnCount;

        private MiniGameSlideMergeLevel _levelConf;
        private MiniGameSlideMergeStage _stageConf;
        private int _spawnCount;
        private int _maxLevelItemId;
        private int _previewId;    // 下一个ID
        private List<(int id, int weight)> _spawnPool = new();

        public override void InitData(int index, bool isGuide, int level)
        {
            LevelID = level;
            Index = index;
            IsGuide = false;
            Type = MiniGameType.MiniGameSlideMerge;

            Reset();
            _levelConf = MiniGameConfig.Instance.GetMiniGameSlideMergeLevel(level);
            _stageConf = MiniGameConfig.Instance.GetMiniGameSlideMergeStage(level);
            _maxLevelItemId = _stageConf.ItemList.Last();

            foreach (var item in _stageConf.RandomPool)
            {
                var (id, weight, _) = item.ConvertToInt3();
                _spawnPool.Add((id, weight));
            }
        }

        public override void DeInit()
        {
            Reset();
        }

        public override void OpenUI()
        {
            UIConfig.UISlideMergeMain.Open(this);
        }

        public override void CloseUI()
        {
            UIConfig.UISlideMergeMain.Close();
        }

        public override void CheckWin() { }

        public override void CheckLose() { }

        public void DebugIncreaseId()
        {
            DebugSetPreviewId(_previewId + 1);
        }

        public void DebugSetPreviewId(int id)
        {
            if (id > MaxLevelItemId)
            {
                id = MaxLevelItemId;
            }
            _previewId = id;
        }

        public MiniGameSlideMergeItem GetNextItem()
        {
            int itemId;
            if (_previewId > 0)
            {
                itemId = _previewId;
                _previewId = SpawnNextItem();
            }
            else
            {
                itemId = SpawnNextItem();
                _previewId = SpawnNextItem();
            }
            return MiniGameConfig.Instance.GetMiniGameSlideMergeItem(itemId);
        }

        private int SpawnNextItem()
        {
            var idx = _spawnCount;
            if (StageConf.FixedPool.Count > idx)
            {
                var item = StageConf.FixedPool[idx];
                _spawnCount++;
                return item;
            }
            else
            {
                var output = _spawnPool.RandomChooseByWeight((e) => e.weight);
                _spawnCount++;
                return output.id;
            }
        }

        private void Reset()
        {
            _spawnCount = 0;
            _spawnPool.Clear();
            _maxLevelItemId = 0;
            _previewId = 0;
        }
    }
}