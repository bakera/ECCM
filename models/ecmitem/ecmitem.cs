using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;


namespace Bakera.Eccm{
	public class EcmItem : EcmFileBase{
		private static TimeSpan OneMinute = TimeSpan.FromMinutes(1);
		private static TimeSpan OneHour = TimeSpan.FromHours(1);
		private static TimeSpan OneDay = TimeSpan.FromDays(1);

		private DataRow myRow = null;

		// �\��
		public const string ReservedMark = "#";
		public const string ReservedIdName = "id";
		public const string ReservedTitleName = "title";
		public const string ReservedPathName = "path";
		public const string ReservedTemplateName = "template";
		public const string ReservedParentName = "parent";
		public const string ReservedSchemaName = "schema";

		public const string NavStayTemplate = "<li class=\"stay\"><em>{0}</em></li>";
		public const string NavLinkTemplate = "<li><a href=\"{0}\">{1}</a></li>";
		public const string AncestorNavLinkTemplate = "<li class=\"stay\"><a href=\"{0}\">{1}</a></li>";
		public const string NavDisabledTemplate = "<li class=\"disabled\"><span>{0}</span></li>";

// �R���X�g���N�^

		// ID �� DataRow ���� EcmItem ���쐬���܂��B
		public EcmItem(string id, DataRow row) : base(row.Table as EcmProject){
			myId = id;
			myRow = row;
			myPath = Project.GetPathByRow(row);
		}

		// DataRow �݂̂��� EcmItem ���쐬���܂��B
		public EcmItem(DataRow row) : base(row.Table as EcmProject){
			myRow = row;
			Id = Project.GetIdByRow(row);
			myPath = Project.GetPathByRow(row);
		}

// ��r���Z�q�̃I�[�o�[���C�h
		public override bool Equals(Object o){
			EcmItem targetItem = o as EcmItem;
			if(targetItem == null) return false;
			return FqId.Equals(targetItem.FqId);
		}

		public override int GetHashCode(){
			return FqId.GetHashCode();
		}

		public static bool operator ==(EcmItem a, EcmItem b){
			if (Object.ReferenceEquals(a, b)) return true;
			if (((Object)a == null) || ((Object)b == null)) return false;
			return a.Equals(b);
		}

		public static bool operator !=(EcmItem a, EcmItem b){
			return !(a == b);
		}

// �C���f�N�T
		// �񖼂���f�[�^���擾���܂��B
		public string this[string colName]{
			get{ return GetData(colName); }
		}


// �v���p�e�B

		// ���S�C�����ꂽ ID ���擾���܂��B
		public override string FqId{
			get{return Project.Id + '/' + myId;}
		}

		// �t�@�C���̍ŏI�A�N�Z�X����������������擾���܂��B�t�@�C�����Ȃ��Ƃ��� "-" ��Ԃ��܂��B
		public string FileTime{
			get{
				if(this.File == null) return "-";
				if(!this.File.Exists) return "-";
				DateTime t = this.File.LastWriteTime;
				TimeSpan ts = Project.Setting.StartTime - t;
				if(ts < OneMinute) return string.Format("{0}s", ts.Seconds);
				if(ts < OneHour) return string.Format("{0}m", ts.Minutes);
				if(ts < OneDay) return string.Format("{0}h", ts.Hours);
				return  string.Format("{0}d", ts.Days);
			}
		}

		// ���� Item �̌��ƂȂ��� DataRow �ɒ��ڃA�N�Z�X���܂��B
		public DataRow DataRow{
			get{return myRow;}
		}

		// ���� Item �� DataRow �̃C���f�N�X���擾���܂��B
		public int RowIndex{
			get{return myRow[0].ToInt32();}
		}

		// �p�X���擾���܂��B
		public override string Path{
			get{return myPath;}
		}

		// �^�C�g�����擾���܂��B
		public string Title{
			get{ return GetReservedData(ReservedTitleName);}
		}


		// ���� EcmItem �ɑΉ�����v���r���[�摜�t�@�C�������� FileInfo �̔z����擾���܂��B
		public FileInfo[] PreviewFiles{
			get{
				if(string.IsNullOrEmpty(Project.Setting.ImageDir)) return null;
				DirectoryInfo previewImageDir = Project.Setting.ImageFullPath;
				if(!previewImageDir.Exists) return null;
				FileInfo[] files = previewImageDir.GetFiles(this.Id + "*.*");
				Array.Sort(files, CompareFileInfoByName);
				return files;
			}
		}


		// ���� EcmItem �ɑΉ�����O���[�o���e���v���[�g�̖��̂��擾���܂��B
		public string Template{
			get{
				string temp = GetReservedData(ReservedTemplateName);
				if(!string.IsNullOrEmpty(temp)) return temp;
				temp = Project.Setting.DefaultGrobalTemplate;
				if(!string.IsNullOrEmpty(temp)) return temp;
				return null;
			}
		}


		// ���� EcmItem �ɑΉ�����X�L�[�}�̖��̂��擾���܂��B
		public string SchemaName{
			get{
				string sch = GetReservedData(ReservedSchemaName);
				if(!string.IsNullOrEmpty(sch)) return sch;
				sch = Project.Setting.DefaultSchemaName;
				if(!string.IsNullOrEmpty(sch)) return sch;
				return null;
			}
		}

		// ���� EcmItem �ɑΉ�����X�L�[�}�̃t�@�C�����擾���܂��B
		public FileInfo SchemaFile{
			get{
				if(string.IsNullOrEmpty(SchemaName)) return null;
				DirectoryInfo schemaDir = Project.Setting.SchemaDirInfo;
				if(!schemaDir.Exists) return null;
				FileInfo[] files = schemaDir.GetFiles(SchemaName + ".xsd");
				if(files.Length > 0) return files[0];
				return null;
			}
		}


		// ���̃v���W�F�N�g�ɑ����邷�ׂĂ� EcmItem ���擾���܂��B
		public EcmItem[] AllItem{
			get{
				return Project.GetAllItems();
			}
		}


		// ���̃v���W�F�N�g�ɑ�����A���g�������� EcmItem ���擾���܂��B
		public EcmItem[] AllOtherItem{
			get{
				List<EcmItem> otheritems = new List<EcmItem>();
				EcmItem[] items = Project.GetAllItems();
				for(int i = 0; i < items.Length; i++){
					if(items[i].FqId == this.FqId) continue;
					otheritems.Add(items[i]);
				}
				return otheritems.ToArray();
			}
		}


		// �e�� EcmItem ���擾���܂��B
		public EcmItem ParentItem{
			get{
				if(string.IsNullOrEmpty(this.Parent)) return null;
				EcmItem result = Project.GetItem(this.Parent);
				if(result == null || result.FqId == this.FqId) return null;
				return result;
			}
		}


		// ���x�����擾���܂��B
		// �e�����l���邩�擾���܂��B�g�b�v�̓��x��0�ł��B
		public int Level{
			get{
				int level = 0;
				EcmItem p = ParentItem;
				for(;;){
					if(p == null) return level;
					p = p.ParentItem;
					level++;
					if(level > Project.Setting.DepthMax) throw new Exception("���x�����擾�ł��܂���B�������[�v�����o���܂����B�e�̐ݒ肪�z���Ă���\��������܂��B");
				}
			}
		}

		// �e��ID���擾���܂��B
		public string Parent{
			get{return GetReservedData(ReservedParentName);}
		}

		// �q�� EcmItem �̔z����擾���܂��B
		public EcmItem[] ChildItems{
			get{
				EcmItem[] result = Project.GetItemsByValue(ReservedParentName, Id);
				if(result != null && result.Length > 0) return result;
				result =  Project.GetItemsByValue(ReservedMark + ReservedParentName, Id);
				if(result != null) return result;
				return new EcmItem[0];
			}
		}

		// �Z��� EcmItem �̔z����擾���܂��B���g���܂܂�܂��B
		// �e�������Ƃ��͋�̔z�񂪕Ԃ�܂��B
		public EcmItem[] SiblingItems{
			get{
				if(this.Parent == null) return new EcmItem[0];
				return this.ParentItem.ChildItems;
			}
		}

		// ��c�� EcmItem �̔z����擾���܂��B���g�͊܂܂�܂���B
		// �e�������Ƃ��͋�̔z�񂪕Ԃ�܂��B
		// �z�Q�Ƃ����o�����ꍇ�A�����ŏ�����ł��؂�܂��B
		public EcmItem[] AncestorItems{
			get{
				if(this.Parent == null) return new EcmItem[0];
				// �z�Q�ƃ`�F�b�N�p
				List<EcmItem> al = new List<EcmItem>();

				EcmItem item = this.ParentItem;
				for(;;){
					if(item == null) break;
					// �z�Q�ƃ`�F�b�N
					foreach(EcmItem i in al){
						if(i.FqId == item.FqId){
							goto AncestorRoopEnd;
						}
					}
					al.Add(item);
					item = item.ParentItem;
				}
				AncestorRoopEnd:
				al.Reverse();
				return al.ToArray();
			}
		}

		public bool IsAncestorOf(EcmItem target){
			if(target == null) return false;
			if(target.Parent == null) return false;

			// �z�Q�ƃ`�F�b�N�p
			List<EcmItem> al = new List<EcmItem>();

			EcmItem item = target.ParentItem;
			for(;;){
				if(item == null) return false;
				if(item == this) return true;
				// �z�Q�ƃ`�F�b�N
				foreach(EcmItem i in al){
					if(i.FqId == item.FqId){
						return false;
					}
				}
				al.Add(item);
				item = item.ParentItem;
			}
		}

		// ParsePermissionRule �ɓK�����Ă���� true ��Ԃ��܂��B
		// ParsePermissionRule �� ���O=�l �̕�����ł��B
		// ParsePermissionRule ���w�肳��Ă��Ȃ��Ƃ��A�s���Ȓl�̂Ƃ��� true ���Ԃ�܂��B
		public bool ParsePermit{
			get{
				if(Project.Setting.ParsePermissonRule == null) return true;
				string[] splitted = Project.Setting.ParsePermissonRule.Split(new char[]{'='});
				if(splitted.Length < 2) return true;
				// ���肷��
				string colName = splitted[0].Trim();
				string colValue = splitted[1].Trim();
				if(this[colName] == null) return false;
				if(this[colName].Trim() == colValue) return true;
				return false;
			}
		}



// public ���\�b�h

		// �C�ӂ̖��O�̃f�[�^���擾���܂��B
		public string GetData(string colName){
			if(Project.Columns.IndexOf(colName) >= 0) return myRow[colName] as string;
			return null;
		}

		// �C�ӂ̖��O�̗\�񖼃f�[�^���擾���܂��B
		public string GetReservedData(string colName){
			string result = GetData(ReservedMark + colName);
			if(result != null) return result;
			return GetData(colName);
		}

		// ��Ɏw�肳�ꂽ ID �� EcmItem ���擾���܂��B
		public EcmItem GetEcmItem(string colName){
			string idName = GetData(colName);
			if(string.IsNullOrEmpty(idName)) return null;
			EcmItem result = Project.GetItem(idName);
			return result;
		}


		// �����N�e�L�X�g��href�����̒l���w�肵�ăA���J�[���擾���܂��B
		public string GetAnchorByTextAndHref(string innerText, string href){
			string result = null;
			if(string.IsNullOrEmpty(href)){
				result = string.Format(Project.Setting.StayTemplate, innerText);
			} else {
				result = string.Format(Project.Setting.AnchorTemplate, href, innerText);
			}
			return result;
		}

		// �����N�e�L�X�g���w�肵�ăA���J�[���擾���܂��B
		public string GetAnchorByText(string innerText){
			return GetAnchorByTextAndHref(innerText, this.RelUri());
		}

		// �����N�e�L�X�g�ƂȂ�p�����[�^�̗񖼂��w�肵�ăA���J�[���擾���܂��B
		public string GetAnchorByName(string colName){
			return GetAnchorByText(this[colName]);
		}

		// title�������N�e�L�X�g�Ƃ���A���J�[���擾���܂��B
		public string GetAnchor(){
			return GetAnchorByName(ReservedTitleName);
		}

		// �����N�e�L�X�g���w�肵�Đ�΃����N�A���J�[���擾���܂��B
		public string GetAbsUrlAnchorByText(string innerText){
			string href = string.Format("http://{0}{1}", Project.Setting.AbsUrlDomain, this.Path);
			return GetAnchorByTextAndHref(innerText, href);
		}

		// �����N�e�L�X�g�ƂȂ�p�����[�^�̗񖼂��w�肵�Đ��URL�A���J�[���擾���܂��B
		public string GetAbsUrlAnchorByName(string colName){
			return GetAbsUrlAnchorByText(this[colName]);
		}

		// ���URL�Ƀ����N����A���J�[���擾���܂��B
		public string GetAbsUrlAnchor(){
			return GetAbsUrlAnchorByName(ReservedTitleName);
		}




		// �����N�e�L�X�g���w�肵�āA�i�r�p�����N��li�v�f���擾���܂��B
		public string GetNavLinkByText(string innerText){
			string relUri = this.RelUri();
			string result = null;
			if(!this.Exists){
				result = string.Format(NavDisabledTemplate, innerText);
			} else if(string.IsNullOrEmpty(relUri)){
				result = string.Format(NavStayTemplate, innerText);
			} else {
				// ��c�����N�p�e���v���[�g������ꍇ�A�����N�悪��c�����肷��
				if(!string.IsNullOrEmpty(AncestorNavLinkTemplate) && this.IsAncestorOf(Project.CurrentItem)){
					result = string.Format(AncestorNavLinkTemplate, relUri, innerText);
				} else {
					result = string.Format(NavLinkTemplate, relUri, innerText);
				}
			}
			return result;

		}

		// �����N�e�L�X�g�ƂȂ�p�����[�^�̗񖼂��w�肵�āA�i�r�p�����N��li�v�f���擾���܂��B
		public string GetNavLinkByName(string colName){
			return GetNavLinkByText(this[colName]);
		}

		// title�������N�e�L�X�g�Ƃ���i�r�p�����N��li�v�f���擾���܂��B
		public string GetNavLink(){
			return GetNavLinkByName(ReservedTitleName);
		}

		// ID��Ԃ��܂��B
		public override string ToString(){
			return Id;
		}

		// Format ���\�b�h
		public new string Format(string s){
			string[] para = s.Split(',');
			object[] datas = new object[para.Length+1];
			datas[0] = RowIndex;
			for(int i=1; i< para.Length; i++){
				// ���l�Ƃ��ĉ��߂ł���f�[�^�͐��l�Ƃ݂Ȃ�
				string str = this[para[i]];
				int num = 0;
				if(Int32.TryParse(str, out num)){
					datas[i] = num;
				} else {
					datas[i] = str;
				}
			}
			return String.Format(para[0], datas);
		}

		// �^����ꂽ�p�X�ɑ�������t�@�C�����擾���܂��B
		public EcmTextFile GetFile(string path){
			string relPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path), path);
			return new EcmTextFile(relPath, Project);
		}

		// �^����ꂽ�p�X�ɑ�������摜���擾���܂��B
		public EcmImageFile GetImage(string path){
			string relPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path), path);
			return new EcmImageFile(relPath, Project);
		}

		// �^����ꂽ�p�X�ɑ�������t�@�C�����擾���A�C���N���[�h���܂��B
		public string Include(string path){
			EcmTextFile file = GetFile(path);
			return file.ReadContent();
		}

		// �^����ꂽ�p�X�ɑ�������摜�t�@�C�����擾���Aimg�v�f�𐶐����܂��B
		public string ImgElement(string path){
			EcmImageFile img = GetImage(path);
			return img.ImgElement();
		}

	}
}

