﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Util;
using System.Diagnostics;

public class Packager
{
    #region 枚举

    public enum EAndroidBuildPlatform
    {
        NoPlatform,
    }

    #endregion

    #region 菜单

    [MenuItem("UnityGameFramework/Build iPhone Resource", false, 100)]
    public static void BuildiPhoneResource()
    {
        BuildAssetResource(BuildTarget.iOS, AppPlatform.StreamingAssetsPath);
    }

    [MenuItem("UnityGameFramework/Build Android Resource", false, 101)]
    public static void BuildAndroidResource()
    {
        BuildAssetResource(BuildTarget.Android, AppPlatform.StreamingAssetsPath);
    }

    [MenuItem("UnityGameFramework/Build Windows Resource", false, 102)]
    public static void BuildWindowsResource()
    {
        BuildAssetResource(BuildTarget.StandaloneWindows, AppPlatform.StreamingAssetsPath);
    }

    [MenuItem("UnityGameFramework/PackageAllResource", false, 103)]
    public static void PackageAllResource()
    {
        BuildAssetResource(BuildTarget.StandaloneWindows, AppPlatform.GetPackageResPath(BuildTarget.StandaloneWindows));
        BuildAssetResource(BuildTarget.Android, AppPlatform.GetPackageResPath(BuildTarget.Android));
        BuildAssetResource(BuildTarget.iOS, AppPlatform.GetPackageResPath(BuildTarget.iOS));

        BuildTargetGroup curtargetgroup = AppPlatform.GetCurBuildTargetGroup();
        BuildTarget curtarget = AppPlatform.GetCurBuildTarget();
        EditorUserBuildSettings.SwitchActiveBuildTarget(curtargetgroup, curtarget);
    }

    public static void BuildAndroidNoPlatform()
    {
        string path = GetAndroidBuildPath(EAndroidBuildPlatform.NoPlatform);
        List<string> scenes = GetAllScenes();
        BuildPipeline.BuildPlayer(scenes.ToArray(), path, BuildTarget.Android, BuildOptions.None);
    }

    #endregion

    #region 函数

    /// <summary>
    /// 生成绑定素材
    /// </summary>
    private static void BuildAssetResource(BuildTarget target, string resPath)
    {
        // 1.
        if (Directory.Exists(AppPlatform.DataPath))
        {
            Directory.Delete(AppPlatform.DataPath, true);
        }
        
        if (Directory.Exists(resPath))
        {
            Directory.Delete(resPath, true);
        }
        Directory.CreateDirectory(resPath);
        AssetDatabase.Refresh();

        // 2.
        SetAllAssetBundleName();

        // 3.
        BuildPipeline.BuildAssetBundles(resPath, BuildAssetBundleOptions.None, target);

        // 4.复制配置文件
        CopyConfig(resPath);
        CopyLua(resPath);

        // 5.
        BuildFileIndex(resPath);
        AssetDatabase.Refresh();    
    }

    private static void SetAllAssetBundleName()
    {
        SetAssetBundleName(Application.dataPath + "/Prefabs");
    }

    private static void SetAssetBundleName(string path)
    {
        DirectoryInfo rootDirInfo = new DirectoryInfo(path);
        foreach (DirectoryInfo dirInfo in rootDirInfo.GetDirectories())
        {
            foreach (FileInfo pngFile in dirInfo.GetFiles("*.prefab", SearchOption.AllDirectories))
            {
                string source = pngFile.FullName.Replace("\\", "/");
                string assetpath = "Assets" + source.Substring(Application.dataPath.Length);
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetpath);
                string[] assetNames = assetpath.Split('/');
                string assetName = "";

                if (AppConst.IsEmptyResBundle)
                {
                    assetImporter.assetBundleName = "";
                    assetImporter.assetBundleVariant = "";
                }
                else
                {
                    assetName = assetNames[2] + "/" + assetNames[assetNames.Length - 2];
                    assetImporter.assetBundleName = assetName;
                    assetImporter.assetBundleVariant = "unity3d";
                }
            }
        }

        AssetDatabase.Refresh();
    }

    private static void BuildFileIndex(string resPath)
    {
        ///----------------------创建文件列表-----------------------
        string newFilePath = resPath + "/files.txt";
        if (File.Exists(newFilePath))
            File.Delete(newFilePath);

        List<string> files = new List<string>();
        Recursive(resPath, ref files);

        FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
        StreamWriter sw = new StreamWriter(fs);
        for (int i = 0; i < files.Count; i++)
        {
            string file = files[i];
            if (file.EndsWith(".meta") || file.Contains(".DS_Store"))
                continue;

            string md5 = Utility.Md5file(file);
            string value = file.Replace(resPath, string.Empty);
            sw.WriteLine(value + "|" + md5);
        }

        sw.Close();
        fs.Close();
    }

    /// <summary>
    /// 遍历目录及其子目录
    /// </summary>
    private static void Recursive(string path, ref List<string> files)
    {
        string[] names = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);

        foreach (string filename in names)
        {
            string ext = Path.GetExtension(filename);
            if (ext.Equals(".meta"))
                continue;

            files.Add(filename.Replace('\\', '/'));
        }

        foreach (string dir in dirs)
        {
            Recursive(dir, ref files);
        }
    }

    /// <summary>
    /// 复制配置文件夹
    /// </summary>
    private static void CopyConfig(string resPath)
    {
        string sourcepath = Application.dataPath + "/" + AppConst.ArtPath + "/" + AppConst.ConfigDirName;
        string dstpath = resPath + "/" + AppConst.ConfigDirName;
        FileUtil.CopyFileOrDirectory(sourcepath, dstpath);
    }

    /// <summary>
    /// 复制lua文件
    /// </summary>
    /// <param name="resPath"></param>
    private static void CopyLua(string resPath)
    {
        string sourcepath = Application.dataPath + "/" + AppConst.LuaDirName;
        string dstpath = resPath + "/" + AppConst.LuaDirName;
        FileUtil.CopyFileOrDirectory(sourcepath, dstpath);
    }

    /// <summary>
    /// 获取所有的场景
    /// </summary>
    /// <returns></returns>
    private static List<string> GetAllScenes()
    {
        List<string> scenes = new List<string>();
        scenes.Clear();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
                continue;

            scenes.Add(scene.path);
        }

        return scenes;
    }

    /// <summary>
    /// 获取android建造路径
    /// </summary>
    /// <param name="platform"></param>
    /// <returns></returns>
    private static string GetAndroidBuildPath(EAndroidBuildPlatform platform)
    {
        string prefix = "../Product/";
        if (platform == EAndroidBuildPlatform.NoPlatform)
        {
            return string.Format("{0}{1}.apk", prefix, AppConst.AppName);
        }

        return "";
    }

    #endregion
}
