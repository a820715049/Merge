/*
 * @Author: qun.chao
 * @Date: 2023-10-12 14:45:42
 */
using static FAT.GameModuleManager.ModuleScope;

namespace FAT
{
    public class ModuleContainer
    {
        [ModuleMark(AppLaunch)] public AudioMan audioMan;
        [ModuleMark(AppLaunch)] public ConfigMan configMan;
        [ModuleMark(AppLaunch)] public CommonTipsMan commonTipsMan;
        [ModuleMark(AppLaunch)] public ArchiveMan archiveMan;
        [ModuleMark(ConfReady)] public UserGradeMan userGradeMan;
        [ModuleMark(ConfReady)] public IAP iap;
        [ModuleMark(ConfReady)] public CDKeyMan cdKeyMan;
        [ModuleMark(ConfReady)] public ObjectMan objectMan;
        [ModuleMark(ConfReady)] public HandbookMan handbookMan;
        [ModuleMark(ConfReady)] public RewardMan rewardMan;
        [ModuleMark(ConfReady)] public CoinMan coinMan;
        [ModuleMark(ConfReady)] public MergeLevelMan mergeLevelMan;
        [ModuleMark(ConfReady)] public MergeBoardMan mergeBoardMan;
        [ModuleMark(ConfReady)] public MergeItemMan mergeItemMan;
        [ModuleMark(ConfReady)] public MergeItemDifficultyMan mergeItemDifficultyMan;
        [ModuleMark(ConfReady)] public MainMergeMan mainMergeMan;
        [ModuleMark(ConfReady)] public FeatureUnlockMan featureUnlockMan;
        [ModuleMark(ConfReady)] public NpcMan npcMan;
        [ModuleMark(ConfReady)] public MainOrderMan mainOrderMan;
        [ModuleMark(ConfReady)] public GameNet.NetworkMan networkMan;
        [ModuleMark(ConfReady)] public BagMan bagMan;
        [ModuleMark(ConfReady)] public ItemInfoMan itemInfoMan;
        [ModuleMark(ArchiveLoaded)] public DeviceNotification notification;
        [ModuleMark(ArchiveLoaded)] public MergeEnergyMan mergeEnergyMan;
        [ModuleMark(ArchiveLoaded)] public PlayerGroupMan playerGroupMan;
        [ModuleMark(ArchiveLoaded)] public AccountMan accountMan;
        [ModuleMark(ArchiveLoaded)] public GuideMan guideMan;
        [ModuleMark(ArchiveLoaded)] public AdsMan adsMan;
        [ModuleMark(ArchiveLoaded)] public CommunityLinkMan communityLinkMan;
        //must be before than activity to receive de activity
        [ModuleMark(ArchiveLoaded)] public DailyEvent dailyEvent;
        [ModuleMark(ArchiveLoaded)] public CardMan cardMan;
        [ModuleMark(ArchiveLoaded)] public DecorateMan decorateMan;
        [ModuleMark(ArchiveLoaded)] public TaskManager taskMan;
        [ModuleMark(ArchiveLoaded)] public MiniBoardMan miniBoardMan;
        [ModuleMark(ArchiveLoaded)] public MiniBoardMultiMan miniBoardMultiMan;
        [ModuleMark(ArchiveLoaded)] public MineBoardMan mineBoardMan;
        [ModuleMark(ArchiveLoaded)] public PachinkoManager pachinkoMan;
        [ModuleMark(ArchiveLoaded)] public ActivityTrigger activityTrigger;
        [ModuleMark(ArchiveLoaded)] public Activity activity;
        //later than activity to be adjusted by activity
        [ModuleMark(ArchiveLoaded)] public ShopMan shopMan;
        //depend on activity
        [ModuleMark(ArchiveLoaded)] public MapSceneMan mapSceneMan;
        [ModuleMark(ArchiveLoaded)] public SpecialRewardMan specialRewardMan;
        [ModuleMark(ArchiveLoaded)] public RandomBoxMan randomBoxMan;
        [ModuleMark(ArchiveLoaded)] public MailMan mailMan;
        [ModuleMark(ArchiveLoaded)] public AutoGuide autoGuide;
        //ensure modules needed popup is initialized before this
        [ModuleMark(ArchiveLoaded)] public ScreenPopup screenPopup;
        [ModuleMark(ArchiveLoaded)] public SigninManager loginSignMan;
        [ModuleMark(ArchiveLoaded)] public MiniGameDataMan miniGameDataMan;
        [ModuleMark(ArchiveLoaded)] public RemoteApiMan remoteApiMan;
    }
}