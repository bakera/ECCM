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


// �R���X�g���N�^
		public Compiler(){
			myCurrentAsm = Assembly.GetAssembly(typeof(Eccm));
			AssemblyName[] refAsmNames = myCurrentAsm.GetReferencedAssemblies();
			var tempCurrentReferenceAsmNames = new List<string>();
			for(int i=0; i < refAsmNames.Length; i++){
				try{
					string asmLocation = Assembly.Load(refAsmNames[i]).Location;
					
					// �ȉ��̂悤�ȃG���[���o���������̂� corlib ����菜��
					// error CS1703: ���� ID 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' �̃A�Z���u�������ɃC���|�[�g����Ă��܂��B�d�����Ă���Q�Ƃ̈�����폜���Ă��������B
					if(asmLocation.EndsWith("mscorlib.dll")) continue;
					tempCurrentReferenceAsmNames.Add(asmLocation);
				} catch(FileNotFoundException){}
			}
			tempCurrentReferenceAsmNames.Add(myCurrentAsm.Location);
			myCurrentReferenceAsmNames = tempCurrentReferenceAsmNames.ToArray();

		}


// �v���p�e�B
		public string[] CurrentReferenceAsmNames{
			get{return myCurrentReferenceAsmNames;}
		}


// ���\�b�h
		// �t�@�C������ǂݎ�����R�[�h���R���p�C�����܂��B
		// �o�̓t�@�C���� null �Ƃ���ƃI���������ŃR���p�C�����s���܂��B
		// �R�[�h�̎�ނ͍ŏ��̃t�@�C���̊g���q�Ŕ��ʂ��܂��B
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
				throw new Exception("�s���ȃt�@�C���g���q�ł��B�R���p�C�����錾�ꂪ����ł��܂���B");
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

		// �P��t�@�C������ǂݎ�����R�[�h���R���p�C�����܂��B
	    public CompilerResults Compile(FileInfo sourceFile, FileInfo dest, EcmProject project){
			return Compile(new FileInfo[]{sourceFile}, dest, project);
	    }


	}
}
