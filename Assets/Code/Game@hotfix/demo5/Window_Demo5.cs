using BDFramework;
using BDFramework.ScreenView;
using BDFramework.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[UI((int)WinEnum.Win_Demo5, "Windows/window_messageBox")]
public class Window_Demo5 : AWindow
{
    [TransformPath("txt_title")] private Text title;
    [TransformPath("txt_msg")] private Text msg;
    [TransformPath("btn_ok")] private Button btn_ok;
    [TransformPath("btn_cancle")] private Button btn_cancle;

    public Window_Demo5(string path) : base(path)
    {
    }

    public override void Init()
    {
        base.Init();
        title.text = "New_Title";
        BDebug.LogError("Window_Demo5 初始化");
        btn_ok.onClick.AddListener(()=> {
            msg.text = "On btn_ok click!";
        });

        btn_cancle.onClick.AddListener(() =>
        {
            msg.text = "On btn_cancle click!";
            ScreenViewManager.Inst.MainLayer.BeginNavTo("main");
        });
        //BDLauncher.OnUpdate += Update;
    }

    public override void Update()
    {
        //base.Update();
        BDebug.LogError("[Hotfix]:messageBox::update:"+Time.realtimeSinceStartup);
    }

    public override void Open(WindowData data = null)
    {
        base.Open(data);
        BDebug.Log("Open");
    }

    public override void Close()
    {
        base.Close();
        BDebug.Log("Close");
    }

    public override void Destroy()
    {
        base.Destroy();
        BDebug.Log("Destroy");
    }

    public void OnGUI()
    {
        if (GUI.Button(new Rect(100, 10, 100, 20), "BUtton"))
        {
            BDebug.LogError("OnGUI Test");
        }
    }
}
