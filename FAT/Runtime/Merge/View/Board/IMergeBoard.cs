/*
 * @Author: qun.chao
 * @Date: 2021-02-19 10:51:00
 */
namespace FAT
{
    public interface IMergeBoard
    {
        void Init();
        void Setup(int w, int h);
        void Cleanup();
    }
}