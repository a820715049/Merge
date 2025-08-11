/*
 * @Author: qun.chao
 * @Date: 2021-05-28 10:20:18
 */
namespace FAT
{
    using System.Collections.Generic;
    using UnityEngine;
    using Merge;

    public class MBBoardView : MonoBehaviour
    {
        [SerializeField] private float sizeOfCell;
        [SerializeField] private RectTransform root;
        [SerializeField] private MBBoardBg bgCtrl;
        [SerializeField] private MBBoardFg fgCtrl;
        [SerializeField] private MBBoardSelector selectorCtrl;
        [SerializeField] private MBBoardDrag draggerCtrl;
        [SerializeField] private MBBoardItemHolder containerCtrl;
        [SerializeField] private MBBoardEffect effectCtrl;
        [SerializeField] private MBBoardIndicator indCtrl;
        [SerializeField] private RectTransform moveCtrl;
        [SerializeField] private RectTransform pairCtrl;
        [SerializeField] private RectTransform topEffectCtrl;
        [SerializeField] private BoardRes boardRes;

        public MBBoardBg boardBg => bgCtrl;
        public MBBoardFg boardFg { get { return fgCtrl; } }
        public MBBoardSelector boardSelector { get { return selectorCtrl; } }
        public MBBoardDrag boardDrag { get { return draggerCtrl; } }
        public MBBoardItemHolder boardHolder { get { return containerCtrl; } }
        public MBBoardEffect boardEffect { get { return effectCtrl; } }
        public MBBoardIndicator boardInd { get { return indCtrl; } }
        public RectTransform boardRoot { get { return root; } }
        public RectTransform moveRoot { get { return moveCtrl; } }
        public RectTransform pairRoot { get { return pairCtrl; } }
        public RectTransform topEffectRoot { get { return topEffectCtrl; } }
        public BoardRes BoardRes => boardRes;
        public float cellSize { get { return sizeOfCell; } }

        private List<IMergeBoard> mAllCompList = new List<IMergeBoard>();

        public void Setup()
        {
            (bgCtrl as IMergeBoard).Init();
            (fgCtrl as IMergeBoard).Init();
            (indCtrl as IMergeBoard).Init();
            (effectCtrl as IMergeBoard).Init();
            (draggerCtrl as IMergeBoard).Init();
            (selectorCtrl as IMergeBoard).Init();
            (containerCtrl as IMergeBoard).Init();
        }

        public void OnBoardEnter(MergeWorld world, MergeWorldTracer tracer)
        {
            boardRes.Install(world.activeBoard.boardId);
            BoardUtility.SetBoxAssets(world.activeBoard.boardId);
            BoardViewManager.Instance.OnBoardEnter(world, tracer, this);
        }

        public void OnBoardLeave()
        {
            BoardViewManager.Instance.OnBoardLeave();
        }
    }
}