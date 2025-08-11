using System;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class MBBoardAutoGuide : MonoBehaviour
    {
        public int Interval;
        public BoardType boardType;
        public GameObject Finger;
        public enum BoardType
        {
            None,
            Mineboard,
        }
        private int _curInterval;
        private bool _isPlaying;

        public void SecondUpdate()
        {
            if (_isPlaying || UIManager.Instance.GetLayerRootByType(UILayer.Top).childCount > 0)
            {
                return;
            }
            switch (boardType)
            {
                case BoardType.None:
                    break;
                case BoardType.Mineboard:
                    {
                        _curInterval++;
                        if (_curInterval >= Interval)
                        {
                            RefreshMineAuto();
                        }
                    }
                    break;
            }
        }

        public void Interrupt()
        {
            _curInterval = 0;
            HideFinger();
        }

        private void RefreshMineAuto()
        {
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.Mine, out var activity);
            if (activity != null && activity is MineBoardActivity)
            {
                var act = activity as MineBoardActivity;
                foreach (var kv in BoardViewManager.Instance.ActiveBonusCache)
                {
                    if (act.ConfD.BonusItemMax.Contains(kv.Value))
                    {
                        if (BoardViewManager.Instance.GetItemView(kv.Key) != null)
                        {
                            ShowFinger(BoardViewManager.Instance.GetItemView(kv.Key).transform.position);
                            return;
                        }
                    }
                }
                foreach (var kv in BoardViewManager.Instance.ActiveAutoSourceCache)
                {
                    if (BoardViewManager.Instance.GetItemView(kv.Key) != null)
                    {
                        ShowFinger(BoardViewManager.Instance.GetItemView(kv.Key).transform.position);
                        return;
                    }
                }
            }
        }

        private void ShowFinger(Vector3 pos)
        {
            _isPlaying = true;
            Finger.SetActive(true);
            Finger.transform.position = pos;
        }

        private void HideFinger()
        {
            Finger.SetActive(false);
            _isPlaying = false;
        }
    }
}