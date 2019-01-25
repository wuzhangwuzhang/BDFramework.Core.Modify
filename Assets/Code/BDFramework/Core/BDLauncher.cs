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
using UnityEngine.UI;

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
        public string FileServerUrl = "192.168.3.203";
        static public Action OnStart { get; set; }
        static public Action OnUpdate { get; set; }
        static public Action OnLateUpdate { get; set; }
        static public Action OnGUIAct { get; set; }

        public Slider slider;
        public Text loadTips;
        public GameObject panel;

        // Use this for initialization
        private void Awake()
        {
            this.gameObject.AddComponent<IEnumeratorTool>();
        }

        public IEnumerator Start()
        {
            string platform = Utils.GetPlatformPath(Application.platform);
            string localConfigPath = Application.persistentDataPath + "/" + platform + "_Server/"+ platform + "_VersionConfig.json";
            if (File.Exists(localConfigPath))
            {
                BDebug.Log("Resources already copy to persistantDataPath,return!");
                yield return null;
            }
            else
            {
                BDebug.Log("First Start,Copy Resources!");
                StartCoroutine(CopyStreamAsset2PersistantPath(platform + "_Server/"+ platform + "_VersionConfig.json"));

                yield return new WaitForSeconds(0.5f);
                AssetConfig localconf = null;

                if (File.Exists(localConfigPath))
                {
                    localconf = LitJson.JsonMapper.ToObject<AssetConfig>(File.ReadAllText(localConfigPath));
                    BDebug.Log("version:" + localconf.Version);
                    foreach (var item in localconf.Assets)
                    {
                        if (-1 == item.LocalPath.IndexOf('.'))//非文件不处理
                        {
                            continue;
                        }
                        StartCoroutine(CopyStreamAsset2PersistantPath(platform + "/" + item.LocalPath));
                    }
                }
                else
                {
                    BDebug.Log("not exist path:" + localConfigPath);
                }

            }
            yield return null;

            BDebug.Log("Check for Update!", "red");
            //检查更新资源更新
            StartCoroutine(CheckUpdateResources(()=>{

                //进入游戏
                BDebug.Log("Enter Game!", "red");
                LaunchLocal();
            }));
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
            BDebug.Log("Copy file to persistantPath:"+ srcFilePath, "red");
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


        int downLoadIndex= 0;
        int taskCount = 0;
        /// <summary>
        /// 检查需要更新的资源
        /// </summary>
        private IEnumerator CheckUpdateResources(Action CallBack)
        {
          
            var path = Application.persistentDataPath;
            var t = VersionContorller.Start("http://"+ FileServerUrl, path,
                (i, j) =>
                {
                    downLoadIndex = i;
                    taskCount = j;
                    if (i == j && j == 0)
                    {
                        slider.value = 1f;
                        panel.SetActive(false);
                        loadTips.text = string.Format("资源加载完成，游戏初始化中");
                        BDebug.LogError("no file to download");
                    }
                    else if (i == j && j != 0)
                    {
                        slider.value = 1f;
                        panel.SetActive(false);
                        loadTips.text = string.Format("资源加载完成，游戏初始化中");
                        BDebug.Log("<color=yellow>Resource download finished</color>");
                    }
                    else
                    {
                        float progress = (i + 1) * 1f / j;
                        slider.value =  progress;
                        loadTips.text = string.Format("资源加载进度：[{0}]",progress.ToString("P"));
                        Debug.LogFormat("资源更新进度：{0}/{1}", i, j);
                    }
                },
                (error) =>
                {
                    Debug.LogError("错误:" + error);
                }, CallBack );
            //var result = t.Result;
            //Debug.Log("下载状态返回:" + result);
            yield return null;
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