/*
 * @Author: qun.chao
 * @Date: 2023-10-23 18:31:17
 */
using fat.gamekitdata;

namespace FAT
{
    public interface IUserDataHolder
    {
        void SetData(LocalSaveData archive);
        void FillData(LocalSaveData archive);
    }
    public interface IPreSetUserDataListener            //注意！这个接口在upgradedata之前调用，因此它的实现如果发生存档改变，必须在代码里处理，不能依赖IUserDataVersionUpgrader
    {
        void OnPreSetUserData(LocalSaveData archive);
    }
    public interface IPostSetUserDataListener
    {
        void OnPostSetUserData();
    }
    public interface IServerDataReceiver
    {
        void OnReceiveServerData(ServerData data);
    }
    public interface IUserDataVersionUpgrader
    {
        void OnDataVersionUpgrade(LocalSaveData src, LocalSaveData dst);
    }
    public interface IDeltaUserDataModifier
    {
        void ModifyDeltaUser(LocalSaveData data, LocalSaveData oldData);
    }
    public interface IUserDataInitializer
    {
        void InitUserData();
    }
}