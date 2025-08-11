/*
 * @Author: qun.chao
 * @Date: 2023-09-07 11:47:37
 */

using System.Linq;
using UnityEngine;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class GuideActImpTokenMask : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        public override void Play(string[] param)
        {
            if (param.Length < 2)
            {
                _StopWait();
                Debug.LogError("[GUIDE] hand_pro params less than 4");
                return;
            }

            bool block = param[0].Contains("true");
            bool mask = param[1].Contains("true");

            Game.Manager.activity.LookupAny(EventType.WeeklyRaffle, out var act);
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIActivityWeeklyRaffleMain);

            if (act is ActivityWeeklyRaffle raffle)
            {
                if (ui != null && ui is UIActivityWeeklyRaffleMain main)
                {
                    int day = raffle.GetOffsetDay();
                    if (main.ItemMap.ContainsKey(day))
                    {
                        var trans = main.ItemMap[day].transform.Find("token");
                        if (block)
                        {
                            Game.Manager.guideMan.ActionSetBlock(block);
                        }

                        if (mask)
                        {
                            Game.Manager.guideMan.ActionShowMask(trans);
                        }
                    }
                }
            }


            _StopWait();
        }
    }
}