/**
 * @Author: handong.liu
 * @Date: 2020-09-17 11:05:45
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public abstract class ResultedAsyncTask<T> : AsyncTaskBase
{
    public abstract T result { get; }
}

public class SimpleResultedAsyncTask<T> : ResultedAsyncTask<T>
{
    public static SimpleResultedAsyncTask<T> AlwaysSuccess
    {
        get {
            if(sAlwaysSuccess == null)
            {
                sAlwaysSuccess = new SimpleResultedAsyncTask<T>();
                sAlwaysSuccess.Success(default(T));
            }
            return sAlwaysSuccess;
        }
    }
    public static SimpleResultedAsyncTask<T> AlwaysFail {
        get {
            if(sAlwaysFail == null)
            {
                sAlwaysFail = new SimpleResultedAsyncTask<T>();
                sAlwaysFail.Fail();
            }
            return sAlwaysFail;
        }
    }
    private static SimpleResultedAsyncTask<T> sAlwaysSuccess;
    private static SimpleResultedAsyncTask<T> sAlwaysFail;
    public override T result {get {return mResult;}}
    public override string error => mError;
    public override long errorCode => mErrorCode;
    private T mResult;
    private string mError;
    private long mErrorCode;
    public void Success(T ret)
    {
        this.mResult = ret;
        TaskSuccess();
    }
    public void Fail(string error = null, long errCode = 0, ErrorCodeType errorType = ErrorCodeType.GameError)
    {
        this.mResult = default(T);
        this.mError = error;
        this.mErrorCode = ErrorCodeUtility.ConvertToCommonCode(errCode, errorType);
        TaskFail();
    }
    public void FailWithResult(T ret, string error = null, long errCode = 0, ErrorCodeType errorType = ErrorCodeType.GameError)
    {
        this.mResult = ret;
        this.mError = error;
        this.mErrorCode = ErrorCodeUtility.ConvertToCommonCode(errCode, errorType);
        TaskFail();
    }
}