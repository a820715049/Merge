/**
 * @Author: xiaochun.liu
 * @Date: 2023-10-30 18:29:45
 */
using System.IO;
using CenturyGame.Framework.ConfigData;
using System.Threading.Tasks;

public static class ProtoFileUtility
{
    public static byte[] Decrypt(byte[] data)
    {
        var key = System.BitConverter.ToString(Constant.kUserLengthLength1).Replace("-", System.String.Empty).ToLower();
        var bytes = ConfigHelper.GetDecryptTable(data, key, Constant.kUserMagic, true);
        return bytes;
    }

    public static byte[] Read(string file)
    {
        if (File.Exists(file))
        {
            var data = File.ReadAllBytes(file);
            var key = System.BitConverter.ToString(Constant.kUserLengthLength1).Replace("-", System.String.Empty).ToLower();
            var bytes = ConfigHelper.GetDecryptTable(data, key, Constant.kUserMagic, true);
            return bytes;
        } else {
            EL.DebugEx.Error($"File not found {file}");
            throw new FileNotFoundException("File not found: " + file);
        }
    }

    public static async Task Load(string file, System.Action<Google.Protobuf.CodedInputStream> cb)
    {
        var data = await Task.Run(() => Read(file));
        cb.Invoke(new Google.Protobuf.CodedInputStream(data));
    }
}