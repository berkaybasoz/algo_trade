using Microsoft.CSharp;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Interfaces;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Views.Model
{
    class CompileFunc
    {
        public List<string> ReferenceAssemblies = new List<string>();

        public string BaseDirectory { get; set; }

        public CompileFunc(string directory)
        {
            BaseDirectory = directory;

            ReferenceAssemblies.Add("QuantConnect.Common.dll");
            ReferenceAssemblies.Add("QuantConnect.Algorithm.dll");
            ReferenceAssemblies.Add("QuantConnect.Indicators.dll");
        }

        public CompileFunc(string directory, List<string> referencedAssemblies)
        {
            BaseDirectory = directory; 
            ReferenceAssemblies = referencedAssemblies;
        }


        #region Compile
        public IAlgorithm RuntimeCompile(string code)
        {
            Assembly assembly = CompileSource(code);

            List<string> types = Loader.GetExtendedTypeNames(assembly);

            string typeOfAlgorithm = types.FirstOrDefault();

            Type typeOfAlgo = assembly.GetType(typeOfAlgorithm);  //Type typeOfAlgo = assembly.GetType("QuantConnect.Algorithm.CSharp.TEBDynamicBasicAlgo1");

            var algorithm = (IAlgorithm)Activator.CreateInstance(typeOfAlgo);

            MethodInfo method = typeOfAlgo.GetMethod("Initialize");

            if (method.IsStatic)
                method.Invoke(null, null);
            else
                method.Invoke(algorithm, null);

            return algorithm;
            //yada
            //CurrentAssemblyLocation=  assembly.Location;
            //Config.Set("algorithm-type-name",String.Format(@"C:\Aktarım\Lean-master\UserInterface\bin\Debug\",currentDLL));


        }

        private Assembly CompileSource(string code)
        {


            CSharpCodeProvider provider = new CSharpCodeProvider();

            CompilerParameters parameters = new CompilerParameters();
            // Reference to System.Drawing library

            if (ReferenceAssemblies != null)
            {
                foreach (var assmblyname in ReferenceAssemblies)
                {
                    parameters.ReferencedAssemblies.Add(Path.Combine(BaseDirectory, assmblyname));//@"C:\Aktarım\Lean-master\Common\bin\Debug\QuantConnect.Common.dll" 
                    //parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Common\bin\Debug\QuantConnect.Common.dll");
                }
            }

            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = false;
            //parameters.TreatWarningsAsErrors = false;
            //parameters.WarningLevel = 4; 
            //parameters.TempFiles.KeepFiles = false;


            CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);

            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }

                throw new InvalidOperationException(sb.ToString());
            }


            Assembly assembly = results.CompiledAssembly;
            return assembly;
        }


        [Obsolete("Bunu kullanma")]
        private Assembly CompileSourceOrig(string code)
        {


            CSharpCodeProvider provider = new CSharpCodeProvider();

            CompilerParameters parameters = new CompilerParameters();
            // Reference to System.Drawing library


            parameters.ReferencedAssemblies.Add(Path.Combine(BaseDirectory, "QuantConnect.Common.dll"));//@"C:\Aktarım\Lean-master\Common\bin\Debug\QuantConnect.Common.dll"
            parameters.ReferencedAssemblies.Add(Path.Combine(BaseDirectory, "QuantConnect.Algorithm.dll"));//@"C:\Aktarım\Lean-master\Algorithm\bin\Debug\QuantConnect.Algorithm.dll");
            parameters.ReferencedAssemblies.Add(Path.Combine(BaseDirectory, "QuantConnect.Indicators.dll"));//@"C:\Aktarım\Lean-master\Indicators\bin\Debug\QuantConnect.Indicators.dll");

            //parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Common\bin\Debug\QuantConnect.Common.dll");
            //parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Algorithm\bin\Debug\QuantConnect.Algorithm.dll");
            //parameters.ReferencedAssemblies.Add(@"C:\Aktarım\Lean-master\Indicators\bin\Debug\QuantConnect.Indicators.dll");
            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = false;
            //parameters.TreatWarningsAsErrors = false;
            //parameters.WarningLevel = 4; 
            //parameters.TempFiles.KeepFiles = false;


            CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);

            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }

                throw new InvalidOperationException(sb.ToString());
            }


            Assembly assembly = results.CompiledAssembly;
            return assembly;
        }
        #endregion
    }
}
