/**
 * @Author: handong.liu
 * @Date: 2021-03-29 15:52:06
 */
using fat.rawdata;


using System;
namespace Config
{
	[Serializable]
	public class CareerLevelConfig
	{
		public int Id;
		public string Name;
		public string Desc;
		public int RoleLevel;
		public int ItemCount;
		public int Popular;
		public int LevelBase;
		public string Icon;
		public int RolePartSetId;
	}
	[Serializable]
	public class AssetConfig
	{
		public string Group;
		public string Asset;
        public string Key {
            get {
                if(string.IsNullOrEmpty(mKey))
                {
                    mKey = string.Format("{0}:{1}", Group, Asset);
                }
                return mKey;
            }
            set {
                mKey = value;
            }
        }
        private string mKey;
	}
	[Serializable]
	public class StyleAddConfig
	{
		public int Id;
		public int Add;
	}
	[Serializable]
	public class ColorConfig
	{
		public int H;
		public int S;
		public int V;
		public int Idx;
	}
	[Serializable]
	public class RewardConfig
	{
		public int Id;
		public int Count;
	}

	[Serializable]
	public class MergeGridItem
	{
		public int Id;
		public int State;
		public int Param;
	}
	
	[Serializable]
	public class RandomBoxShowReward
	{
		public int Id;
		public int MinCount;
		public int MaxCount;
	}
	
	[Serializable]
	public class GuideMergeRequire
	{
		public GuideMergeRequireType Type;
		public int Value;
		public int Extra;
	}
	[Serializable]
	public class CareerItemConfig
	{
		public int Id;
		public int CareerLevel;
		public string Name;
		public string Desc;
		public CareerItemType Type;
		public float BaseValue;
		public float ValueAdd;
		public float BasePrice;
		public float PriceMultiply;
		public int Count;
		public string Icon;
	}
	[Serializable]
	public class TipsConfig
	{
		public int Time;
		public string Content;
	}
	[Serializable]
	public class DailyBonusConfig
	{
		public int DayNum;
		public int CareerId;
		public int ItemId;
	}

	[Serializable]
	public class RoundsArrayConfig
	{
		public int[] RoundsArray;
	}

	//TODO: remove
	public enum ItemType
	{
		None = 0,		//非道具
		SweepSpeedup = 1,		//加速打扫
	}
	[Serializable]
	public class ObjItemConfig
	{
		public int Id;
		public ItemType Type;
	}
    //TODO: remove
	[Serializable]
	public class ObjCareerConfig
	{
		public int Id;
		public string Introduce;
		public string Name;
		public string ShowAction;
		public string Weapon;
		public int[] DefaultWallPaper;
		public int[] DefaultFloor;
		public string BtnIcon;
		public string WeaponIcon;
		public int MentorId;
		public string MentorIcon;
		public string Music;
	}
    //TODO: check
	[Serializable]
	public class GachaConfig
	{
		public int Id;
		public int Weight;
	}
    //TODO: remove
	[Serializable]
	public class CareerLevelTabConfig
	{
		public int StartLevel;
		public int EndLevel;
		public string Icon;
	}
    //TODO: remove
	public enum CareerItemType
	{
		Purchase = 0,		//直接获得物品
		AddIncome = 1,		//秒产加成（万分之几）
		DecClothPrice = 2,		//减少服装售价（万分之几）
		MoreFame = 3,		//获得声望（万分之几）
		DecFurniturePrice = 4,		//减少家具售价（万分之几）
		NewCareer = 5,		//新职业开放（每次一个）
		AllCharacterAddIncome = 6,		//所有角色收入增加(万分之一）
		IntimacyAdd = 7,		//宠物交互增加亲密度
	}
    
    [Serializable]
    public class CoordConfig
    {
        public int Col; //列 x
        public int Row; //行 y
    }
    
    [Serializable]
    public class IntRangeConfig
    {
        public int Min; //最小值
        public int Max; //最大值
    }
}