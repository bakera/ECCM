using System;
using System.Drawing;
using System.IO;
using System.Text;


namespace Bakera.Eccm{
	public class EcmImageFile : EcmFileBase{

		private const string ImgElementTemplate = "<img src=\"{0}\" width=\"{1}\" height=\"{2}\" alt=\"{3}\" />";
		private Bitmap myImage = null;

// コンストラクタ

		public EcmImageFile(string path, EcmProject project) : base(path, project){
			LoadImage();
		}

// プロパティ

		public int Width{
			get{
				return myImage.Width;
			}
		}

		public int Height{
			get{
				return myImage.Height;
			}
		}


// メソッド


		public void LoadImage(){
			if(this.File.Exists){
				using(FileStream fs = this.File.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
					myImage = new Bitmap(fs);
				}
			}
		}

		public string ImgElement(){
			return ImgElement(null);
		}


		public string ImgElement(string alt){
			if(!Exists) return string.Format("<!-- no image: {0} -->", Path);
			return string.Format(ImgElementTemplate, RelUri(), Width, Height, alt);
		}


		public override string ToString(){
			return ImgElement();
		}




	}
}

