using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Bakera.Eccm{

	[XmlRoot(Namespace = "", ElementName = "eccm-setting")]
	public class Setting{
		private XmlDocument myXml = null; // �f�[�^�\�[�X�� XML
		private string myId = null;
		private DateTime myStartTime;
		private FileInfo myBaseFile;
		private string myProjectName = "�v���W�F�N�g������";
		private string myRootDir = "c:\\";
		private string myDocumentDir = "document_root";
		private string myTemplateDir = "template";
		private string mySchemaDir = "schema";
		private string myCsvPath = "data.csv";
		private string myCsvLinkPath = "";
		private string myTemplateExt = ".txt";
		private string myPreviewRootUrl = "http://127.0.0.1"; // ���[�g�ƂȂ� URL�A������ / �͕s�v
		private int myDepthMax = 100; // �������[�v���o�p�̃J�E���^���
		private string myAnchorTemplate = "<a href=\"{0}\">{1}</a>";
		private string myStayTemplate = "<em class=\"stay\">{0}</em>";
		private bool myTemplateCommentDelete = true; // �e���v���[�g����ECM�R�����g���폜���邩�ǂ����Btrue�͍폜����
		private bool myAutoPathGenerate = false; // �p�X�w�肪�����Ƃ��� ID ����t�@�C�����������������邩�ǂ����Bfalse �͐������Ȃ�
		private bool myEnableRegexTemplate = false;
		private string myCsvEncoding = "Shift_JIS"; // CSV �̕�������������
		private string myHtmlEncoding = "Shift_JIS"; // HTML �̕�������������
		private bool myHtmlEncodingDetectFromMetaCharset = false; // HTML �̕�������������
		private string myColorSeparateRule;
		private string myParsePermissonRule;
		private string mySvnUpdateCommand;
		private string myHandler = "ProjectSetting,AllParse";
		private string myImageDir;
		private string myHiddenColumn;
		private string[] myHiddenColumnList;
		private string myDefaultSortColumn;
		private string myDefaultGrobalTemplate;
		private string myDefaultSchemaName;
		private string myIndexLinkSuffix;
		private PluginKind myPluginKind;

		private static Assembly myAssembly;
		private static Type[] myHandlerTypes;
		private static Type[] myProcessorTypes;


// �ÓI�������R���X�g���N�^
		static Setting(){
			myAssembly = Assembly.GetAssembly(typeof(Eccm));
			Type[] types = myAssembly.GetTypes();

			List<Type> handlerTypes = new List<Type>();
			List<Type> processorTypes = new List<Type>();

			foreach(Type t in types){
				if(t.IsSubclassOf(typeof(EcmProjectHandler))) handlerTypes.Add(t);
				if(t.IsSubclassOf(typeof(EcmProcessorBase))) processorTypes.Add(t);
			}
			myHandlerTypes = handlerTypes.ToArray();
			myProcessorTypes = processorTypes.ToArray();
		}

// �R���X�g���N�^

		public Setting(){
			myStartTime = DateTime.Now;
		}

// �ÓI�v���p�e�B

		[XmlIgnore]
		public static Assembly Assembly{
			get{return myAssembly;}
		}
		[XmlIgnore]
		public static Type[] HandlerTypes{
			get{return myHandlerTypes;}
		}
		[XmlIgnore]
		public static Type[] ProcessorTypes{
			get{return myProcessorTypes;}
		}


// �ҏW�s�\�v���p�e�B

		[XmlIgnore]
		public DateTime StartTime{
			get{return myStartTime;}
		}

		// ���̃v���W�F�N�g�̃V�X�e���t�@�C���Q���i�[�����x�[�X�f�B���N�g�����擾���܂��B
		[XmlIgnore]
		public DirectoryInfo BaseDir{
			get{return myBaseFile.Directory;}
		}

		// ���̃v���W�F�N�g�̍쐬�� (�V�X�e���t�@�C���Q���i�[�����x�[�X�f�B���N�g���̍쐬��) ���擾���܂��B
		[XmlIgnore]
		public DateTime CreatedTime{
			get{
				if(this.BaseDir == null || !this.BaseDir.Exists) return DateTime.MinValue;
				return this.BaseDir.LastWriteTime;
			}
		}


		// ���̐ݒ� XML �t�@�C�����g���擾���܂��B
		[XmlIgnore]
		public FileInfo BaseFile{
			get{return myBaseFile;}
			set{myBaseFile = value;}
		}

		// Id ��ݒ�E�擾���܂��B
		[XmlIgnore]
		public string Id{
			get{return myId;}
			set{myId = value;}
		}

		// �h�L�������g�f�B���N�g�����擾���܂��B
		[XmlIgnore]
		public DirectoryInfo DocumentFullPath{
			get{
				if(DocumentDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, DocumentDir));
			}
		}

		// �e���v���[�g�f�B���N�g�����擾���܂��B
		[XmlIgnore]
		public DirectoryInfo TemplateFullPath{
			get{
				if(TemplateDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, TemplateDir));
			}
		}

		// �v���r���[�摜�f�B���N�g�����擾���܂��B
		[XmlIgnore]
		public DirectoryInfo ImageFullPath{
			get{
				if(ImageDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, ImageDir));
			}
		}

		// �X�L�[�}�f�B���N�g�����擾���܂��B
		[XmlIgnore]
		public DirectoryInfo SchemaDirInfo{
			get{
				if(SchemaDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, SchemaDir));
			}
		}

		// �O���f�[�^�f�B���N�g�����擾���܂��B
		[XmlIgnore]
		public DirectoryInfo ExtDataDirInfo{
			get{
				if(SchemaDir == null) return null;
				return new DirectoryInfo(Path.Combine(RootDir, ExtDataDir));
			}
		}

		// CSV �t�@�C���̃p�X���擾���܂��B
		[XmlIgnore]
		public string CsvFullPath{
			get{
				if(CsvPath == null) return null;
				return Path.Combine(RootDir, CsvPath);
			}
		}


		[XmlIgnore]
		public Encoding HtmlEncodingObj{
			get{return Util.GetEncoding(HtmlEncoding);}
		}

		[XmlIgnore]
		public Encoding CsvEncodingObj{
			get{return Util.GetEncoding(CsvEncoding);}
		}

		[XmlIgnore]
		public string[] HiddenColumnList{
			get{
				if(myHiddenColumnList != null) return myHiddenColumnList;
				if(string.IsNullOrEmpty(myHiddenColumn)){
					myHiddenColumnList = new string[0];
				} else {
					string[] hiddenStrs = myHiddenColumn.Split(',');
					myHiddenColumnList = new string[hiddenStrs.Length];
					for(int i=0; i < hiddenStrs.Length; i++){
						myHiddenColumnList[i] = hiddenStrs[i].Trim();
					}
				}
				return myHiddenColumnList;
			}
		}


// �ҏW�\�v���p�e�B

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.General)]
		[EccmDescription("�v���W�F�N�g��", "�v���W�F�N�g�̖��̂��w�肵�܂��B")]
		public string ProjectName{
			get{ return myProjectName; }
			set{ myProjectName = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("�f�[�^��f�B���N�g��", "�f�[�^�̊�ƂȂ�f�B���N�g�����w�肵�܂��BDocumentDir�Ȃǂ̐ݒ�́A���̃f�B���N�g������̑��΃p�X�Ƃ��ĉ��߂���܂��B")]
		public string RootDir{
			get{ return myRootDir; }
			set{ myRootDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("CSV�p�X", "�f�[�^�t�@�C���̏ꏊ���w�肵�܂��BRootDir ����̑��΃p�X�w��Ƃ��ĉ��߂���܂��B")]
		public string CsvPath{
			get{ return myCsvPath; }
			set{ myCsvPath = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("�h�L�������g�f�B���N�g��", "HTML �̃f�[�^���u�����f�B���N�g�����w�肵�܂��BRootDir ����̑��΃p�X�w��Ƃ��ĉ��߂���܂��B")]
		public string DocumentDir{
			get{ return myDocumentDir; }
			set{ myDocumentDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("�e���v���[�g�f�B���N�g��","�e���v���[�g�̃f�[�^���u�����f�B���N�g�����w�肵�܂��BRootDir ����̑��΃p�X�w��Ƃ��ĉ��߂���܂��B")]
		public string TemplateDir{
			get{ return myTemplateDir; }
			set{ myTemplateDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("�X�L�[�}�f�B���N�g��", "XML Validation �Ŏg�p����X�L�[�}���u�����f�B���N�g�����w�肵�܂��BRootDir ����̑��΃p�X�w��Ƃ��ĉ��߂���܂��B")]
		public string SchemaDir{
			get{ return mySchemaDir; }
			set{ mySchemaDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("�O���f�[�^�f�B���N�g��", "�v���O�C��������g�p����O���f�[�^���u�����f�B���N�g�����w�肵�܂��BRootDir ����̑��΃p�X�w��Ƃ��ĉ��߂���܂��B")]
		public string ExtDataDir{ get; set; }

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Directory)]
		[EccmDescription("�v���r���[�摜�f�B���N�g��", "�v���r���[�摜��z�u����f�B���N�g�����w�肵�܂��BRootDir ����̑��΃p�X�w��Ƃ��ĉ��߂���܂��B")]
		public string ImageDir{
			get{ return myImageDir; }
			set{ myImageDir = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Link)]
		[EccmDescription("CSV�����N�p�X", "CSV�t�@�C�����J�����߂� URL ���w�肵�܂��Bfile: �� http: �Ŏn�܂��� URI ���w�肷��K�v������܂��BCSV ���u����Ă���f�B���N�g�����J���悤�ɂ���̂��I�X�X���ł��B")]
		public string CsvLinkPath{
			get{ return myCsvLinkPath; }
			set{ myCsvLinkPath = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Link)]
		[EccmDescription("�v���r���[���[�gURL", "�v���r���[���̃h�L�������g���[�g���w�� URL ���w�肵�܂��B��� URI ���w�肷��K�v������܂��B���� URL �̓v���r���[�p�̃����N�Ɏg�p����܂��B�� : http://mde7:8000/")]
		public string PreviewRootUrl{
			get{ return myPreviewRootUrl; }
			set{ myPreviewRootUrl = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("�e���v���[�g�g���q", "�W���̃e���v���[�g�t�@�C���̊g���q���w�肵�܂��B")]
		public string TemplateExt{
			get{ return myTemplateExt; }
			set{ myTemplateExt = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("�e���v���[�g�ő吔", "��̃h�L�������g�Ŏg�p�ł���e���v���[�g�̐��̏�����w�肵�܂��B�p�[�X���ɏ��������e���v���[�g�������̐��𒴂���ƁA�������[�v�Ɣ��f���ăp�[�X������ł��؂�܂��B")]
		public int DepthMax{
			get{ return myDepthMax; }
			set{ myDepthMax = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("�A���J�[�e���v���[�g", "GetAnchor() �[�����\�b�h�Ŏg�p����A���J�[�̐��`���w�肵�܂��B�u{0}�v�Ɓu{1}�v�����ꂼ��R���e���c�̃p�X����у^�C�g���ƂȂ�܂��B")]
		public string AnchorTemplate{
			get{ return myAnchorTemplate; }
			set{ myAnchorTemplate = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("���������N�e���v���[�g", "GetAnchor() �[�����\�b�h�Ŏg�p����A���������N�p�̐��`���w�肵�܂��B�u{0}�v���R���e���c�̃^�C�g���ƂȂ�܂��B")]
		public string StayTemplate{
			get{ return myStayTemplate; }
			set{ myStayTemplate = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("���URL�h���C��", "GetAbsUrlAnchor() �[�����\�b�h�Ŏg�p����A���URL�����̂��߂̃h���C�������w�肵�܂��B�X�L�[���������Ďw�肷��K�v������܂��B")]
		public string AbsUrlDomain{
			get; set;
		}

		[EccmEditable("�e���v���[�g����ECM�R�����g���폜����")]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("ECM�R�����g�̍폜", "�e���v���[�g����ECM�R�����g���폜���邩�ǂ������w�肵�܂��Btrue �ɂ���ƍ폜���܂��B")]
		public bool TemplateCommentDelete{
			get{ return myTemplateCommentDelete; }
			set{ myTemplateCommentDelete = value; }
		}

		[EccmEditable("�p�X��������������")]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("�p�X��������", "�p�X�w�肪�����Ƃ��AID ����t�@�C�����������������邩�ǂ����w�肵�܂��Btrue �ɂ���� /[ID].html ���t�@�C�����Ƃ��ď������܂��B")]
		public bool AutoPathGenerate{
			get{ return myAutoPathGenerate; }
			set{ myAutoPathGenerate = value; }
		}

		[EccmEditable("�e���v���[�g�̐��K�\���u����L���ɂ���")]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("�e���v���[�g�̐��K�\���u��", "�e���v���[�g�t�@�C�����̐��K�\���ɂ��u�������e���v���[�g�v�̃T�|�[�g��L���ɂ��邩�ǂ����w�肵�܂��Btrue �ɂ���ƗL���ɂȂ�܂����A�������x���ቺ���邩������܂���B")]
		public bool EnableRegexTemplate{
			get{ return myEnableRegexTemplate; }
			set{ myEnableRegexTemplate = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("CSV��������������", "CSV�t�@�C���̕����������������w�肵�܂��BMicrosoft Excel 2003 ���ۑ����� CSV �̏ꍇ�� Shift_JIS �ƂȂ�܂��BXML�X�v���b�h�V�[�g�𗘗p����ꍇ�A���̐ݒ�͖�������܂��B")]
		public string CsvEncoding{
			get{ return myCsvEncoding; }
			set{ myCsvEncoding = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("HTML��������������", "HTML�t�@�C���̕����������������w�肵�܂��B�e���v���[�g�t�@�C�����������Ŏw�肵�����������������ɍ��킹��K�v������܂��B")]
		public string HtmlEncoding{
			get{ return myHtmlEncoding; }
			set{ myHtmlEncoding = value; }
		}

		[EccmEditable("meta charset����̕����������������ʂ�L���ɂ���")]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("meta charset����̕�����������������", "Publish�O��HTML�t�@�C����meta charset��ǂ�ŕ����������������������ʂ��܂��Btrue�ɂ���ƗL���ɂȂ�܂��B")]
		public bool HtmlEncodingDetectFromMetaCharset{
			get{ return myHtmlEncodingDetectFromMetaCharset; }
			set{ myHtmlEncodingDetectFromMetaCharset = value; }
		}
		
		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("�f�t�H���g�O���[�o���e���v���[�g", "�f�t�H���g�̃O���[�o���e���v���[�g���w�肷�邱�Ƃ��ł��܂��B�ʂ� template �̒l���w�肳��Ă����ꍇ�͂����炪�D�悳��܂��B")]
		public string DefaultGrobalTemplate{
			get{ return myDefaultGrobalTemplate; }
			set{ myDefaultGrobalTemplate = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("�����N�̖����������������", "Uri() ���\�b�h�Ȃǂ� Uri ���������ꂽ�Ƃ��A�����Ɏw�蕶���񂪂���΍폜���܂��B�uindex.html�v���w�肷��ƁA/ �ŏI��郊���N���������ꂽ�肵�܂��B")]
		public string IndexLinkSuffix{
			get{ return myIndexLinkSuffix; }
			set{ myIndexLinkSuffix = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Parser)]
		[EccmDescription("�v���O�C���̌���", "�v���O�C���̌���� None(�Ȃ�) / CSharp(C#) / JScript (JScript.NET) ����I�����܂��B")]
		public PluginKind PluginKind{
			get{ return myPluginKind; }
			set{ myPluginKind = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("�f�t�H���g�\�[�g��", "�f�t�H���g�Ń\�[�g�Ɏg�p�������w�肵�܂��B")]
		public string DefaultSortColumn{
			get{ return myDefaultSortColumn; }
			set{ myDefaultSortColumn = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("�F�������[��", "�\�̐F�����Ɏg�p����񖼂Ɛ��K�\�����w�肵�܂��B() �Ŋ���ꂽ�������قȂ�ꍇ�ɈقȂ�O���[�v�Ƃ݂Ȃ��܂��B���Ƃ��΁Atitle=(.{3}) �Ǝw�肷��� title �̓���3�������r���ĐF�������܂��B�񖼂��ȗ������ ID ���r���܂��B")]
		public string ColorSeparateRule{
			get{ return myColorSeparateRule; }
			set{ myColorSeparateRule = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("�F�t�����[��1", "�\�̐F�����Ɏg�p����񖼂Ɛ��K�\�����w�肵�A�Y������s�̐F��ς��܂��B���Ƃ��΁Astatus=(����|�m�F��) �ȂǂƎw�肷��ƁA�Y������s��class=\"pattern1\"�����܂��B")]
		public string ColorRule1{ get; set;}


		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("�F�t�����[��2", "�\�̐F�����Ɏg�p����񖼂Ɛ��K�\�����w�肵�A�Y������s�̐F��ς��܂��B���Ƃ��΁Astatus=(����|�m�F��) �ȂǂƎw�肷��ƁA�Y������s��class=\"pattern2\"�����܂��B")]
		public string ColorRule2{ get; set;}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("�F�t�����[��3", "�\�̐F�����Ɏg�p����񖼂Ɛ��K�\�����w�肵�A�Y������s�̐F��ς��܂��B���Ƃ��΁Astatus=(����|�m�F��) �ȂǂƎw�肷��ƁA�Y������s��class=\"pattern3\"�����܂��B")]
		public string ColorRule3{ get; set;}


		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("��\����", "��\���ɂ������񖼂��J���}�ŋ�؂��ċL�q���܂��B")]
		public string HiddenColumn{
			get{ return myHiddenColumn; }
			set{ myHiddenColumn = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.View)]
		[EccmDescription("Parse������", "Parse��������������w�肵�܂��B���w��̏ꍇ�͑S�ċ�����܂��B�ݒ�� : phase=1")]
		public string ParsePermissonRule{
			get{ return myParsePermissonRule; }
			set{ myParsePermissonRule = value; }
		}


		[EccmFieldGenre(EccmFieldGenreType.Misc)]
		[EccmDescription("�V�F���R�}���h", "(���̋@�\�͖������ł��B)�u�R�}���h���s�v�Ŏ��s����R�}���h�̏����l��ݒ肵�܂��B�� : svn up \\projects\\ECCM\\eccm_contents\\RMTL\\html")]
		public string SvnUpdateCommand{
			get{ return mySvnUpdateCommand; }
			set{ mySvnUpdateCommand = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Misc)]
		[EccmDescription("ECCM�n���h�����j���[", "�㕔�̃��j���[�ɕ\������ EccmHandler �̌^�����J���}��؂�ŗ񋓂��Ďw�肵�܂��B")]
		public string Handler{
			get{ return myHandler; }
			set{ myHandler = value; }
		}

		[EccmEditable]
		[EccmFieldGenre(EccmFieldGenreType.Misc)]
		[EccmDescription("�f�t�H���g�X�L�[�}��", "XML Validation �Ŏg�p����f�t�H���g�̃X�L�[�}���w�肵�܂��Bschema��Œl���w�肳��Ă���ꍇ�A�X�̎w��̕����D�悳��܂��B")]
		public string DefaultSchemaName{
			get{ return myDefaultSchemaName; }
			set{ myDefaultSchemaName = value; }
		}


// �ÓI���\�b�h

		// Xml �t�@�C������ Setting �����[�h���܂��B
		public static Setting GetSetting(string targetFile){
			return GetSetting(new FileInfo(targetFile));
		}

		// Xml �t�@�C������ Setting �����[�h���܂��B
		public static Setting GetSetting(FileInfo targetFile){

			XmlSerializer xs = new XmlSerializer(typeof(Setting));
			Setting result;

			using(FileStream fs = targetFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
				result = xs.Deserialize(fs) as Setting;
				fs.Close();
			}

			if(result == null) return null;
			result.Id = Path.GetFileNameWithoutExtension(targetFile.Name);
			result.BaseFile = targetFile;

			return result;
		}




// private���\�b�h
		private string LoadData(string nodeName){
			XmlNodeList xnl = myXml.DocumentElement.GetElementsByTagName(nodeName);
			if(xnl.Count == 0) return null;
			return xnl[0].InnerText;
		}

		private string LoadDir(string nodeName){
			string s = LoadData(nodeName);
			if(String.IsNullOrEmpty(s)) return null;
			s = s.Replace('/', '\\');
			if(!s.EndsWith("\\")) s += '\\';
			return s;
		}

		private bool? LoadBoolData(string nodeName){
			string s = LoadData(nodeName);
			if(String.IsNullOrEmpty(s)) return null;
			if(s.Equals("true", StringComparison.CurrentCultureIgnoreCase)) return true;
			if(s.Equals("false", StringComparison.CurrentCultureIgnoreCase)) return false;
			return null;
		}

		private int? LoadIntData(string nodeName){
			string s = LoadData(nodeName);
			if(s == null) return null;
			int result = 0;
			try{
				result = Convert.ToInt32(s);
			} catch(FormatException){
				return null;
			}
			return result;
		}

	}



// �J�X�^������


	// Setting �̃t�B�[���h�ɂ��̑������w�肷��ƁA�ҏW��ʂŕҏW�\�ƂȂ�܂��B
	// �����͐ݒ��ʂ� description �ł��B
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class EccmEditableAttribute : Attribute{

		private string myLabel;

		public EccmEditableAttribute(string label){
			myLabel = label;	
		}
		public EccmEditableAttribute(){}

		// ���x�����擾���܂�
		// bool�l�̃`�F�b�N�{�b�N�X�̃��x���Ƃ��Ďg�p����܂��B
		public string Label{
			get{return myLabel;}
		}

	}


	// Setting �̃t�B�[���h�̃W���������w�肷�鑮���ł��B
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class EccmFieldGenreAttribute : Attribute{

		private EccmFieldGenreType myGenre;
		public EccmFieldGenreAttribute(EccmFieldGenreType genre){
			myGenre = genre;	
		}

		public EccmFieldGenreType Genre{
			get{return myGenre;}
		}

	}

	public enum EccmFieldGenreType{
		General,
		Directory,
		Link,
		Parser,
		View,
		Misc
	}


	// Setting �̃t�B�[���h�ɂ��̑������w�肷��ƁA�ҏW��ʂŕҏW�\�ƂȂ�܂��B
	// �����͐ݒ��ʂ� description �ł��B
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class EccmDescriptionAttribute : Attribute{

		private string myDescription;
		private string myName;

		public EccmDescriptionAttribute(string name, string desc){
			myName = name;	
			myDescription = desc;	
		}

		public string Name{
			get{return myName;}
		}

		public string Description{
			get{return myDescription;}
		}

		public override string ToString(){
			return myDescription;
		}
	}


}




