/*
 * @Author: qun.chao
 * @Date: 2021-03-09 18:04:05
 */
using UnityEngine;

namespace FAT
{
    public class MBAutoDestroyOnDisable : MonoBehaviour
    {
        public float lifeTime;

        private void Update()
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime < 0f)
            {
                _DestroySelf();
            }
        }

        private void OnDisable()
        {
            _DestroySelf();
        }

        private void _DestroySelf()
        {

            Destroy(gameObject);
        }
    }
}