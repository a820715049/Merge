using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using EL;
using UnityEngine.Rendering;
using DG.Tweening;
using Spine.Unity;
using UnityEngine.Assertions.Must;

namespace FAT {
    public class TransitionUpgrade : MonoBehaviour {
        public struct Wall {
            public GameObject obj;
            public Transform root;
            public SkeletonAnimation anim;
            public MeshRenderer sprite;
        }

        public struct Cloud {
            public GameObject obj;
            public Transform root;
            public SortingGroup sort;
            public ParticleSystem ps1;
            public ParticleSystem ps2;
        }

        public class WallGroup {
            public Transform rootBL;
            public Transform rootBR;
            public Transform rootTL;
            public Transform rootTR;
            public SortingGroup sortBL;
            public SortingGroup sortBR;
            public SortingGroup sortTL;
            public SortingGroup sortTR;
            public List<Wall> wallBL = new();//A
            public List<Wall> wallBR = new();//B
            public List<Wall> wallTL = new();//C
            public List<Wall> wallTR = new();//D
            public int CountT => wallBL.Count;
            public int count;

            public void Init(Transform root_, string n_) {
                static void SetupWall(List<Wall> list_, Transform root_) {
                    list_.Clear();
                    if(root_ != null) {
                        var w = ParseWall(root_.gameObject);
                        w.obj.SetActive(false);
                        w.anim.loop = false;
                        list_.Add(w);
                    }
                }
                rootBL = root_.Find($"BL{n_}");
                rootBR = root_.Find($"BR{n_}");
                rootTL = root_.Find($"TL{n_}");
                rootTR = root_.Find($"TR{n_}");
                rootBL.Access(out sortBL);
                rootBR.Access(out sortBR);
                rootTL.Access(out sortTL);
                rootTR.Access(out sortTR);
                SetupWall(wallBL, rootBL.Find("a"));
                SetupWall(wallBR, rootBR.Find("b"));
                SetupWall(wallTL, rootTL.Find("c"));
                SetupWall(wallTR, rootTR.Find("d"));
            }

            public void Resize(int n_, Vector2 size_, Vector2 offset_, float oY_) {
                count = n_;
                var hX = n_ * size_.x + offset_.x;
                var hY = n_ * size_.y + offset_.y;
                rootBL.localPosition = new(0, -hY + oY_);
                rootBR.localPosition = new(hX, oY_);
                rootTL.localPosition = new(-hX, oY_);
                rootTR.localPosition = new(0, hY + oY_);
            }

            public void Refresh(int count_, int order_, int of_, Vector2 size_) {
                static void Create(List<Wall> list_) {
                    var template = list_[0];
                    var obj = GameObject.Instantiate(template.obj, template.root.parent);
                    var t = ParseWall(obj);
                    list_.Add(t);
                }
                static void Create4(WallGroup g_) {
                    Create(g_.wallBL);
                    Create(g_.wallBR);
                    Create(g_.wallTL);
                    Create(g_.wallTR);
                }
                static Wall Use(Wall wall_, int order_) {
                    wall_.sprite.sortingOrder = order_;
                    return wall_;
                }
                static (Wall, Wall, Wall, Wall) Use4(WallGroup g_, int n_) {
                    var bl = Use(g_.wallBL[n_], g_.count - n_);
                    var br = Use(g_.wallBR[n_], n_);
                    var tl = Use(g_.wallTL[n_], n_);
                    var tr = Use(g_.wallTR[n_], n_);
                    return (bl, br, tl, tr);
                }
                static void Disable4(WallGroup g_, int n_) {
                    g_.wallBL[n_].obj.SetActive(false);
                    g_.wallBR[n_].obj.SetActive(false);
                    g_.wallTL[n_].obj.SetActive(false);
                    g_.wallTR[n_].obj.SetActive(false);
                }
                sortBL.sortingOrder = order_ + (2 + of_);
                sortBR.sortingOrder = order_ + (3 + of_);
                sortTL.sortingOrder = order_ - (2 + of_);
                sortTR.sortingOrder = order_ - (1 + of_);
                for (var n = wallBL.Count; n < count_; ++n) {
                    Create4(this);
                }
                for (var n = 0; n < count_; ++n) {
                    var (bl, br, tl, tr) = Use4(this, n);
                    bl.root.localPosition = new(-size_.x * n, size_.y * n);
                    br.root.localPosition = new(-size_.x * n, -size_.y * n);
                    tl.root.localPosition = new(size_.x * n, size_.y * n);
                    tr.root.localPosition = new(size_.x * n, -size_.y * n);
                }
                for (var n = 0; n < wallBL.Count; ++n) {
                    Disable4(this, n);
                }
            }

            public (IEnumerator, IEnumerator) AnimateIn(string n_, float i_, float i1_, float d_, out float t_) {
                static void A(Wall wall_, string anim_) {
                    wall_.obj.SetActive(true);
                    wall_.anim.AnimationName = anim_;
                }
                IEnumerator R1() {
                    var wait = new WaitForSeconds(i_);
                    for(var n = count - 1; n >= 0; --n) {
                        A(wallBL[n], n_);
                        yield return wait;
                    }
                    for(var n = count - 1; n >= 0; --n) {
                        A(wallBR[n], n_);
                        yield return wait;
                    }
                }
                IEnumerator R2() {
                    var waitD = new WaitForSeconds(i1_);
                    var wait = new WaitForSeconds(i_);
                    yield return waitD;
                    for(var n = count - 1; n >= 0; --n) {
                        A(wallTL[n], n_);
                        yield return wait;
                    }
                    for(var n = count - 1; n >= 0; --n) {
                        A(wallTR[n], n_);
                        yield return wait;
                    }
                }
                t_ = i1_ + count * (i_ + d_) - i_;
                return (R1(), R2());
            }

            public void AnimateOut(string n_) {
                for (var n = 0; n < count; ++n) {
                    wallBL[n].anim.AnimationName = n_;
                    wallBR[n].anim.AnimationName = n_;
                    wallTL[n].anim.AnimationName = n_;
                    wallTR[n].anim.AnimationName = n_;
                }
            }
        }

        private Cloud cloud;
        private readonly WallGroup wall1 = new();
        private readonly WallGroup wall2 = new();
        public Vector2 size1;
        public Vector2 size2;
        public Vector2 offset1;
        public Vector2 offset2;
        public float wallInDelay = 1.0f;
        public float soldDelay = 0.3f;
        public float soldWait = 0.2f;
        public float centerOffset = 0.65f;
        public float wall2Offset = 0.4f;
        public int padding1 = 1;
        public int padding2 = 1;
        public Vector4 cloudEmission;
        public float cloudWait;
        public float cloudBaseSegment;
        public string wallIn = "pf_deco_build_appear";
        public string wallOut = "pf_deco_build_disappear";
        public float wall1IntervalIn;
        public float wall1DurationIn;
        public float wall2IntervalIn;
        public float wall2DurationIn;
        public float wall2DelayIn;
        public float outWait = 1.8f;
        private int count1;
        private int count2;

        private void OnValidate() {
            if (Application.isPlaying) return;
            var template1 = ParseWall(transform.TryFind("BL/a"));
            var template2 = ParseWall(transform.TryFind("BL2/a"));
            static void Check(SkeletonAnimation target_, string in_, ref float dIn_) {
                var clip = target_.skeletonDataAsset.GetSkeletonData(false).Animations;
                foreach (var c in clip) {
                    if (c.Name == in_) {
                        dIn_ = c.Duration;
                        break;
                    }
                }
            }
            Check(template1.anim, wallIn, ref wall1DurationIn);
            Check(template2.anim, wallIn, ref wall2DurationIn);
        }

        public void Awake() {
            var objCloud = transform.TryFind("cloud");
            var rootCloud = objCloud.transform;
            cloud = new() {
                obj = objCloud, root = rootCloud,
                sort = rootCloud.GetComponent<SortingGroup>(),
                ps1 = rootCloud.FindEx<ParticleSystem>("eff"),
                ps2 = rootCloud.FindEx<ParticleSystem>("eff/cloud_small"),
            };
            wall1.Init(transform, string.Empty);
            wall2.Init(transform, "2");
        }

        private static Wall ParseWall(GameObject obj_) {
            var root = obj_.transform;
            return new() {
                obj = obj_, root = root,
                anim = root.Access<SkeletonAnimation>("n"),
                sprite = root.Access<MeshRenderer>("n"),
            };
        }

        public void Resize(Bounds bounds, int order_) {
            var hSize = bounds.extents;
            var n1 = Mathf.CeilToInt(hSize.x / size1.x);
            count1 = n1 + padding1;
            var n2 = Mathf.CeilToInt(hSize.x / size2.x);
            count2 = n2;
            var hY1 = count1 * size1.y;
            var hY2 = count2 * size2.y;
            if (hY2 - hY1 < wall2Offset) {
                count2 += padding2;
                hY2 = count2 * size2.y;
            }
            var oY = hY2 - hY2 - wall2Offset;
            transform.position = bounds.center + new Vector3(0, size1.y * n1 - hSize.y * 2 * centerOffset, 0);
            wall1.Resize(count1, size1, offset1, 0);
            wall2.Resize(count2, size2, offset2, oY);
            var sCloud = count1 / cloudBaseSegment;
            cloud.root.localScale = Vector3.one * sCloud;
            cloud.sort.sortingOrder = order_ + 1;
            wall1.Refresh(count1, order_, 0, size1);
            wall2.Refresh(count2, order_, 3, size2);
        }

        private void UpdatePS(ParticleSystem ps_, int rate_) {
            var e = ps_.emission;
            e.rateOverTime = rate_;
        }

        public IEnumerator AnimateSold(Animation sold_) {
            var waitS1 = new WaitForSeconds(soldDelay);
            var waitS2 = new WaitForSeconds(sold_.clip.length + soldWait);
            yield return waitS1;
            sold_.Play();
            yield return waitS2;
        }

        public IEnumerator AnimateIn() {
            yield return new WaitForSeconds(wallInDelay);
            var (a11, a12) = wall1.AnimateIn(wallIn, wall1IntervalIn, 0, wall1DurationIn, out var t1);
            StartCoroutine(a11);
            StartCoroutine(a12);
            var (a21, a22) = wall2.AnimateIn(wallIn, wall2IntervalIn, wall2DelayIn, wall1DurationIn, out var t2);
            StartCoroutine(a21);
            StartCoroutine(a22);
            var waitI = new WaitForSeconds(Mathf.Max(cloudWait, Mathf.Max(t1, t2)));
            UpdatePS(cloud.ps1, (int)cloudEmission.x);
            UpdatePS(cloud.ps2, (int)cloudEmission.y);
            cloud.obj.SetActive(false);
            cloud.obj.SetActive(true);
            // 建造音效
            Game.Manager.audioMan.TriggerSound("HouseComplete");
            yield return waitI;
        }

        public IEnumerator AnimateOut() {
            yield return null;
            var waitO = new WaitForSeconds(outWait);
            UpdatePS(cloud.ps1, 0);
            UpdatePS(cloud.ps2, 0);
            wall1.AnimateOut(wallOut);
            wall2.AnimateOut(wallOut);
            yield return waitO;
        }
    }
}