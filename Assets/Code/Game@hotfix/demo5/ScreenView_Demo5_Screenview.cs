using BDFramework.ScreenView;
using BDFramework.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ScreenView("messageBox")]
public class ScreenView_Demo5_Screenview : IScreenView
{
    public string Name { get; private set; }

    public bool IsLoad { get; private set; }

    public void BeginExit()
    {
        this.IsLoad = false;
        BDebug.Log("exit messageBox");
    }

    public void BeginInit()
    {
        this.IsLoad = true;
        UIManager.Inst.LoadWindows((int)WinEnum.Win_Demo5);
        UIManager.Inst.ShowWindow((int)WinEnum.Win_Demo5);
        BDebug.Log("show messsageBox");
    }

    public void FixedUpdate(float delta)
    {
        BDebug.Log("ScreenView_Demo5_Screenview::FixedUpdate:" + delta);
    }

    public void Update(float delta)
    {
        BDebug.Log("ScreenView_Demo5_Screenview::Update:"+delta);
    }
}
