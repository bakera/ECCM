using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Bakera.Eccm{

	public class ColorPattern{
	
		public Regex Regex{get;set;}
		public String ColumnName{get;set;}
	
		public bool IsMatch(EcmItem item){
			string s = item[this.ColumnName];
			if(s == null) s = "";
			return this.Regex.IsMatch(s);
		}

		public string MatchValue(EcmItem item){
			if(item == null) return null;
			string s = item[this.ColumnName];
			if(s == null) s = "";
			Match m = this.Regex.Match(s);
			if(m.Groups.Count > 1){
				return m.Groups[1].Value;
			}
			return null;
		}


		public static ColorPattern Parse(string ruleStr){
			return Parse(ruleStr, null);
		}

		public static ColorPattern Parse(string ruleStr, string defaultColumnName){
			if(string.IsNullOrEmpty(ruleStr)) return null;
			ColorPattern result = new ColorPattern();

			string[] colorSeparateRules = ruleStr.Split('=');
			if(colorSeparateRules.Length > 1){
				result.ColumnName = colorSeparateRules[0];
				result.Regex = new Regex(colorSeparateRules[1]);
				return result;
			}

			if(string.IsNullOrEmpty(defaultColumnName)) return null;
			result.ColumnName = defaultColumnName;
			result.Regex = new Regex(colorSeparateRules[0]);
			return result;
		}
	
	}

}
