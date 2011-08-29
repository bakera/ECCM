using System;
using System.IO;
using System.CodeDom.Compiler;

namespace Bakera.Eccm{
	public class PluginCompiler{

		private const string JsExt = ".js";
		private const string CsExt = ".cs";
		private const string VbExt = ".vb";

		private Setting mySetting = null;
		private string myPluginDllPath = null;

// コンストラクタ

		public PluginCompiler(Setting s){
			mySetting = s;
		}


// public メソッド

		public void CompileFile(string filePath){
			if(!File.Exists(filePath)){
				throw new Exception("ファイルがありません : " + filePath);
			}

			CodeDomProvider myCompiler;
			string ext = Path.GetExtension(filePath).ToLower();
			switch(ext){
			case JsExt:
				myCompiler = new Microsoft.JScript.JScriptCodeProvider();
				break;
			case CsExt:
				myCompiler = new Microsoft.CSharp.CSharpCodeProvider();
				break;
			case VbExt:
				myCompiler = new Microsoft.VisualBasic.VBCodeProvider();
				break;
			default:
				throw new Exception("未対応のファイル拡張子です: " + filePath);
			}

			myPluginDllPath = mySetting.TemplateDir + Path.GetFileNameWithoutExtension(filePath) + ".dll";
			string code = Util.LoadFile(filePath);

			CompilerParameters cp = new CompilerParameters();
			cp.GenerateExecutable = true;
			cp.OutputAssembly = myPluginDllPath;
			CompilerResults cr = myCompiler.CompileAssemblyFromSource(cp, code);
			foreach(string s in cr.Output){
			    Console.WriteLine(s);
			}
			
			myCompiler.Dispose();
		}



	}
}
