using fat.rawdata;

namespace FAT
{
    public class SigninPop : IScreenPopup
    {
        public void Setup(Popup popupConf, UIResource uiResource, int popupId)
        {
            PopupConf = popupConf;
            PopupRes = uiResource;
            PopupId = popupId;
        }
    }
}