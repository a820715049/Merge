/*
 * @Author: qun.chao
 * @Date: 2020-07-27 14:35:22
 */
namespace EL
{
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _Instance;
        public static T Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new T();
                return _Instance;
            }
        }
    }
}