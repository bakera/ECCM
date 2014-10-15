using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Bakera.Eccm{
	public class Parser : EcmProcessorBase{

		private Regex myMsReg = new EcmRegex.MemberSelector();
		private Regex myMetaCharsetReg = new EcmRegex.MetaCharset();

		private ExportManager myExp = null;
		private int myDepthCounter = 0;//�������[�v���o�p
		private string myReadData = null;
		private Dictionary<Type, int> myPluginProcessCounter = new Dictionary<Type, int>();

		public new const string Name = "ECCM�p�[�T�[";
		public new const string Description = "ECCM�R�����g�̃p�[�X�ƒu�����s���A�R���e���c��publish���܂��B";

// �R���X�g���N�^
		public Parser(EcmProject proj) : base(proj){}


// �v���p�e�B

		// �p�[�X�O�̓ǂݎ�����f�[�^���擾���܂��B
		public string ReadData{
			get{return myReadData;}
		}


// public ���\�b�h


		// �^����ꂽ EcmItem ���A�^����ꂽ FileStream ����f�[�^��ǂݎ���ăp�[�X���A���ʂ̕������Ԃ��܂��B
		private string Parse(EcmItem targetItem, FileInfo targetFile){

			string result = null;
			// �p�[�X�J�n
			try{
				Log.AddInfo("{0} �p�[�X�J�n", targetItem);
				myDepthCounter = 0;
				if(targetItem == null){
					Log.AddError("{0} �� null �ł��B", targetItem);
					return null;
				}
				Project.CurrentItem = targetItem;
				Encoding enc = Setting.HtmlEncodingObj;
				myExp = new ExportManager(this, targetItem);

				if(targetFile != null){
					Log.AddInfo("���������������u{0}�v�Ƃ��ăf�[�^�ǂݎ����J�n���܂��B", enc.EncodingName);
					try{
						myReadData = LoadFile(targetFile, enc);
						Log.AddInfo("�f�[�^��ǂݎ��܂����B(�f�[�^�T�C�Y : {0})", myReadData.Length);
					} catch(IOException e){
						Log.AddError("�t�@�C�� {0} ���J���܂��� : {1}", targetItem.FilePath, e.Message);
						return result;
					} catch(UnauthorizedAccessException e){
						Log.AddError("�t�@�C�� {0} ���J���܂��� : {1}", targetItem.FilePath, e.Message);
						return result;
					}

					if(Setting.HtmlEncodingDetectFromMetaCharset){
						Log.AddInfo("meta charset�ɂ�镶���R�[�h�������ʂ��s���܂��B");
						Encoding detectedEnc = DetectEncodingFromMetaCharset(myReadData);
						if(detectedEnc == null){
							Log.AddError("meta charset�����o�ł��Ȃ��������߁A�����𒆒f���܂��B");
							return null;
						} else {
							if(detectedEnc.CodePage != enc.CodePage){
								Log.AddInfo("���ʌ��ʂ͊���̕����R�[�h�ƈقȂ邽�߁A���ʌ��ʂɊ�Â��ăt�@�C�����J���Ȃ����܂��B");
								myReadData = LoadFile(targetFile, detectedEnc);
								Log.AddInfo("�f�[�^��ǂݎ��܂����B(�f�[�^�T�C�Y : {0})", myReadData.Length);
							} else {
								Log.AddInfo("���ʌ��ʂ͊���̕����R�[�h�ƈ�v���Ă��܂����B");
							}
						}
					}
					// �G�N�X�|�[�g���Ă���
					myExp.Parse(myReadData);
					Log.AddInfo("{0} �ҏW�̈�G�N�X�|�[�g����", targetItem);
				} else {
					Log.AddInfo("���̓f�[�^�͂���܂���B");
					myReadData = "";
				}

				if(string.IsNullOrEmpty(targetItem.Template)){
					result = GeneralParse(myReadData, false);
				} else {
					EcmTemplate template = GetTemplate(targetItem.Template);
					if(!template.Exists){
						Log.AddAlert("�O���[�o���e���v���[�g�̃f�[�^���擾�ł��܂���ł����B�t�@�C����������܂���B: {0}", template.File.FullName);
						return null;
					}
					Log.AddInfo("�O���[�o���e���v���[�g�̃f�[�^���擾���܂����B�t�@�C�� : {0}, �T�C�Y: {1}", template.File.FullName, template.File.Length);
					result = GeneralParse(template.GetData());
				}
				Log.AddInfo("{0} �p�[�X����", targetItem);
			} catch(ParserException e){
				Log.AddError("{0} �p�[�X�G���[: {1}", targetItem, e.Message);
			} catch(Exception e){
				Log.AddError("{0} �̏������ɃG���[������: {1}", targetItem, e.ToString());
			}
			return result;
		}



		// �^����ꂽ EcmItem �ɑΉ�����t�@�C���� Parse ���Ēu�����܂��B
		public override ProcessResult Process(EcmItem targetItem){

			ProcessResult result = new ProcessResult();

			// �v���W�F�N�g�������O�o��
			Log.AddInfo("Project: {0}, FileTime:{1}, DataTime: {2}", Project.Id, Project.FileTime, Project.DataTime);

			// ���S�m�F
			string[] errorId = Project.GetDuplicateId();
			if(errorId.Length > 0){
				Log.AddError("ID: {0} ���d�����Ă��܂��B", string.Join(", ", errorId));
				return result;
			}
			Log.AddInfo("ID�̐��������m�F");

			string[] errorPath = Project.GetDuplicatePath();
			if(errorPath.Length > 0){
				Log.AddError("������ ID �� {0} ���Q�Ƃ��Ă��܂��B", string.Join(", ", errorPath));
				return result;
			}
			Log.AddInfo("Path�̐��������m�F");


			Log.AddInfo("ID : {0} �̍��ڂ𔭌�", targetItem.Id);

			if(string.IsNullOrEmpty(targetItem.Path)){
				Log.AddError("ID : {0} �ɂ� Path ���ݒ肳��Ă��Ȃ����߁A�p�[�X�ł��܂���B", targetItem.Id);
				return result;
			}

			// �t�@�C�������邩?
			// �O���[�o���e���v���[�g���w�肳��Ă���ꍇ�̓t�@�C���������Ă��������邱�Ƃɒ��ӁB
			Log.AddInfo("{0} �̃t�@�C�� : {1}", targetItem.Id, targetItem.FilePath);
			targetItem.File.Refresh();
			if(targetItem.File.Exists){
				// �t�@�C��������
				// �������݂ł��邩�ǂ��� (���b�N����Ă��Ȃ���) �m�F����
				try{
					using(FileStream fs = targetItem.File.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None)){
						fs.Close();
					}
					Log.AddInfo("�t�@�C�� {0} �͑��݂��܂����A�������݉\�Ȃ悤�ł��B", targetItem.FilePath);
				} catch(IOException e){
					result.AddError("�t�@�C�� {0} �͏������݂ł��Ȃ��悤�ł� : {1}", targetItem.FilePath, e.Message);
					Log.AddError("�t�@�C�� {0} �͏������݂ł��Ȃ��悤�ł� : {1}", targetItem.FilePath, e.Message);
					return result;
				} catch(UnauthorizedAccessException e){
					result.AddError("�t�@�C�� {0} �͏������݂ł��Ȃ��悤�ł� : {1}", targetItem.FilePath, e.Message);
					Log.AddError("�t�@�C�� {0} �͏������݂ł��Ȃ��悤�ł� : {1}", targetItem.FilePath, e.Message);
					return result;
				}
				result.Result = Parse(targetItem, targetItem.File);
			} else {
				// �t�@�C�����Ȃ�
				if(targetItem.Template != null){
					Log.AddInfo("�t�@�C�� {0} �͂���܂��񂪁A�O���[�o���e���v���[�g���w�肳��Ă��܂�(�e���v���[�g�� : {1})�B", targetItem.FilePath, targetItem.Template);
					result.Result = Parse(targetItem, null);
				} else {
					result.AddError("ID : {0} �̃t�@�C�� {1} ������܂��� (�O���[�o���e���v���[�g���w�肳��Ă��܂���)�B", targetItem.Id, targetItem.FilePath);
					Log.AddError("ID : {0} �̃t�@�C�� {1} ������܂��� (�O���[�o���e���v���[�g���w�肳��Ă��܂���)�B", targetItem.Id, targetItem.FilePath);
					return result;
				}
			}

			// �p�[�T�[�� null ��Ԃ�����I��
			if(result.Result == null){
				result.AddError("{0} �̃p�[�X�Ɏ��s���܂����B�p�[�X�I�����܂��B", targetItem.Id);
				Log.AddError("{0} �̃p�[�X�Ɏ��s���܂����B�t�@�C���ɏ������܂��ɏI�����܂��B", targetItem.Id);
				return result;
			}

			// �p�[�X���ʂƔ�r���Ă݂�
			if(result.Result == ReadData){
				// ���ʂ������Ȃ̂ŉ������Ȃ�
				Log.AddInfo("�p�[�X�������܂������A���ʂ̓p�[�X�O�ƕω�����܂���ł����B");
				result.Message = "�p�[�X�������܂������A���ʂ̓p�[�X�O�ƕω�����܂���ł����B";
			} else {
				// ���ʂ��Ⴄ�̂ŏ�������
				Log.AddInfo("�p�[�X���ʂ̓p�[�X�O�ƈقȂ�܂��B�t�@�C���ւ̏������݂����݂܂��B");
				try{
					if(!targetItem.File.Directory.Exists){
						targetItem.File.Directory.Create();
						Log.AddWarning("�f�B���N�g�� {0} ���݂���܂���ł����B�V�K�Ƀf�B���N�g�����쐬���܂����B", targetItem.File.Directory.FullName);
					}

					using(FileStream fs = targetItem.File.Open(FileMode.Create, FileAccess.Write, FileShare.None)){
						using(StreamWriter sw = new StreamWriter(fs, Project.Setting.HtmlEncodingObj)){
							sw.Write(result.Result);
							sw.Close();
						}
						fs.Close();
					}
					targetItem.File.Refresh();
					string s = string.Format("�t�@�C�� {0} �ɏ������݂܂����B(�T�C�Y: {1})", targetItem.FilePath, targetItem.File.Length);
					Log.AddInfo(s);
					result.Message = s;
				} catch(IOException e){
					result.AddError("�t�@�C�� {0} �ɏ������߂܂��� : {1}", targetItem.FilePath, e.Message);
					Log.AddError("�t�@�C�� {0} �ɏ������߂܂��� : {1}", targetItem.FilePath, e.Message);
					return result;
				} catch(UnauthorizedAccessException e){
					result.AddError("�t�@�C�� {0} �ɏ������ތ���������܂��� : {1}", targetItem.FilePath, e.Message);
					Log.AddError("�t�@�C�� {0} �ɏ������ތ���������܂��� : {1}", targetItem.FilePath, e.Message);
					return result;
				}
			}
			return result;
		}

// private ���\�b�h

		// �t�@�C������f�[�^��ǂݍ���
		private static string LoadFile(FileInfo targetFile, Encoding enc){
			string result = null;

			using(FileStream fs = targetFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
				using(StreamReader sr = new StreamReader(fs, enc)){
					result = sr.ReadToEnd();
					sr.Close();
				}
				fs.Close();
			}
			return result;
		}


		//meta charset���當�������������𔻕ʂ���
		private Encoding DetectEncodingFromMetaCharset(string data){
			Match m = myMetaCharsetReg.Match(data);
			if(m.Success){
				Log.AddInfo("meta charset�����o���܂����B {0}", m.Value);
				string charsetValue = m.Groups[1].Value;
				try{
					Encoding result = Encoding.GetEncoding(charsetValue);
					Log.AddInfo("charset: {1} / Encoding: {0} ", charsetValue, result);
					return result;
				} catch(ArgumentException e){
					Log.AddError("charset: {0} �ɑΉ�����Encoding���擾�ł��܂���ł���: {1}", charsetValue, e.Message);
					return null;
				}
			}

			Log.AddInfo("meta charset�����o�ł��܂���ł����B");
			return null;
		}

		// �e���v���[�g�E�G�N�X�|�[�g�E�v���p�e�B���p�[�X
		// �v���O�C������Ăׂ�悤��public
		public string GeneralParse(string data){
			bool commentDelete = false;
			if(Setting.TemplateCommentDelete == true) commentDelete = true;
			return GeneralParse(data, commentDelete);
		}

		private string GeneralParse(string data, bool commentDelete){
			// �������[�v�`�F�b�N
			if(++myDepthCounter > Setting.DepthMax){
				throw new ParserException("�e���v���[�g�������������" + Setting.DepthMax.ToString() + "�ɒB�������߁A�������[�v�̋^��������Ɣ��f���ď����𒆎~���܂����B�e���v���[�g�������̏���𑝂₵�����ꍇ�́A�ݒ�́uDepthMax/�e���v���[�g�ő吔�v�̒l�𑝂₵�Ă��������B");
			}

			MarkedData md = MarkedData.Parse(data);
			if(md == null) return data;

			// ��v�f�v���p�e�B�͏�ɍ폜
			if(md.EndMark == null) commentDelete = true;

			switch(md.MarkType){
			case MarkType.Export:
				return ExportParse(md, commentDelete);
			case MarkType.Template:
				return TemplateParse(md, commentDelete);
			case MarkType.Property:
				return PropertyParse(md, commentDelete);
			default:
				return data;
			}
		}


		// �e���v���[�g�̃p�[�X
		private string TemplateParse(MarkedData md, bool commentDelete){
			Log.AddInfo("{0} �e���v���[�g��F�m : {1} / commentDelete = {2}", Project.CurrentItem, md.MarkName, commentDelete);
			string result = md.FrontData;
			result += InnerTemplateParse(md, md.MarkName, commentDelete);

			// �����ċA����
			Log.AddInfo("{0} �e���v���[�g�̌��̃f�[�^������ : {1}", Project.CurrentItem, md.MarkName);
			result += GeneralParse(md.BackData, commentDelete);
			return result;
		}

		// �e���v���[�g�̒��g������
		private string InnerTemplateParse(MarkedData md, string templateName, bool commentDelete){
			Type pluginType = null;
			string result = "";

			// �v���O�C�����g����ݒ�̏ꍇ�A����
			if(Project.Setting.PluginKind != PluginKind.None){
				if(Project.PluginAssembly != null){
					string nameSpace = Project.GetPluginNameSpace();
					// ���O�� - �͖���
					string pName = nameSpace + '.' + templateName.Replace("-", "");
					pluginType = Project.PluginAssembly.GetType(pName);
				}

				// �v���O�C����������Ώ���
				if(pluginType != null && pluginType.IsSubclassOf(typeof(EcmPluginBase))){
					if(!commentDelete) result += md.StartMark;
					result += ProcessPlugin(pluginType, md);
					if(!commentDelete) result += md.EndMark;
					return result;
				}
			}

			// �e���v���[�g��T��
			EcmTemplate template = GetTemplate(templateName);
			if(template.Exists){
				template.Backup();
				string templateResult = null;
				if(Setting.EnableRegexTemplate){
					if(string.IsNullOrEmpty(md.InnerData)){
						Log.AddInfo("{0} �e���v���[�g�t�@�C���̐��K�\�����L���ł����A���݃R���e���c�̃f�[�^���������߁A�e���v���[�g�����̂܂܎g�p���܂� : {1}", Project.CurrentItem, templateName);
						templateResult = template.GetData();
					} else {
						Log.AddInfo("{0} �e���v���[�g�t�@�C���̐��K�\�������� : {1}", Project.CurrentItem, templateName);
						TemplateRegexParser trp = new TemplateRegexParser(Project, Log, templateName);
						templateResult = trp.Parse(md, template.GetData());
					}
				} else {
					templateResult = template.GetData();
				}
				if(!commentDelete) result += md.StartMark;
				result += GeneralParse(templateResult);
				if(!commentDelete) result += md.EndMark;
				return result;
			}

			// CSV �f�[�^�Ńe���v���[�g���w�肳��Ă��Ȃ����T��
			string csvTemplateName = Project.CurrentItem[templateName];
			if(!string.IsNullOrEmpty(csvTemplateName)){
				Log.AddInfo("{0} �f�[�^ {1} �Ńe���v���[�g {2} ���w�肳��Ă��܂��B", Project.CurrentItem, templateName, csvTemplateName);
				return InnerTemplateParse(md, csvTemplateName, commentDelete);
			}

			Log.AddAlert("{0} �e���v���[�g���݂���܂���ł����B: {1}", Project.CurrentItem, templateName);

			// �e���v���[�g�f�[�^���Ȃ���Β��g���ċA����
			result += md.StartMark;
			result += GeneralParse(md.InnerData);
			result += md.EndMark;
			return result;
		}



		// �G�N�X�|�[�g�̃p�[�X
		private string ExportParse(MarkedData md, bool commentDelete){
			Log.AddInfo("{0} �G�N�X�|�[�g��F�m : {1}", Project.CurrentItem, md.MarkName);

			string result = md.FrontData + md.StartMark;
			// �G�N�X�|�[�g�������߂�
			string exportData = myExp[md.MarkName];
			if(exportData == null){
				Log.AddWarning("{0} �G�N�X�|�[�g���ꂽ���e������܂���ł��� : {1}", Project.CurrentItem, md.MarkName);
				result += GeneralParse(md.InnerData, false);
			} else {
				Log.AddInfo("{0} �G�N�X�|�[�g���e������ : {1}", Project.CurrentItem, md.MarkName);
				result += GeneralParse(exportData, false);
			}
			result += md.EndMark;

			// �����ċA����
			Log.AddInfo("{0} �G�N�X�|�[�g�̌��̃f�[�^������ : {1}", Project.CurrentItem, md.MarkName);
			result += GeneralParse(md.BackData, commentDelete);
			return result;
		}


		// �v���p�e�B���p�[�X
		private string PropertyParse(MarkedData md, bool commentDelete){
			Log.AddInfo("{0} �v���p�e�B��F�m : {1}", Project.CurrentItem, md.MarkName);

			string result = md.FrontData;
			if(!commentDelete) result += md.StartMark;

			// �v���p�e�B�f�[�^�擾�^�[�Q�b�g���擾
			EcmItem targetItem = Project.GetItem(md.PropertyIdName);

			// �^�[�Q�b�g��������Ȃ���Ώ������Ȃ�
			if(targetItem == null){
				Log.AddAlert("{0} �ɂ� ID : {1} �̃I�u�W�F�N�g��v�����܂������A�����ł��܂���ł����B", Project.CurrentItem, md.PropertyIdName);
			} else {
				Object item = targetItem;
				Match ms = myMsReg.Match(md.PropertyParamName);
				while(ms.Success){
					string memberStr = ms.Groups[1].Value;
					Log.AddInfo("{0} �����o {1} ��F�m", Project.CurrentItem, memberStr);
					if(item == null){
						Log.AddAlert("{0} {1}{2} �ɂ� {3} ���Ăяo�����Ƃ��܂������A�I�u�W�F�N�g�� null �ł��B", Project.CurrentItem, md.PropertyIdName, md.PropertyParamName, memberStr);
					}
					try{
						item = Eval(item, memberStr);
					} catch(Exception e){
						throw new ParserException("{0} {1}{2} �ɂ� {3} ���Ăяo���܂������A��O���������܂��� : {4}", Project.CurrentItem, md.PropertyIdName, md.PropertyParamName, memberStr, e);
					}
					if(item == null){
						Log.AddWarning("{0} {1}{2} �ɂ� {3} ���Ăяo�������ʂ� null �ɂȂ�܂����B", Project.CurrentItem, md.PropertyIdName, md.PropertyParamName, memberStr);
						break;
					}
					ms = ms.NextMatch();
				}
				if(item == null){
					Log.AddWarning("{0} {1}{2} �̌��ʂ� null �ɂȂ�܂����B", Project.CurrentItem, md.PropertyIdName, md.PropertyParamName);
				} else {
					if(item is Array){
						foreach(Object o in item as Object[]){
							result += o.ToString();
						}
					} else {
						result += item.ToString();
					}
				}
			}

			if(!commentDelete) result += md.EndMark;

			// �����ċA����
			Log.AddInfo("{0} �v���p�e�B�̌��̃f�[�^������ : {1}", Project.CurrentItem, md.MarkName);
			result += GeneralParse(md.BackData, commentDelete);
			return result;
		}



		// �e���v���[�g�I�u�W�F�N�g���擾����
		private EcmTemplate GetTemplate(string templateName){
			EcmTemplate result = new EcmTemplate(Project, templateName);
			return result;
		}



		// �[�����\�b�h�̎��s
		// ���� = EcmString or EcmString[]
		public Object Eval(Object item, string memberStr){
			if(item is Array){
				Log.AddInfo("{0} Eval����: �I�u�W�F�N�g�� {1} �ł��B�z��ł��̂ŃC�e���[�^�������s���܂��B", Project.CurrentItem, item);
				item = EvalArray(item as Object[], memberStr);
			} else if(item is EcmItem){
				Log.AddInfo("{0} Eval����: �I�u�W�F�N�g�� EcmItem {1} �ł��B", Project.CurrentItem, item);
				item = EvalItem(item as EcmItem, memberStr);
			} else if(item is EcmString){
				Log.AddInfo("{0} Eval����: �I�u�W�F�N�g�� {1} {2} �ł��B", Project.CurrentItem, item.GetType(), item);
				item = EvalString(item as EcmString, memberStr);
			} else {
				Log.AddInfo("{0} Eval����: �I�u�W�F�N�g�� ������(����:{1})�ł��B", Project.CurrentItem, item.ToString().Length);
				item = EvalString(new EcmString(item.ToString(), Project), memberStr);
			}
			return item;
		}

		// EcmItem �ɑ΂��� Eval ���������s���܂��B
		private Object EvalItem(EcmItem ei, string memberStr){
			if(memberStr == "") return ei;

			string[] members = memberStr.Split('(', ')');
			string memberName = members[0];
			string memberParam = null;

			// memberParam �� null���v���p�e�B�A""�������Ȃ����\�b�h�A�������遨���������\�b�h
			if(members.Length > 1){
				// ���\�b�h
				Log.AddInfo("{0} ���\�b�h {1} ��F�m", Project.CurrentItem, memberStr);
				MethodInfo m;
				memberParam = members[1];
				if(memberParam == ""){
					// �����i�V���\�b�h
					Log.AddInfo("{0} ���\�b�h {1} �Ɉ����͎w�肳��Ă��܂���", Project.CurrentItem, memberStr);
					m = typeof(EcmItem).GetMethod(memberName, Type.EmptyTypes);
					if(m != null) return m.Invoke(ei, null);
				} else {
					// ���������\�b�h
					Log.AddInfo("{0} ���\�b�h {1} �̈����� {2} �ł�", Project.CurrentItem, memberStr, memberParam);
					m = typeof(EcmItem).GetMethod(memberName, new Type[]{typeof(string)});
					if(m != null) return m.Invoke(ei, new Object[]{memberParam});
				}
				// �݂���Ȃ�
				Log.AddWarning("���\�b�h��������܂���ł��� : {0}.{1}", ei, memberStr);
			} else {
				// �v���p�e�B
				Log.AddInfo("{0} �v���p�e�B {1} ��F�m", Project.CurrentItem, memberStr);
				PropertyInfo p;

				// �C���f�N�T�ɂ�����B
				if(ei[memberName] != null) return ei[memberName];

				// EcmItem �̃f�t�H���g�v���p�e�B�ɂ�����
				p = typeof(EcmItem).GetProperty(memberName);
				if(p != null) return p.GetValue(ei, null);

				// Export�ɂ����� (���̏�� Parse ����)
				string expTarget = ei.GetExport(this, memberStr);
				if(expTarget != null){
					Log.AddInfo("{0} �� Export {1} ���擾���܂��� (�T�C�Y : {2})", ei.FqId, memberStr, expTarget.Length);
					return GeneralParse(expTarget);
				}

				// �݂���Ȃ�
				Log.AddWarning("�v���p�e�B��������܂���ł��� : {0}.{1}", ei, memberStr);
				return null;
			}
			return ei;
		}

		// EcmString �Ƃ��̔h���N���X�ɑ΂��� Eval ���������s���܂��B
		private Object EvalString(EcmString o, string memberStr){
			if(memberStr == "") return o;

			Type t = o.GetType();

			string[] members = memberStr.Split('(', ')');
			string memberName = members[0];
			string memberParam = null;

			// memberParam �� null���v���p�e�B�A""�������Ȃ����\�b�h�A�������遨���������\�b�h
			if(members.Length > 1){
				// ���\�b�h
				Log.AddInfo("{0} ���\�b�h {1} ��F�m", Project.CurrentItem, memberStr);
				MethodInfo m;
				memberParam = members[1];
				if(memberParam == ""){
					// �����i�V���\�b�h
					Log.AddInfo("{0} ���\�b�h {1} �Ɉ����͎w�肳��Ă��܂���", Project.CurrentItem, memberStr);
					m = t.GetMethod(memberName, Type.EmptyTypes);
					if(m != null) return m.Invoke(o, null);
				} else {
					// ���������\�b�h
					Log.AddInfo("{0} ���\�b�h {1} �̈����� {2} �ł�", Project.CurrentItem, memberStr, memberParam);
					m = t.GetMethod(memberName, new Type[]{typeof(string)});
					if(m != null) return m.Invoke(o, new Object[]{memberParam});
				}
				// �݂���Ȃ�
				Log.AddWarning("���\�b�h��������܂���ł��� : {0}.{1}", o, memberStr);
			} else {
				// �v���p�e�B
				Log.AddInfo("{0} �v���p�e�B {1} ��F�m", Project.CurrentItem, memberStr);
				PropertyInfo p;

				p = t.GetProperty(memberName);
				if(p != null) return p.GetValue(o, null);

				// �݂���Ȃ�
				Log.AddWarning("�v���p�e�B��������܂���ł��� : {0}.{1}", o, memberStr);
			}
			return o;
		}

		private Object EvalArray(Object[] items, string memberStr){
			Log.AddInfo("{0} �C�e���[�^�����̑Ώ�: {1}(�v�f��{2}) .{3}", Project.CurrentItem, items.GetType(), items.Length, memberStr);

			Object[] result = new Object[items.Length];
			for(int i = 0; i < items.Length; i++){
				Log.AddInfo("{0} �C�e���[�^����({1}/{2})", Project.CurrentItem, i+1, items.Length);
				Object o = Eval(items[i], memberStr);
				Log.AddInfo("{0} �C�e���[�^����{1}�̌���: {2}", Project.CurrentItem, i+1, o);
				if(o is EcmItem){
					result[i] = o as EcmItem;
				} else {
					result[i] = new EcmString(o.ToString(), Project);
				}
			}

			return result;
		}




		// �v���O�C������
		// �v���O�C���}�l�[�W������Ă΂�邱�Ƃ�����̂� public
		public string ProcessPlugin(Type pluginType, MarkedData md){
			Log.AddInfo("{0} �v���O�C���𔭌� : {1}", Project.CurrentItem, pluginType.Name);

			// ���̃v���O�C�����Ă΂��͉̂����?
			if(myPluginProcessCounter.ContainsKey(pluginType)){
				myPluginProcessCounter[pluginType]++;
			} else {
				myPluginProcessCounter.Add(pluginType, 1);
			}
			int calledCount = myPluginProcessCounter[pluginType];

			string result = null;
			ConstructorInfo ci = null;
			EcmPluginBase pluginInstance = null;

			try{
				ci = pluginType.GetConstructor(EcmPluginBase.PluginConstractorParams);
				if(ci == null){
					Log.AddAlert("{0} �R���X�g���N�^���擾�ł��܂���ł����B", Project.CurrentItem);
					return null;
				}
			} catch(Exception e) {
				Log.AddAlert("{0} �R���X�g���N�^�̎擾���ɗ�O���������܂����B{1}", Project.CurrentItem, e);
				return null;
			}

			try{
				Object o = ci.Invoke(new Object[]{new EcmPluginParams(this, Setting, Project, md, calledCount)});
				pluginInstance = o as EcmPluginBase;
				if(pluginInstance == null){
					Log.AddAlert("{0} �R���X�g���N�^�����s���܂������A�I�u�W�F�N�g���쐬����܂���ł����B", Project.CurrentItem);
					return null;
				}
			} catch(Exception e) {
				Log.AddAlert("{0} �R���X�g���N�^�̎��s���ɗ�O���������܂����B{1}", Project.CurrentItem, e);
				return null;
			}

			try{
				pluginInstance.Parse();
				result = pluginInstance.Document.ToString();
			} catch(Exception e) {
				Log.AddAlert("{0} �v���O�C���̎��s���ɗ�O���������܂����B{1}", Project.CurrentItem, e);
				return null;
			}

			Log.AddInfo("{0} �v���O�C��{1}�̏��������B���ʂ̕�����:{2}", Project.CurrentItem, pluginType.Name, result.Length);

			return result;
		}


	}


// Exception

	[Serializable()]
	public class ParserException : Exception{
		public ParserException(Object o) : base(o.ToString()){}
		public ParserException(string s, params Object[] o) : base(string.Format(s, o)){}
		public ParserException(string s) : base(s){}
		public ParserException(string s, Exception e) : base(s, e){}
	};

}




