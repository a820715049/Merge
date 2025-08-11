#define NEED_DISK_CHECK
#if UNITY_WEBGL || UNITY_EDITOR
#undef NEED_DISK_CHECK
#endif
using CenturyGame.AppUpdaterLib.Runtime.Interfaces;
#if NEED_DISK_CHECK
using SimpleDiskUtils;
#endif

public class WrapperStorageInfoProvider : IStorageInfoProvider
{
    public int GetAvailableDiskSpace()
    {
#if NEED_DISK_CHECK
        return DiskUtils.CheckAvailableSpace();;
#else
        return int.MaxValue;
#endif
    }

    public int GetTotalDiskSpace()
    {
#if NEED_DISK_CHECK
        return DiskUtils.CheckTotalSpace();
#else
        return int.MaxValue;
#endif
    }

    public int GetBusyDiskSpace()
    {
#if NEED_DISK_CHECK
        return DiskUtils.CheckBusySpace();
#else
        return 0;
#endif
    }
}