using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Views.Model.Algo
{
    public class AlgoCode
    {

        public AlgoCode(string displayName, string className, string fileName,bool isDebugable = false, string filePath = null)
        { 
            DisplayName = displayName;
            ClassName = className;
            FileName = fileName;
            IsDebugable = isDebugable;
            FilePath = filePath;
        }

        public string DisplayName { get; set; }//TEB Basic Algo1 
        public string ClassName { get; set; }//TEBBasicAlgo1
        public string FileName { get; set; }//TEBBasicAlgo1.cs
        public string FilePath { get; set; }//C://TEBBasicAlgo1.cs
        public string Code { get; set; }//Csharp code
        public bool IsDebugable { get; set; }
    }
}
