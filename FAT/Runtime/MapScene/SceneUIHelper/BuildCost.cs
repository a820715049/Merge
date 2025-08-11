using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using EL;
using TMPro;

namespace FAT {
    public class BuildCost : MonoBehaviour {
        public UIImageState imgFrame;
        public UIImageRes imgIcon;
        public RectMask2D bar;
        public float size;
        public TextMeshProUGUI count;
        public Animation tick;
        public Button imgBtn;

        private int objId;
        
        #if UNITY_EDITOR

        public void OnValidate() {
            if (Application.isPlaying || GetComponentInParent<Canvas>() == null) return;
            var root = transform;
            imgFrame = root.FindEx<UIImageState>("frame");
            imgIcon = root.FindEx<UIImageRes>("icon");
            imgBtn = root.FindEx<Button>("icon");
            bar = root.FindEx<RectMask2D>("progress");
            size = bar.rectTransform.sizeDelta.y;
            count = root.FindEx<TextMeshProUGUI>("text");
            tick = root.FindEx<Animation>("tick");
        }

        #endif

        private void OnClickBtn()
        {
            if (Game.Manager.configMan.GetObjToolConfig(objId) != null)
                UIManager.Instance.OpenWindow(UIConfig.UIGetToolsHelp, objId);
        }
        
        public void Start()
        {
            if (imgBtn != null)
                imgBtn.onClick.AddListener(OnClickBtn);
        }

        public void Refresh(int id_, int v_, int req_, bool costOnlyText)
        {
            objId = id_;
            var objMan = Game.Manager.objectMan;
            var bConf = objMan.GetBasicConfig(id_);
            imgIcon.SetImage(bConf.Icon);
            var v = Mathf.Min(v_, req_);
            if (costOnlyText) count.text = $"{req_}";
            else count.text = $"{v}/{req_}";
            var f = size * v / req_;
            bar.rectTransform.sizeDelta = new(0, f);
            Tick(false);
        }

        public void Tick(bool v_) {
            count.gameObject.SetActive(!v_);
            tick.gameObject.SetActive(v_);
        }
    }
}