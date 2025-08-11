/*
 * @Author: qun.chao
 * @Date: 2025-03-07 10:16:14
 */
using System;
using System.Collections.Generic;
using static FAT.UIBingoMain;

namespace FAT
{
    public class UIBingoTransitionHelper
    {
        private Dictionary<string, Action> transitionMap = new();
        private AnimGroup group;

        public void InitOnce(AnimGroup group_)
        {
            group = group_;
            BindTransition(State.Welcome, State.Select, Transition_Welcom_To_Select);
            BindTransition(State.Welcome, State.Main, Transition_Welcom_To_Main);
            BindTransition(State.Select, State.Main, Transition_Select_To_Main);
            BindTransition(State.Main, State.NextRound, Transition_Main_To_NextRound);
            BindTransition(State.NextRound, State.Main, Transition_NextRound_To_Main);
        }

        public void Transit(State from, State to)
        {
            if (transitionMap.TryGetValue($"{from}_{to}", out var action))
            {
                action?.Invoke();
            }
        }

        private void BindTransition(State from, State to, Action action)
        {
            transitionMap[$"{from}_{to}"] = action;
        }

        private void Transition_Welcom_To_Select()
        {
            group.talk.SetTrigger("Show");
            group.selectSpawner.SetTrigger("Show");
        }

        private void Transition_Welcom_To_Main()
        {
            group.spCurtain.AnimationState.SetAnimation(0, "hide", false);
            group.talk.SetTrigger("Hide");
            group.progress.SetTrigger("Show");
            group.spawner.SetTrigger("Show");
        }

        private void Transition_Select_To_Main()
        {
            group.spCurtain.AnimationState.SetAnimation(0, "hide", false);
            group.selectSpawner.SetTrigger("Hide");
            group.talk.SetTrigger("Hide");
            group.progress.SetTrigger("Show");
            group.spawner.SetTrigger("Show");
        }

        private void Transition_Main_To_NextRound()
        {
            group.spawner.SetTrigger("Hide");
            group.progress.SetTrigger("Hide");
            group.spCurtain.AnimationState.SetAnimation(0, "show", false);
            group.nextRound.SetTrigger("Show");
        }

        private void Transition_NextRound_To_Main()
        {
            group.nextRound.SetTrigger("Hide");
            group.spCurtain.AnimationState.SetAnimation(0, "hide", false);
            group.progress.SetTrigger("Show");
            group.spawner.SetTrigger("Show");
        }
    }
}