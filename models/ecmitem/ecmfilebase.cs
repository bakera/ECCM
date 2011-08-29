using System;
using System.IO;
using System.Text;


namespace Bakera.Eccm{
	public abstract class EcmFileBase : EcmString{


		protected FileInfo myFile;
		protected string myData = null;
		protected string myPath;

// �R���X�g���N�^

		public EcmFileBase(string path, EcmProject project) : this(project){
			this.Path = path;
		}

		public EcmFileBase(EcmProject project) : base(project){
			myProject = project;
		}



// �v���p�e�B
		// �p�X���擾���܂��B
		public virtual string Path{
			get{ return myPath; }
			private set{ myPath = value.Replace("\\", "/"); }
		}

		// ���� EcmItem �ɑΉ�����t�@�C�������� FileInfo ���擾���܂��B
		public virtual FileInfo File{
			get{
				if(this.Path == null) return null;
				if(myFile == null) myFile = new FileInfo(PathToFilename(this.Path));
				return myFile;
			}
		}

		// ���� EcmItem �ɑΉ�����t�@�C���̗L�����擾���܂��B
		public virtual bool Exists{
			get{
				if(this.File == null) return false;
				return this.File.Exists;
			}
		}

		// �t�@�C���̃t���p�X���擾���܂��B
		public string FilePath{
			get{
				if(this.File == null) return null;
				return this.File.FullName;
			}
		}

		public long FileSize{
			get{
				if(this.File == null) return 0;
				if(!this.File.Exists) return 0;
				return this.File.Length;
			}
		}

		// �t�@�C���T�C�Y��Z�k�`�Ŏ���������������擾���܂��B�t�@�C�����Ȃ��Ƃ��� "-" ��Ԃ��܂��B
		public string FileSizeShort{
			get{
				long size = FileSize;
				if(size == 0) return "-";
				if(size < 1024) return size.ToString();
				return String.Format("{0}K", size/1024);
			}
		}

		// 1000 ���݂ŒP�ʂ̕ς��A�t�@�C���T�C�Y��������������擾���܂��B
		public string FileSize1000{
			get{
				long size = FileSize;
				if(size < 1000) return size.ToString();
				if(size < 1000000 )return String.Format("{0}KB", size/1000);
				return String.Format("{0}MB", size/1000000);
			}
		}

		// 1024 ���݂ŒP�ʂ̕ς��A�t�@�C���T�C�Y��������������擾���܂��B
		public string FileSize1024{
			get{
				long size = FileSize;
				if(size < 1024) return size.ToString();
				if(size < 1024 * 1024 )return String.Format("{0}KB", size/1024);
				return String.Format("{0}MB", size/(1024 * 1024));
			}
		}

		// �t�@�C���T�C�Y�̐��l���L���o�C�g�Ŏ擾���܂��B
		public long FileSizeKB{
			get{
				decimal size = (decimal)FileSize / 1000m;
				return (long)Math.Ceiling(size);
			}
		}
		// �t�@�C���T�C�Y�̐��l���L�r�o�C�g�Ŏ擾���܂��B
		public long FileSizeKiB{
			get{
				decimal size = (decimal)FileSize / 1024m;
				return (long)Math.Ceiling(size);
			}
		}

		// �t�@�C���T�C�Y�̐��l�����K�o�C�g�Ŏ擾���܂��B
		public long FileSizeMB{
			get{
				decimal size = (decimal)FileSize / 1000000m;
				return (long)Math.Ceiling(size);
			}
		}
		// �t�@�C���T�C�Y�̐��l�����r�o�C�g�Ŏ擾���܂��B
		public long FileSizeMiB{
			get{
				decimal size = (decimal)FileSize / (1024m*1024m);
				return (long)Math.Ceiling(size);
			}
		}


// ���\�b�h

		// Project.CurrentItem ����̑���URI���擾���܂��B
		public string RelUri(){
			EcmFileBase target = Project.CurrentItem;
			if(target == this) return "";

			Uri targetUri = new Uri(Project.Setting.PreviewRootUrl + this.Path);
			Uri fromUri = new Uri(Project.Setting.PreviewRootUrl + Project.CurrentItem.Path);
			if(targetUri.AbsoluteUri == fromUri.AbsoluteUri) return "";

			Uri resultUri = fromUri.MakeRelativeUri(targetUri);
			string result = resultUri.ToString();
			if(Project.Setting.IndexLinkSuffix != null){
				result = Util.CutRight(result, Project.Setting.IndexLinkSuffix);
			}
			// �����܂ŗ��Č��ʂ���̏ꍇ�A"./" �̉��߂Ɏ��s���Ă���
			if(result == "") return "./";
			return result;
		}

		public string RelUrl(){
			return RelUri();
		}

		// �C�ӂ̕�����𑊑�URI�ɕϊ����܂��B
		public string RelUri(string s){

			Uri targetUri = new Uri(Project.Setting.PreviewRootUrl + s);
			Uri fromUri = new Uri(Project.Setting.PreviewRootUrl + this.Path);

			if(targetUri.AbsoluteUri == fromUri.AbsoluteUri) return "";

			Uri resultUri = fromUri.MakeRelativeUri(targetUri);
			string result = resultUri.ToString();
			if(!string.IsNullOrEmpty(Project.Setting.IndexLinkSuffix)){
				result = Util.CutRight(result, Project.Setting.IndexLinkSuffix);
			}
			// �����܂ŗ��Č��ʂ���̏ꍇ�A"./" �̉��߂Ɏ��s���Ă���
			if(result == "") return "./";
			return result;
		}
		public string RelUrl(string s){
			return RelUri(s);
		}

		public string ReadContent(){
			if(myData == null) LoadData();
			return myData;
		}

		public void LoadData(){
			if(!this.File.Exists){
				throw new Exception("�t�@�C�������[�h���悤�Ƃ��܂������A�t�@�C����������܂��� : " + this.File.FullName);
			}
			myData = Util.LoadFile(this.File.FullName, myProject.Setting.HtmlEncodingObj);
		}

		// �t�@�C���ɃR���e���c���������݂܂��B
		public void WriteContent(string s){
			if(!this.File.Exists){
				throw new Exception("�t�@�C���ɏ������݂��悤�Ƃ��܂������A�t�@�C����������܂��� : " + this.File.FullName);
			}
			Util.WriteFile(this.FilePath, s, myProject.Setting.HtmlEncodingObj);
		}

		// �^����ꂽ�p�X�ɑ�������t�@�C���� (�T�[�o���̃t���p�X��) ���擾���܂��B
		// URL �̏ꍇ�̓X�L�[���ƃh���C�����������폜���ď������܂��B
		public string PathToFilename(string path){
			if(path == null) path = "";

			string result = myProject.Setting.DocumentFullPath.FullName.TrimEnd('/') + '/' + path.TrimStart('/');
			return result;
		}

	}
}

