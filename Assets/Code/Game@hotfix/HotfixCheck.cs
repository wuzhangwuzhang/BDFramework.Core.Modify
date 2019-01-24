using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LitJson;
using UnityEngine;

public class HotfixCheck
{
   static public async void Log()
   {
        BDebug.Log("[Hotfix]:Async Json Test Start:"+Time.realtimeSinceStartup);
        var result = await TestAction("aa", "bb",
           (o, p) =>
           {
               BDebug.Log(o.ToString() + " - " + p.ToString());
           },
           (q) =>
           {
               BDebug.Log("Test Json ToObject:" + q);
           });

        BDebug.Log("[Hotfix]:Async Json End:" + Time.realtimeSinceStartup);

    }
   
   public class  testclass
   {
        public int i = 1;
   }

   static async public Task<int> TestAction(string s,string s2,Action<int,int>callback2,Action<string> callback)
   {
        int i = 222111;
        var test =  new testclass();
        test.i = 2222;
        
        var json = JsonMapper.ToJson(test);
        var o = JsonMapper.ToObject<testclass>(json);
        //
        callback(json.ToString());
        int m = 1;
        int n = 2;
        callback2(m, n);
        return 0;
   }

    static void TestLog()
    {
        BDebug.Log("TestLog:" + Time.realtimeSinceStartup);
    }
}
