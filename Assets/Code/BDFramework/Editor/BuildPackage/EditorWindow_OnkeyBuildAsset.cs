﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.Helper;
using BDFramework.Editor;
using BDFramework.Editor.BuildPackage;
using UnityEditor;
using UnityEngine;

public class EditorWindow_OnkeyBuildAsset : EditorWindow
{
    private EditorWindow_Table editorTable;

    private EditorWindow_ScriptBuildDll editorScript;
    private EditorWindow_GenAssetBundle editorAsset;
   public void Show()
   {
      this.editorTable  = new EditorWindow_Table();
      this.editorAsset  = new EditorWindow_GenAssetBundle();
      this.editorScript = new EditorWindow_ScriptBuildDll();
       
       this.minSize = this.maxSize = new Vector2(1050,600);
       base.Show();
   }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        {
            if (editorScript != null)
            {            
                GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                
                editorScript.OnGUI();
                GUILayout.EndVertical();
                Layout_DrawLineV(Color.white);
            }
            if (editorAsset != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                editorAsset.OnGUI();
                GUILayout.EndVertical();
                Layout_DrawLineV(Color.white);
            }
            if (editorTable != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(350), GUILayout.Height(220));
                editorTable.OnGUI();
                GUILayout.EndVertical();
                Layout_DrawLineV(Color.white);
            }
        }
        GUILayout.EndHorizontal();

        Layout_DrawLineH(Color.white);
        OnGUI_OneKeyExprot();
    }


    public string exportPath;
    private bool isGenWindowsAssets = true;
    private bool isGenIosAssets     = true;
    private bool isGenAndroidAssets = true;
    public void OnGUI_OneKeyExprot()
    {
        GUILayout.BeginVertical();
        {
            GUILayout.Label("注:上面按钮操作,会默认生成到StreamingAssets", GUILayout.Width(500), GUILayout.Height(30));
            isGenWindowsAssets = GUILayout.Toggle(isGenWindowsAssets, "生成Windows资源");
            isGenIosAssets     = GUILayout.Toggle(isGenIosAssets, "生成Ios资源");
            isGenAndroidAssets = GUILayout.Toggle(isGenAndroidAssets, "生成Android资源");
            //
            GUILayout.Label("导出地址:" + exportPath, GUILayout.Width(500));
            //
            if (GUILayout.Button("一键导出", GUILayout.Width(350), GUILayout.Height(30)))
            {
                //选择目录
                exportPath = EditorUtility.OpenFolderPanel("选择导出目录", Application.dataPath.Replace("Assets",""), "");
                {
                    //生成windows资源
                    if (isGenWindowsAssets)
                    {
                        var outPath = exportPath+"/"+Utils.GetPlatformPath(RuntimePlatform.WindowsPlayer);
                        //1.编译脚本
                        ScriptBiuldTools.GenDllByMono(Application.dataPath,outPath);
                        //2.打包资源
                        AssetBundleEditorTools.GenAssetBundle("Resource/Runtime/",outPath, BuildTarget.StandaloneWindows);
                        //3.打包表格
                        Excel2SQLiteTools.GenSQLite(outPath);                       
                    }

                    //生成android资源
                    if (isGenAndroidAssets)
                    {
                        
                        var outPath = exportPath+"/"+Utils.GetPlatformPath(RuntimePlatform.Android);
                        //1.编译脚本
                        ScriptBiuldTools.GenDllByMono(Application.dataPath,outPath);
                        //2.打包资源
                        AssetBundleEditorTools.GenAssetBundle("Resource/Runtime/",outPath, BuildTarget.Android);
                        //3.打包表格
                        Excel2SQLiteTools.GenSQLite(outPath);
                    }

                    //生成ios资源
                    if (isGenIosAssets)
                    {                 
                        var outPath = exportPath+"/"+Utils.GetPlatformPath(RuntimePlatform.IPhonePlayer);
                        //1.编译脚本
                        ScriptBiuldTools.GenDllByMono(Application.dataPath,outPath);
                        //2.打包资源
                        AssetBundleEditorTools.GenAssetBundle("Resource/Runtime/",outPath, BuildTarget.iOS);
                        //3.打包表格
                        Excel2SQLiteTools.GenSQLite(outPath);
                    }
                }
                   
            }
            //
            if (GUILayout.Button("资源转hash格式", GUILayout.Width(350), GUILayout.Height(30)))
            {
                exportPath = EditorUtility.OpenFolderPanel("选择导出目录", Application.dataPath.Replace("Assets",""), "");
                if (Directory.Exists(exportPath))
                {
                    AssetUploadToServer.Start(exportPath,"");
                }
                else
                {
                    EditorUtility.DisplayDialog("错误!", "你选择的文件夹有点问题哦~", "爱咋咋地!");
                }
            }
            //
            if (GUILayout.Button("上传到文件服务器[内网测试]", GUILayout.Width(350), GUILayout.Height(30)))
            {
                
                //先不实现,暂时没空
            }
        }
        GUILayout.EndVertical();
        
    }
    
    public static void Layout_DrawLineH(Color color, float height = 4f)
    {

        Rect rect = GUILayoutUtility.GetLastRect();
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, height), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
        GUILayout.Space(height);
    }

    public static void Layout_DrawLineV(Color color, float width = 4f)
    {
        Rect rect = GUILayoutUtility.GetLastRect();
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, width, rect.height), EditorGUIUtility.whiteTexture);
        GUI.color = Color.white;
        GUILayout.Space(width);
    }
}
