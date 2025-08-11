using System;
using FAT.MSG;
using fat.rawdata;
using UnityEngine.Serialization;


public static partial class DataTracker
{
    [Serializable]
    internal class mini_game_base : MergeCommonData
    {
        public MiniGameType mini_game_type;
        public int level_num;
        public int level_id;

        public void FillBase(MiniGameType type, int index, int id)
        {
            mini_game_type = type;
            level_num = index + 1;
            level_id = id;
        }
    }

    private class mini_game_enter : mini_game_base
    {
        public mini_game_enter Fill(MiniGameType type, int index, int id)
        {
            FillBase(type, index, id);
            return this;
        }
    }

    public static void TrackMiniGameStart(MiniGameType type, int index, int id)
    {
        _TrackData(_GetTrackData<mini_game_enter>().Fill(type, index, id));
    }

    private class mini_game_leave : mini_game_base
    {
        public mini_game_leave Fill(MiniGameType type, int index, int id)
        {
            FillBase(type, index, id);
            return this;
        }
    }

    public static void TrackMiniGameLeave(MiniGameType type, int index, int id)
    {
        _TrackData(_GetTrackData<mini_game_leave>().Fill(type, index, id));
    }

    [Serializable]
    private class mini_game_result : mini_game_base
    {
        public bool is_win;
        public int step_num;

        public mini_game_result Fill(MiniGameType type, int index, int id, bool result, int step)
        {
            FillBase(type, index, id);
            is_win = result;
            step_num = step;
            return this;
        }
    }

    public static void TrackMiniGameResult(MiniGameType type, int index, int id, bool result, int step)
    {
        _TrackData(_GetTrackData<mini_game_result>().Fill(type, index, id, result, step));
    }
}