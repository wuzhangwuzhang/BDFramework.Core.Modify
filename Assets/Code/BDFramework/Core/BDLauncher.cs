using System;
using System.Reflection;
using BDFramework;
using BDFramework.GameStart;
using SQLite4Unity3d;
using UnityEngine;
using BDFramework.ResourceMgr;
using UnityEngine.Serialization;
using System.Collections;
using System.IO;
using BDFramework.Helper;
using LitJson;
using System.Collections.Generic;

namespace BDFramework
{
    public enum AssetLoadPath
    {
        Editor,
        Persistent,
        StreamingAsset
    }

    public class BDLauncher : MonoBehaviour
    {
        public AssetLoadPath CodeRoot = AssetLoadPath.Editor;
        public AssetLoadPath SQLRoot = AssetLoadPath.Editor;
        public AssetLoadPath ArtRoot = AssetLoadPath.Editor;
        public string FileServerUrl = "192.168.8.68";
        static public Action OnStart { get; set; }
        static public Action OnUpdate { get; set; }
        static public Action OnLateUpdate { get; set; }
        static public Action OnGUIAct { get; set; }


        // Use this for initialization
        private void Awake()
        {
            this.gameObject.AddComponent<IEnumeratorTool>();
        }

        public IEnumerator Start()
        {
            StartCoroutine(CopyStreamAsset2PersistantPath(Utils.GetPlatformPath(Application.platform) + "/Windows_VersionConfig.json")); ;
            yield return new WaitForSeconds(1f);
            AssetConfig localconf = null;
            string localConfigPath = Application.persistentDataPath + "/" + Utils.GetPlatformPath(Application.platform) + "/Windows_VersionConfig.json";

            if (File.Exists(localConfigPath))
            {
                localconf = LitJson.JsonMapper.ToObject<AssetConfig>(File.ReadAllText(localConfigPath));
                BDebug.Log("version:" + localconf.Version);
                foreach (var item in localconf.Assets)
                {
                    if (item.LocalPath == "LocalDB")
                    {
                        item.LocalPath = "Local.db";
                    }
                    StartCoroutine(CopyStreamAsset2PersistantPath(Utils.GetPlatformPath(Application.platform)+"/" + item.LocalPath));
                }
            }
            else
            {
                BDebug.Log("not exist path:" + localConfigPath);
            }

            yield return new WaitForSeconds(2f);
            string artConfig = Application.persistentDataPath + "/" + Utils.GetPlatformPath(Application.platform) + "/Art/Config.json";
            if (File.Exists(artConfig))
            {
                var content = File.ReadAllText(artConfig);
                var list = JsonMapper.ToObject<List<ManifestItem>>(content);
                foreach (var item in list)
                {
                    for (int i = 0; i < item.Dependencies.Count; i++)
                    {
                        if (item.Dependencies[i] == item.Name)
                            continue;
                        StartCoroutine(CopyStreamAsset2PersistantPath(Utils.GetPlatformPath(Application.platform) + "/Art/" + item.Dependencies[i]));
                    }
                    StartCoroutine(CopyStreamAsset2PersistantPath(Utils.GetPlatformPath(Application.platform) + "/Art/" + item.Name));
                }
            }
            else
            {
                BDebug.LogError("can't find:"+artConfig);

            }
            yield return new WaitForSeconds(1f);
            LaunchLocal();
        }

        /// <summary>
        /// 拷贝包体文件
        /// </summary>
        /// <param name="absulateFilePath">文件相对路径</param>
        /// <returns></returns>
        public IEnumerator CopyStreamAsset2PersistantPath(string absulateFilePath)
        {
            string srcFilePath = "";
#if UNITY_EDITOR
            srcFilePath = "file:///" + Application.streamingAssetsPath + "/" + absulateFilePath;
#elif UNITY_ANDROID
            srcFilePath = "jar:file://" + Application.dataPath + "!/assets/" + absulateFilePath;
#elif UNITY_IPHONE
            srcFilePath = "file://" + Application.dataPath + "/Raw/" + absulateFilePath;
#endif
            string targetPath = Application.persistentDataPath+ "/" + absulateFilePath;

            string filename = absulateFilePath.Substring(absulateFilePath.LastIndexOf('/')+1);
            //BDebug.Log("Copy file to persistantPath:"+ srcFilePath, "red");
            WWW www = new WWW(srcFilePath);
            yield return www;
            string directory = targetPath.Replace("/" + filename, "");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }


            if (!string.IsNullOrEmpty(www.error))
            {
                BDebug.LogError(www.error +" "+srcFilePath);
            }
            else
            {
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
                fs.Write(www.bytes, 0, www.bytes.Length);
                fs.Flush();
                fs.Close();
                if (File.Exists(targetPath))
                {
                    BDebug.LogFormat("Copy to persistantPath OK,{0}", targetPath);
                }
                else
                {
                    BDebug.LogFormat("Copy to persistantPath Failed,{0}", filename);
                }
            }
            www.Dispose();
        }

#region 启动非热更逻辑

        /// <summary>
        /// 启动本地代码
        /// </summary>
        public void LaunchLocal()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();


            var istartType = typeof(IGameStart);
            foreach (var t in types)
            {
                if (t.IsClass && t.GetInterface("IGameStart") != null)
                {
                    var attr = t.GetCustomAttribute(typeof(GameStartAtrribute), false);
                    if (attr != null)
                    {
                        var gs = Activator.CreateInstance(t) as IGameStart;

                        //注册
                        gs.Start();

                        //
                        BDLauncher.OnUpdate = gs.Update;
                        BDLauncher.OnLateUpdate = gs.LateUpdate;
                    }
                }
            }
        }

#endregion

#region 启动热更逻辑

        /// <summary>
        /// 初始化
        /// 修改版本,让这个启动逻辑由使用者自行处理
        /// </summary>
        /// <param name="GameId">单游戏更新启动不需要id，多游戏更新需要id号</param>
        public void Launch(string GameId = "")
        {
            //初始化资源加载
            string coderoot = "";
            string sqlroot = "";
            string artroot = "";

            //各自的路径
            //art
            if (ArtRoot == AssetLoadPath.Editor)
            {
                if (Application.isEditor)
                {
                    //默认不走AssetBundle
                    artroot = "";
                }
                else
                {
                    //手机默认直接读取Assetbundle
                    artroot = Application.persistentDataPath;
                }
            }
            else if (ArtRoot == AssetLoadPath.Persistent)
            {
                artroot = Application.persistentDataPath;
            }

            else if (ArtRoot == AssetLoadPath.StreamingAsset)
            {
                artroot = Application.streamingAssetsPath;
            }

            //sql
            if (SQLRoot == AssetLoadPath.Editor)
            {
                //sql 默认读streaming
                sqlroot = Application.streamingAssetsPath;
            }

            else if (SQLRoot == AssetLoadPath.Persistent)
            {
                sqlroot = Application.persistentDataPath;
            }
            else if (SQLRoot == AssetLoadPath.StreamingAsset)
            {
                sqlroot = Application.streamingAssetsPath;
            }

            //code
            if (CodeRoot == AssetLoadPath.Editor)
            {
                //sql 默认读streaming
                coderoot = "";
            }

            else if (CodeRoot == AssetLoadPath.Persistent)
            {
                coderoot = Application.persistentDataPath;
            }
            else if (CodeRoot == AssetLoadPath.StreamingAsset)
            {
                coderoot = Application.streamingAssetsPath;
            }

            //多游戏更新逻辑
            if (Application.isEditor == false)
            {
                if (GameId != "")
                {
                    artroot = artroot + "/" + GameId;
                    coderoot = coderoot + "/" + GameId;
                    sqlroot = sqlroot + "/" + GameId;
                }
            }

            //sql
            SqliteLoder.Load(sqlroot);
            //art
            BResources.Load(artroot);
            //code
            LoadScrpit(coderoot);
        }

        /// <summary>
        /// 开始热更脚本逻辑
        /// </summary>
        private void LoadScrpit(string root)
        {
            if (root != "") //热更代码模式
            {
                ILRuntimeHelper.LoadHotfix(root);
                ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null,
                    new object[] {true});
            }
            else
            {
                //这里用反射是为了 不访问逻辑模块的具体类，防止编译失败
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {false});
            }
        }

#endregion

        //是否ILR模式
        public bool IsCodeHotfix
        {
            get {
                if (CodeRoot != AssetLoadPath.Editor)
                {
                    return true;
                }
                return false;
            }
        }

        //普通帧循环
        private void Update()
        {
            if (OnUpdate != null)
            {
                OnUpdate();
            }
        }

        //更快的帧循环
        private void LateUpdate()
        {
            if (OnLateUpdate != null)
            {
                OnLateUpdate();
            }
        }

        public void OnGUI()
        {
            if (OnGUIAct != null)
                OnGUIAct();
        }
        void OnApplicationQuit()
        {
#if UNITY_EDITOR
            BDFramework.Sql.SqliteHelper.DB.Close();
            ILRuntimeHelper.Close();
#endif
        }
    }
}