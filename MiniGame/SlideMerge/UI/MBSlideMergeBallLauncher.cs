/*
 * @Author: qun.chao
 * @Date: 2025-04-25 18:38:34
 */
using UnityEngine;
using UnityEngine.EventSystems;
using EL;

namespace MiniGame.SlideMerge
{
    public class MBSlideMergeBallLauncher : MonoBehaviour
    {
        [SerializeField] private Transform aimingAnchor;
        [SerializeField] private Transform aimingLine;
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private EventTrigger eventTrigger;
        [SerializeField] private float maxRot = 10.2f;  // 瞄准线最大偏转角度
        [SerializeField] private float maxOffset = 470f;    // 瞄准线最大偏移量

        public Transform SpawnRoot => spawnRoot;

        private UISlideMergeMain _main;
        private (GameObject view, Ball data) _nextBall = default;
        private float _nextSpawnTime;
        private float _spawnInterval = 1f;

        public void Setup(UISlideMergeMain main_)
        {
            _main = main_;

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entry.callback.AddListener(data => OnPointerRelease((PointerEventData)data));
            eventTrigger.triggers.Add(entry);

            entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(data => OnPointerUpdate((PointerEventData)data));
            eventTrigger.triggers.Add(entry);

            entry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            entry.callback.AddListener(data => OnPointerUpdate((PointerEventData)data));
            eventTrigger.triggers.Add(entry);
        }

        public void InitOnPreOpen()
        {
            RefreshAimingPos(0f);
            SetAimingLineStage(false);
            _nextSpawnTime = -1f;
            TrySpawnNextBall();
        }

        public void CleanupOnPostClose()
        {
            if (_nextBall != default)
            {
                _main.ReleaseBall(_nextBall.view, _nextBall.data);
                _nextBall = default;
            }
        }

        private void Update()
        {
            TrySpawnNextBall();
        }

        private void SetAimingLineStage(bool active)
        {
            aimingAnchor.gameObject.SetActive(active);
        }

        private void OnPointerRelease(PointerEventData eventData)
        {
            RefreshAimingPos(GetAimingOffset(eventData.position));
            TryLaunchBall();
        }

        private void OnPointerUpdate(PointerEventData eventData)
        {
            RefreshAimingPos(GetAimingOffset(eventData.position));
            _main.RefreshGuide(false);
        }

        private float GetAimingOffset(Vector2 screenPos)
        {
            var parent = aimingAnchor.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, null, out var localPos);
            return localPos.x;
        }

        private void RefreshAimingPos(float offsetX)
        {
            if (_nextBall == default)
                return;
            var radius = _nextBall.data.Radius;
            var left = -maxOffset + radius;
            var right = maxOffset - radius;
            var offset = Mathf.Clamp(offsetX, left, right);
            var rot = -offset / left * maxRot;
            aimingAnchor.localPosition = new Vector3(offset, 0f, 0f);
            aimingLine.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        private void TrySpawnNextBall()
        {
            if (_nextBall != default || Time.time < _nextSpawnTime)
                return;
            _nextBall = _main.SpawnNextBall();
            SetAimingLineStage(true);
            RefreshAimingPos(0f);
        }

        private void TryLaunchBall()
        {
            if (_nextBall == default)
                return;
            _main.LaunchBall(_nextBall.view, _nextBall.data);
            _nextBall = default;
            aimingAnchor.gameObject.SetActive(false);
            _nextSpawnTime = Time.time + _spawnInterval;
        }
    }
}