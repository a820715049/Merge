/*
 * @Author: pengjian.zhang
 * @Date: 2024-02-29 14:18:19
 */

using EL;

namespace FAT.Merge
{
    public class ScoreMergeBonusHandler : IMergeBonusHandler
    {
        int IMergeBonusHandler.priority => 102;
        void IMergeBonusHandler.Process(Merge.MergeBonusContext context)
        {
            var score = context.result.config.MergeScore;
            if (score > 0)
            {
                MessageCenter.Get<MSG.ON_MERGE_HAS_SCORE_ITEM>().Dispatch(context.result, score);
            }
        }
        void IMergeBonusHandler.OnRegister()
        {

        }

        void IMergeBonusHandler.OnUnRegister()
        {

        }
    }
}