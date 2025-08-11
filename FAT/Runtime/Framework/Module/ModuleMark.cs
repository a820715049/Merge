/*
 * @Author: qun.chao
 * @Date: 2023-10-18 13:06:55
 */
namespace FAT
{
    public class ModuleMark : System.Attribute
    {
        public GameModuleManager.ModuleScope Scope { get; private set; }
        public ModuleMark(GameModuleManager.ModuleScope s)
        {
            Scope = s;
        }
    }
}