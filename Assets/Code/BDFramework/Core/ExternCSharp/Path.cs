using System.IO;

namespace System.IO
{
   static public class IPath
    {
        //���Mac��Path�ӿ�ʧЧ����
        static public string Combine(string a, string b)
        {
            return a + "/" + b;
        }
    }
}