/*
*@Author:chaoran.zhang
*@Desc:
*@Created Time:2024.02.05 星期一 14:02:41
*/
using System.Linq;
using UnityEngine;

namespace FAT
{
    public class GuideActImpFirstActiveSet:GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }
        
        public override void Play(string[] param)
        {
            if (param.Length < 3)
            {
                _StopWait();
                Debug.LogError("[GUIDE] first_active_set params less than 3");
                return;
            }
            float.TryParse(param[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var angle);
            bool block = param[1].Contains("true");
            bool mask = param[2].Contains("true");
            var albumData = Game.Manager.cardMan.GetCardAlbumData();
            if (albumData == null)
            {
                _StopWait();
                Debug.LogError("[GUIDE] albumData is null");
            }
            else
            {
                var album = UIManager.Instance.TryGetUI(UIConfig.UICardAlbum) as UICardAlbum;
                if (album == null)
                {
                    _StopWait();
                    Debug.LogError("[GUIDE] UICardAlbum is null");
                    return;
                }
                
                Transform trans = null;
                for (int i = 0; i < albumData.GetConfig().GroupInfo.Count; i++)
                {
                    albumData.GetCollectProgress(albumData.GetConfig().GroupInfo[i], out var ownCount, out var allCount);
                    if (ownCount > 0)
                    {
                        trans = album.FindGroupByIndex(i).GetChild(0);
                        break;
                    }
                }

                if (trans == null)
                    trans = album.FindGroupByIndex(0).GetChild(0);
                Game.Manager.guideMan.ActiveGuideContext?.ShowPointerPro(trans, block, mask, _StopWait);
                Game.Manager.guideMan.ActiveGuideContext?.SetAngleOffset(angle);
            }
        }
    }
}