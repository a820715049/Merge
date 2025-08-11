/*
 * @Author: qun.chao
 * @Date: 2024-07-03 11:29:47
 */
using System.Collections.Generic;
using fat.rawdata;

namespace FAT.Merge
{
    public class ItemChoiceBoxComponent : ItemComponentBase
    {
        public static List<int> outputResults = new();
        public ComMergeChoiceBox config => mConfig;
        private ComMergeChoiceBox mConfig = null;

        public static bool Validate(ItemComConfig config)
        {
            return config?.choiceBoxConfig != null;
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).choiceBoxConfig;
        }

        public void CalcChoiceBoxOutput()
        {
            outputResults.Clear();
            Game.Manager.mergeItemDifficultyMan.CalcChoiceBoxOutput(config.ActDiffRange[0],
                                                                     config.ActDiffRange[1],
                                                                     outputResults,
                                                                     config.ItemNum);
        }
    }
}