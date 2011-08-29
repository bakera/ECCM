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

// �v���p�e�B
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


		// �v���O�C���\�[�X�R�[�h�̊g���q���擾���܂��B
		// �ݒ�ɂ�� .cs / .js �̂����ꂩ�������� null �������܂��B
		public string PluginSourceSuffix{
			get{
				if(Setting.PluginKind == PluginKind.CSharp) return CSharpFileSuffix;
				if(Setting.PluginKind == PluginKind.JScript) return JScriptFileSuffix;
				return null;
			}
		}

		// �v���W�F�N�g������v���O�C���̃A�Z���u�������擾���܂��B
		public string BinDirectory{
			get{return myBinDirectory;}
		}


		// �v���W�F�N�g������v���O�C���̃A�Z���u�������擾���܂��B
		public string GetPluginAsmName(){
			return PluginAsmNamePrefix + Id.Replace('-', '_');
		}


		// �n���ꂽ���O�ɑΉ�����v���O�C���\�[�X�t�@�C���̔z����擾���܂��B
		// ������ _ �Ŏn�܂�t�@�C���͏��O���܂��B
		public FileInfo[] GetSrcFiles(string name){
			FileInfo[] files = GetFiles(name);
			List<FileInfo> fl = new List<FileInfo>();
			foreach(FileInfo f in files){
				if(f.Name.StartsWith(TestFilePrefix)) continue;
				fl.Add(f);
			}
			return fl.ToArray();
		}

		// �n���ꂽ���O�ɑΉ�����e�X�g�t�@�C���̔z����擾���܂��B
		// "*" ��n���ƑS�Ẵt�@�C���������Ă��܂��B
		public FileInfo[] GetTestFiles(string name){
			return GetFiles(TestFilePrefix + name);
		}

		// �n���ꂽ���O�ɑΉ�����t�@�C���̔z����擾���܂��B
		// "*" ��n���ƑS�Ẵt�@�C���������Ă��܂��B
		public FileInfo[] GetFiles(string name){
			FileInfo[] files = Setting.TemplateFullPath.GetFiles(name + this.PluginSourceSuffix);
			return files;
		}


		// �v���W�F�N�g������v���O�C���̃t�@�C�����擾���܂��B
		public FileInfo GetPluginAsmFile(){
			string asmFilePath = myBinDirectory.TrimEnd('\\') + '\\' + GetPluginAsmName() + ".dll";
			return new FileInfo(asmFilePath);
		}

		// �v���W�F�N�g������v���O�C���̖��O��Ԃ��擾���܂��B
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

