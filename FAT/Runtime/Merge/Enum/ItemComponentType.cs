/**
 * @Author: handong.liu
 * @Date: 2021-02-19 16:02:18
 */
namespace FAT.Merge
{
    public enum ItemComponentType
    {
        Merge,
        AutoSouce,
        ClickSouce,
        Chest,
        Bonus,
        TapBonus,
        Bubble,
        Dying,
        TimeSkipper,
        Box,
        FrozenOverride,
        Skill,
        FeatureEntry,
        EatSource,
        Eat,
        Activity,
        ToolSouce,
        OrderBox,
        JumpCD,
        SpecialBox,
        ChoiceBox,
        MixSource,
        TrigAutoSource, //触发式产棋子组件,有点击次数,每次点击都会换一下棋子图片,次数点满后死亡同时爆奖励,若棋盘空间不够则直接发往奖励箱
        ActiveSource,   //本身只含有产出次数等信息, 具体产出逻辑由活动控制

        //add type upwards!
        Count
    }
}