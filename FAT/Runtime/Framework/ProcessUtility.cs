/*
 * @Author: qun.chao
 * @Date: 2023-10-20 12:32:30
 */
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 处理外部进程有关逻辑 从CommonEditorUtility类提取出
/// </summary>
public static class ProcessUtility
{
    static string OptimazeNativePath(string path, bool getFullPath = true)
    {
        if (getFullPath)
            path = Path.GetFullPath(path);
        path = path.Replace(@"\", @"/");
        return path;
    }

    static string FindExePath(string name, out List<string> paths)
    {
        var str = System.Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(str))
        {
            str = System.Environment.GetEnvironmentVariable("path");
        }
        if (string.IsNullOrEmpty(str))
        {
            paths = new List<string>();
            return "";
        }
        var index = str.IndexOf("!");
        if (index >= 0)
        {
            str = str.Substring(index + 1);
        }
        var pathes = paths = str.Split(System.IO.Path.PathSeparator).ToList();
#if UNITY_EDITOR_WIN
#else
        pathes.Add("/usr/local/bin");
        pathes.Add("/opt/homebrew/bin");
#endif
        var chosen = pathes.Select(x => System.IO.Path.Combine(x, name)).Where(x => System.IO.File.Exists(x)).FirstOrDefault();
        if (string.IsNullOrEmpty(chosen))
        {
#if UNITY_EDITOR_WIN
            name = name + ".exe";
            chosen = pathes.Select(x => System.IO.Path.Combine(x, name)).Where(x => System.IO.File.Exists(x)).FirstOrDefault();
#endif
        }
        return chosen;
    }

    public static int StartShellWithOutputs(string cmd, string args, string wd, System.Func<string, bool> onOutput = null)
    {
#if UNITY_EDITOR_WIN
        args = $"/c '{cmd} {args}'";
        cmd = "cmd";
#else
        args = $"-c '{cmd} {args}'";
        cmd = "bash";
#endif
        Debug.LogFormat("CommonEditorUtility::StartProcess ----> call shell {0}", args);
        return StartProcess(cmd, args, wd, false, onOutput);
    }

    // onOutput: 返回false代表此行会在最终结果出现
    public static int StartProcess(string exe, string args, string workDir, bool shell = false, System.Func<string, bool> onOutput = null)
    {
        string exePath = exe;
        List<string> triedPath = null;
        if (!shell)
        {
            exePath = FindExePath(exePath, out triedPath);
        }
        if (string.IsNullOrEmpty(exePath))
        {
            Debug.LogErrorFormat("CommonEditorUtility::StartProcess ----> no exe found {0}, paths: {1}", exe, string.Join(",", triedPath));
            return 1;
        }
        Debug.LogFormat("CommonEditorUtility::StartProcess ----> start {0} in {1} {2} [wd={3}]", exe, exePath, args, workDir);
        var pStartInfo = new System.Diagnostics.ProcessStartInfo();
        pStartInfo.FileName = exePath;
        pStartInfo.UseShellExecute = shell;
        pStartInfo.RedirectStandardInput = !shell;
        pStartInfo.RedirectStandardOutput = !shell;
        pStartInfo.RedirectStandardError = !shell;
        var env = System.Environment.GetEnvironmentVariables();
        foreach (var k in env.Keys)
        {
            if (k is string keyStr && env[k] is string valStr)
            {
                if (keyStr.ToLower() == "path")
                {
                    var index = valStr.IndexOf("!");
                    if (index >= 0)
                    {
                        valStr = valStr.Substring(index + 1);
                    }
                }
                pStartInfo.EnvironmentVariables[keyStr] = valStr;
            }
        }

        System.Environment.GetEnvironmentVariables();
        workDir = OptimazeNativePath(workDir);
        pStartInfo.WorkingDirectory = workDir;
        pStartInfo.CreateNoWindow = true;
        pStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        pStartInfo.Arguments = args;
        pStartInfo.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
        pStartInfo.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;

        var proces = System.Diagnostics.Process.Start(pStartInfo);
        var outputAll = "";
        while (!proces.StandardOutput.EndOfStream)
        {
            string line = proces.StandardOutput.ReadLine();
            if (onOutput == null || !onOutput.Invoke(line))
            {
                outputAll += line + "\n";
            }
        }
        var err = proces.StandardError.ReadToEnd();
        proces.WaitForExit();
        var ret = proces.ExitCode;
        if (ret == 0)
        {
            Debug.Log($"{exe} exit with code {ret} \n stdout: {outputAll} \n stderr: {err}");
        }
        else
        {
            Debug.LogError($"{exe} exit with code {ret} \n stdout: {outputAll} \n stderr: {err}");
        }

        proces.Close();
        return ret;
    }

}