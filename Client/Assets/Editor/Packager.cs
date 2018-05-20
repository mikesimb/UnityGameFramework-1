﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Util;

public class Packager
{
    #region 菜单

    [MenuItem("UnityGameFramework/Build iPhone Resource", false, 100)]
    public static void BuildiPhoneResource()
    {
        BuildAssetResource(BuildTarget.iOS);
    }

    [MenuItem("UnityGameFramework/Build Android Resource", false, 101)]
    public static void BuildAndroidResource()
    {
        BuildAssetResource(BuildTarget.Android);
    }

    [MenuItem("UnityGameFramework/Build Windows Resource", false, 102)]
    public static void BuildWindowsResource()
    {
        BuildAssetResource(BuildTarget.StandaloneWindows);
    }

    #endregion

    #region 变量



    #endregion

    #region 函数

    /// <summary>
    /// 生成绑定素材
    /// </summary>
    private static void BuildAssetResource(BuildTarget target)
    {
        // 1.
        if (Directory.Exists(AppPlatform.DataPath))
        {
            Directory.Delete(AppPlatform.DataPath, true);
        }

        string streamPath = AppPlatform.StreamingAssetsPath;
        if (Directory.Exists(streamPath))
        {
            Directory.Delete(streamPath, true);
        }
        Directory.CreateDirectory(streamPath);
        AssetDatabase.Refresh();

        // 2.
        SetAllAssetBundleName();

        // 3.
        string resPath = AppPlatform.StreamingAssetsPath;
        BuildPipeline.BuildAssetBundles(resPath, BuildAssetBundleOptions.None, target);
        
        BuildFileIndex();
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

                assetName = assetNames[2] + "/" + assetNames[assetNames.Length - 2];
                assetImporter.assetBundleName = assetName;
                assetImporter.assetBundleVariant = "unity3d";
            }
        }

        AssetDatabase.Refresh();
    }

    private static void BuildFileIndex()
    {
        string resPath = AppPlatform.StreamingAssetsPath;
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

    #endregion
}
