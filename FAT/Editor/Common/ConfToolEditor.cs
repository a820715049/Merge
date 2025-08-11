using UnityEditor;
using FAT;

public static class ConfToolEditor
{
    // [MenuItem("Tools/Conf/Sync Conf")]
    // public static void SyncConf()
    // {
    //     ConfTool.SyncConf();
    // }

    [MenuItem("Tools/Conf/Sync Proto")]
    public static void SyncProto()
    {
        ConfTool.SyncProto();
    }
}
