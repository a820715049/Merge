using System.Collections;
using System;
using UnityEngine;
using EL.Resource;
using Config;
using Spine.Unity;
using System.Collections.Generic;

namespace FAT {
    public class MapAnimation {
        public readonly List<SkeletonAnimation> spine = new();
        public readonly List<Animator> anim = new();
        public ColorFade fade;

        public void Setup(GameObject asset) {
            asset.GetComponentsInChildren(spine);
            asset.GetComponentsInChildren(anim);
            asset.TryGetComponent(out fade);
        }

        public void Clear() {
            spine.Clear();
            anim.Clear();
            fade = null;
        }

        public float PlaySpine(string name_, bool loop_) {
            var t = 0f;
            foreach(var s in spine) {
                if (s.IsNullOrDestroyed()) continue;
                s.loop = loop_;
                s.AnimationName = name_;
                t = Mathf.Max(t, s.AnimationState.GetCurrent(0).AnimationEnd);
            }
            return t;
        }

        public float PlayAnim(string name_, bool loop_) {
            var t = 0f;
            foreach(var a in anim) {
                a.Play(name_);
                var aS = a.GetCurrentAnimatorStateInfo(0);
                if (!aS.IsName(name_)) aS = a.GetNextAnimatorStateInfo(0);
                t = Mathf.Max(t, aS.length);
            }
            return t;
        }

        public float Play(string name_, bool loop_) {
            var v1 = PlaySpine(name_, loop_);
            PlayAnim(name_, loop_);
            return v1;
        }

        public void Preview(bool v_) {
            if (fade != null)
                fade.Preview(v_);
        }

        public void PauseSpineAnim(bool isPause)
        {
            foreach(var s in spine) 
            {
                if (s.IsNullOrDestroyed()) 
                    continue;
                //timeScale设成0 代表暂停动画
                s.timeScale = isPause ? 0 : 1;
            }
        }
    }
}