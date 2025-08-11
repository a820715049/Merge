/*
 * @Author: qun.chao
 * @Date: 2022-03-15 16:33:37
 */
using System.Collections;
using UnityEngine;

namespace FAT
{
    public class GuideActImpMoveCamera : GuideActImpBase
    {
        private void _StopWait()
        {
            mIsWaiting = false;
        }

        // FAT_TODO
        public override void Play(string[] param)
        {
            float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var _size);
            var _duration = 0f;
            if (param.Length > 2)
                float.TryParse(param[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _duration);
            // var scene = GameSceneManager.GetScene<SchoolScene>(GameSceneType.SchoolScene);
            // scene.helper.SetCameraMoveTo(param[0], _size, _duration, (b) => _StopWait());
            // GameHallManager.Instance.StartCoroutine(_CoImp(param[0], _size, _duration));
        }

        // private IEnumerator _CoImp(string name, float size, float duration)
        // {
        //     yield return new WaitUntil(() => GameSceneManager.IsSceneCreated(GameSceneType.SchoolScene));
        //     var scene = GameSceneManager.GetScene<SchoolScene>(GameSceneType.SchoolScene);
        //     scene.helper.SetCameraMoveTo(name, size, duration, (b) => _StopWait());
        // }
    }
}