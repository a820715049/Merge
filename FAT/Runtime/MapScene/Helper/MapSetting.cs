using System.Collections.Generic;
using System;
using UnityEngine;

namespace FAT {
    public class MapSetting : MonoBehaviour {
        public Vector2 extent = new(10, 10);
        public float extend = 2;
        [Range(0, 1)]
        public float elasticity = 0.5f;
        public float zoomStep = 1f;
        public float zoomScrollStep = 1f;
        public float zoomTransitTime = 0.5f;
        [Range(0.01f, 0.5f)]
        public float dragInertiaThreshold = 0.05f;
        [Range(0f, 0.99f)]
        public float dragInertiaDamping = 0.85f;
        [Range(0.01f, 0.5f)]
        public float zoomInertiaThreshold = 0.2f;
        [Range(0f, 0.99f)]
        public float zoomInertiaDamping = 0.85f;
        public Camera cameraRef;
        public Vector2 cameraPos;
        public float cameraRest;
        public float cameraMin = 8f;
        public float cameraMax = 32f;
        public float zoomFocus = 9.8f;
        private Vector3 axisX;
        private Vector3 axisY;

        #if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying) return;
            CheckCamera();
            axisX = transform.right;
            axisY = transform.up;
        }

        public void CheckCamera() {
            if (cameraRef == null && transform.parent != null) {
                cameraRef = transform.parent.GetComponentInChildren<Camera>();
            }
            if (cameraRef != null) {
                cameraPos = cameraRef.transform.position;
                cameraRest = cameraRef.orthographicSize;
            }
        }

        public static void DrawRect(Vector3 pos, Vector3 sizeX, Vector3 sizeY) {
            Gizmos.DrawLine(pos + sizeX + sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos - sizeX + sizeY);
            Gizmos.DrawLine(pos + sizeX + sizeY, pos - sizeX + sizeY);
        }

        private void OnDrawGizmos() {
            if (!Application.isPlaying)
                CheckCamera();
            var pos = transform.position;
            var sizeX = extent.x * axisX;
            var sizeY = extent.y * axisY;
            DrawRect(pos, sizeX, sizeY);
            sizeX += extend * axisX;
            sizeY += extend * axisY;
            Gizmos.color = new(0.2f, 0.8f, 0.8f);
            DrawRect(pos, sizeX, sizeY);
            if (cameraRef != null) {
                pos = cameraRef.transform.position;
                var ratio = cameraRef.aspect;
                Gizmos.color = new(0.3f, 0.3f, 1f, 0.5f);
                sizeX = cameraMin * ratio * axisX;
                sizeY = cameraMin * axisY;
                DrawRect(pos, sizeX, sizeY);
                Gizmos.color = new(0.2f, 0.2f, 0.8f, 0.5f);
                sizeX = cameraMax * ratio * axisX;
                sizeY = cameraMax * axisY;
                DrawRect(pos, sizeX, sizeY);
                Gizmos.color = new(0.2f, 0.2f, 0.4f);
                var rest = cameraRef.orthographicSize;
                sizeX = rest * ratio * axisX;
                sizeY = rest * axisY;
                DrawRect(pos, sizeX, sizeY);
            }
        }
        #endif
    }
}