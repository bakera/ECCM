using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Xml;


namespace Bakera.Eccm{
	public partial class EcmProject : DataTable{
		public const string PluginAsmNamePrefix = "plugins";
		public const string TestFilePrefix = "_";
		public const string PluginNameSpacePrefix = "Bakera.Eccm.Plugins.";

		public const string CSharpFileSuffix = ".cs";
		public const string JScriptFileSuffix = ".js";


		private static Compiler myCompiler = new Compiler();

		private Assembly myPluginAsm = null;
		private string myBinDirectory = WebConfigurationManager.AppSettings[Eccm.BinDirKey];

// プロパティ
		public Assembly PluginAssembly{
			get{
				if(myPluginAsm == null){
					string asmName = GetPluginAsmName();
					try{
						myPluginAsm = Assembly.Load(asmName);
					} catch(FileNotFoundException){
						return null;
					}
				}
				return myPluginAsm;
			}
		}


		// プラグインソースコードの拡張子を取得します。
		// 設定により .cs / .js のいずれかもしくは null が得られます。
		public string PluginSourceSuffix{
			get{
				if(Setting.PluginKind == PluginKind.CSharp) return CSharpFileSuffix;
				if(Setting.PluginKind == PluginKind.JScript) return JScriptFileSuffix;
				return null;
			}
		}

		// プロジェクト名からプラグインのアセンブリ名を取得します。
		public string BinDirectory{
			get{return myBinDirectory;}
		}


		// プロジェクト名からプラグインのアセンブリ名を取得します。
		public string GetPluginAsmName(){
			return PluginAsmNamePrefix + Id.Replace('-', '_');
		}


		// 渡された名前に対応するプラグインソースファイルの配列を取得します。
		// ただし _ で始まるファイルは除外します。
		public FileInfo[] GetSrcFiles(string name){
			FileInfo[] files = GetFiles(name);
			List<FileInfo> fl = new List<FileInfo>();
			foreach(FileInfo f in files){
				if(f.Name.StartsWith(TestFilePrefix)) continue;
				fl.Add(f);
			}
			return fl.ToArray();
		}

		// 渡された名前に対応するテストファイルの配列を取得します。
		// "*" を渡すと全てのファイルを持ってきます。
		public FileInfo[] GetTestFiles(string name){
			return GetFiles(TestFilePrefix + name);
		}

		// 渡された名前に対応するファイルの配列を取得します。
		// "*" を渡すと全てのファイルを持ってきます。
		public FileInfo[] GetFiles(string name){
			FileInfo[] files = Setting.TemplateFullPath.GetFiles(name + this.PluginSourceSuffix);
			return files;
		}


		// プロジェクト名からプラグインのファイルを取得します。
		public FileInfo GetPluginAsmFile(){
			string asmFilePath = myBinDirectory.TrimEnd('\\') + '\\' + GetPluginAsmName() + ".dll";
			return new FileInfo(asmFilePath);
		}

		// プロジェクト名からプラグインの名前空間を取得します。
		public string GetPluginNameSpace(){
			return PluginNameSpacePrefix + Id.Replace('-', '_');
		}


		public Type[] GetPlugins(){
			if(PluginAssembly == null){
				return new Type[0];
			}

			List<Type> result = new List<Type>();
			foreach(Type t in PluginAssembly.GetTypes()){
				if(t == null) continue;
				if(t.IsSubclassOf(typeof(EcmPluginBase))) result.Add(t);
			}
			return result.ToArray();
		}


	}
}

