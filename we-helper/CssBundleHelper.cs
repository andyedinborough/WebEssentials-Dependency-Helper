using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace we_helper {
	class CssBundleHelper : IFileHelper {
		public static Func<string , string, string> MinifyCss { get; set; }
		public Helper Helper { get; set; }

		public bool IsHelperFor(string file) {
			return file.EndsWith(".css.bundle", StringComparison.OrdinalIgnoreCase);
		}
		public string[] FindDepenencies(string content) {
			var xdoc = XDocument.Parse(content);
			return xdoc.Descendants("file").Select(x => x.Value.Trim()).ToArray();
		}
		public async Task<IEnumerable<Tuple<string, string>>> TransformAsync(string file, string content) {
			string css;
			using (var writer = new System.IO.StringWriter()) {
				foreach (var dep in FindDepenencies(content)) {
					var inputFile = dep.ToUri(Helper.BaseUri).LocalPath;
					var inputContent = await Utilities.ReadAllTextAsync(inputFile);
					await writer.WriteLineAsync(inputContent);
				}
				css = writer.ToString();
			}

			var baseFile = file.Substring(0, file.Length - ".css.bundle".Length);
			return new[]{
				Tuple.Create(baseFile + ".css", css),
				Tuple.Create(baseFile + ".min.css", MinifyCss(file, css)),
			};
		}
	}

}
