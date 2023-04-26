using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Microsoft.VisualBasic.ApplicationServices;
using OggVorbisEncoder.Setup;
using System.Reflection;
using System;
using System.Linq;
using dnSpy;

namespace KotHModLoaderGUI
{
    internal class AssemblyManager
    {
        public AssemblyManager()
        {
            //Decompile();
        }

        private void Decompile()
        {
            var decompiler = new CSharpDecompiler("D:\\KotHModLoader\\KotHModLoaderGUI\\bin\\Debug\\KingOfTheHat_Data\\Managed\\Assembly-CSharp.dll", new DecompilerSettings());
            var tree = decompiler.DecompileWholeModuleAsSingleFile();
            int children = tree.Children.Count();
            //string code = decompiler.DecompileWholeModuleAsString();

            var decompiler2 = new CSharpDecompiler("D:\\KotHModLoader\\KotHModLoaderGUI\\bin\\Debug\\KingOfTheHat_Data\\Managed\\Assembly-CSharp.dll MAIN", new DecompilerSettings());
            var tree2 = decompiler2.DecompileWholeModuleAsSingleFile();
            int children2 = tree2.Children.Count();
            //string code2 = decompiler2.DecompileWholeModuleAsString();

            for (int i = 0; i < (children2 < children ? children2 : children); i++)
            {
                bool equal = tree.Children.ElementAt(i).ToString() ==  tree2.Children.ElementAt(i).ToString();
                if(!equal)
                {

                }
            }
        }
    }
}
