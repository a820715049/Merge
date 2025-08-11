/**
 * @Author: handong.liu
 * @Date: 2021-02-02 18:36:14
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public class AppLoader : ILoadingProgress
{
    public bool isUpdateFinish => GameUpdateManager.Instance.state == GameUpdateManager.State.Finish;
    private enum State
    {
        Working,
        AskingRetry,
        TellingMaintenance,
        TellingForceUpdate
    }
    float ILoadingProgress.progress01 => mProgress;
    private State mState = State.Working;
    private float mUpdatingStartProgress = 0.2f;
    private float mUpdatingEndProgress = 0.8f;
    private float mProgress;
    private float mProgressTime = 2;
    private bool mWillFinish = false;

    public void SetWillFinish()
    {
        mWillFinish = true;
    }

    public void Update(float dt)
    {
        var mUpdater = GameUpdateManager.Instance;
        if(mUpdater == null || mUpdater.state == GameUpdateManager.State.Finish || mUpdater.state == GameUpdateManager.State.Idle)
        {
            mProgress += dt / mProgressTime;
            if(mUpdater == null || mUpdater.state == GameUpdateManager.State.Idle)
            {
                mProgress = Mathf.Min(mUpdatingStartProgress, mProgress);
            }
        }
        else
        {
            if(mUpdater != null)
            {
                mProgress = Mathf.Lerp(mUpdatingStartProgress, mUpdatingEndProgress, mUpdater.progress);
            }
        }
        if(!mWillFinish)
        {
            mProgress = Mathf.Clamp(mProgress, 0, 0.994f);
        }
        else
        {
            mProgress = Mathf.Clamp01(mProgress);
        }
        if(mUpdater != null && mState == State.Working)
        {
            if(mUpdater.state == GameUpdateManager.State.Error)
            {
                StateRetry(mUpdater);
            }
            else if(mUpdater.state == GameUpdateManager.State.ForceUpdate)
            {
                StateForceUpdate();
            }
            else if(mUpdater.state == GameUpdateManager.State.Maintenance)
            {
                StateMaintenance(mUpdater);
            }
        }
    }

    public void StateRetry(GameUpdateManager mUpdater)
    {
        mState = State.AskingRetry;
        // FAT_TODO
        // DreamMerge.UIUtility.ShowErrorMessage(GameUpdateManager.Instance.errorCode, () => {
        //     mState = State.Working;
        // });
        DebugEx.Error("AppLoader::StateRetry");
    }

    public void StateForceUpdate()
    {
        //force update
        mState = State.TellingForceUpdate;
        // FAT_TODO
        // UILoading.SetLoadingState(UILoading.LoadingPhase.AppForceUpdate);
        // Game.Instance.ShowAppForceUpdate();
        DebugEx.Error("AppLoader::StateForceUpdate");
    }

    public void StateMaintenance(GameUpdateManager mUpdater)
    {
        //maintenance
        mState = State.TellingMaintenance;
        // FAT_TODO
        // DreamMerge.UIUtility.ShowMessageBoxLoading("#SystemTip44", () => {
        //     mState = State.Working;
        // });
        DebugEx.Error("AppLoader::StateMaintenance");
    }
}