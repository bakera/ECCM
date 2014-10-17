using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.JScript;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Web.Configuration;

namespace Bakera.Eccm{
	public class Compiler{

		private const string CSCVersionConfigKeyName = "EccmCsharpCompilerVersion";
		private string CSCVersionConfig = WebConfigurationManager.AppSettings[CSCVersionConfigKeyName];

		private Assembly myCurrentAsm;
		private string[] myCurrentReferenceAsmNames;
		private const string CsCodeFrame = @"
using System;
using Bakera.Eccm;
namespace {0}{{
public class {1} : EcmPluginBase{{
public {1}(EcmPluginParams param) : base(param){{}}
{2}
}}
}}
";
		private const string JsCodeFrame = @"
import System;
import Bakera.Eccm;
package {0}{{
class {1} extends EcmPluginBase{{
function {1}(param : EcmPluginParams){{super(param)}}
{2}
}}
}}
";


// コンストラクタ
		public Compiler(){
			myCurrentAsm = Assembly.GetAssembly(typeof(Eccm));
			AssemblyName[] refAsmNames = myCurrentAsm.GetReferencedAssemblies();
			var tempCurrentReferenceAsmNames = new List<string>();
			for(int i=0; i < refAsmNames.Length; i++){
				try{
					string asmLocation = Assembly.Load(refAsmNames[i]).Location;
					
					// 以下のようなエラーが出る環境があるので corlib を取り除く
					// error CS1703: 同じ ID 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' のアセンブリが既にインポートされています。重複している参照の一方を削除してください。
					if(asmLocation.EndsWith("mscorlib.dll")) continue;
					tempCurrentReferenceAsmNames.Add(asmLocation);
				} catch(FileNotFoundException){}
			}
			tempCurrentReferenceAsmNames.Add(myCurrentAsm.Location);
			myCurrentReferenceAsmNames = tempCurrentReferenceAsmNames.ToArray();

		}


// プロパティ
		public string[] CurrentReferenceAsmNames{
			get{return myCurrentReferenceAsmNames;}
		}


// メソッド
		// ファイルから読み取ったコードをコンパイルします。
		// 出力ファイルを null とするとオンメモリでコンパイルを行います。
		// コードの種類は最初のファイルの拡張子で判別します。
	    public CompilerResults Compile(FileInfo[] sourceFiles, FileInfo dest, EcmProject project){
			if(sourceFiles == null || sourceFiles.Length == 0) return null;

			CodeDomProvider compiler = null;
			string codeFrame = null;
			if(sourceFiles[0].Extension.Equals(EcmProject.CSharpFileSuffix)){
				if(string.IsNullOrEmpty(CSCVersionConfig)){
					compiler = new CSharpCodeProvider();
				} else {
					compiler = new CSharpCodeProvider(new Dictionary<string, string>(){{"CompilerVersion", CSCVersionConfig}});
				}
				codeFrame = CsCodeFrame;
			} else if(sourceFiles[0].Extension.Equals(EcmProject.JScriptFileSuffix)){
				compiler = new JScriptCodeProvider();
				codeFrame = JsCodeFrame;
			} else {
				throw new Exception("不明なファイル拡張子です。コンパイルする言語が特定できません。");
			}

			string[] sourceData = new string[sourceFiles.Length];
			for(int i=0; i< sourceFiles.Length; i++){
				string tempData = "";
				using(FileStream fs = sourceFiles[i].Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
					using(StreamReader sr = new StreamReader(fs)){
						tempData = sr.ReadToEnd();
						sr.Close();
					}
					fs.Close();
				}
				string fileName = Path.GetFileNameWithoutExtension(sourceFiles[i].Name).Replace("-", "");
				sourceData[i] = string.Format(codeFrame, project.GetPluginNameSpace(), fileName.ToLower(), tempData);
			}

			CompilerParameters cp = new CompilerParameters(myCurrentReferenceAsmNames);
			if(dest ==null){
				cp.GenerateInMemory = true;
			} else {
				cp.GenerateInMemory = false;
				cp.OutputAssembly = dest.FullName;
			}
//			Util.Throw(myCurrentReferenceAsmNames);
			
			CompilerResults cr = compiler.CompileAssemblyFromSource(cp, sourceData);
			return cr;
	    }

		// 単一ファイルから読み取ったコードをコンパイルします。
	    public CompilerResults Compile(FileInfo sourceFile, FileInfo dest, EcmProject project){
			return Compile(new FileInfo[]{sourceFile}, dest, project);
	    }


	}
}
