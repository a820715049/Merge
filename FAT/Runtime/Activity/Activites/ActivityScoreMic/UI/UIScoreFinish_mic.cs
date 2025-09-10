using EL;

namespace FAT
{
    public class UIScoreFinish_mic : UIBase
    {
        protected override void OnCreate()
        {
            transform.AddButton("Content/ConfirmBtn", Close);
        }
    }
}