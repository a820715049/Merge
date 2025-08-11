using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    [ExecuteInEditMode]
    public class MBGuideMaskAlphaBridge : MonoBehaviour
    {
        public Graphic target;
        private float offsetMainTexRef;
        public float offsetMainTex;

        public void OnValidate()
        {
            TryGetComponent<Graphic>(out target);
        }

        public void Update()
        {
            if (target == null || offsetMainTex == offsetMainTexRef) return;
            offsetMainTexRef = offsetMainTex;
            target.material.SetFloat("_AlphaAnim", offsetMainTex);
        }
    }
}