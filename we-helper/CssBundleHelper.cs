using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace we_helper {
	class CssBundleHelper : IFileHelper {
		public static Func<string, string> MinifyCss { get; set; }
		public Helper Helper { get; set; }

		public bool IsHelperFor(string file) {
			return file.EndsWith(".css.bundle", StringComparison.OrdinalIgnoreCase);
		}
		public string[] FindDepenencies(string content) {
			var xdoc = XDocument.Parse(content);
			return xdoc.Descendants("file").Select(x => x.Value.Trim()).ToArray();
		}
		public IEnumerable<Tuple<string, string>> Transform(string file, string content) {
			var css = FindDepenencies(content)
				.Select(x => "/*#source " + x + " */" + Environment.NewLine + System.IO.File.ReadAllText(x.ToUri(Helper.BaseUri).LocalPath))
				.Join(Environment.NewLine);
			var baseFile = file.Substring(0, file.Length - ".css.bundle".Length);
			return new[]{
				Tuple.Create(baseFile + ".css", css),
				Tuple.Create(baseFile + ".min.css", MinifyCss(css)),
			};
		}
	}

}
