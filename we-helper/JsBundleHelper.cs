using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace we_helper {
	class JsBundleHelper : IFileHelper {
		public Helper Helper { get; set; }

		public bool IsHelperFor(string file) {
			return file.EndsWith(".js.bundle", StringComparison.OrdinalIgnoreCase);
		}
		public string[] FindDepenencies(string content) {
			var xdoc = XDocument.Parse(content);
			return xdoc.Descendants("file").Select(x => x.Value.Trim()).ToArray();
		}
		public async Task<IEnumerable<Tuple<string, string>>> TransformAsync(string file, string content) {
			var baseFile = file.Substring(0, file.Length - ".js.bundle".Length);
			var outputFile = baseFile + ".js";
			using (var writer = new System.IO.StringWriter()) {
				foreach (var dep in FindDepenencies(content)) {
					var inputFile = dep.ToUri(Helper.BaseUri).LocalPath;
					var inputContent = await Utilities.ReadAllTextAsync(inputFile);
					await writer.WriteLineAsync("///#source 1 1 " + dep + Environment.NewLine + inputContent);
				}

				var result = writer.ToString();
				await Utilities.WriteAllTextAsync(outputFile, result);
				JsHelper.MinifyJsWithSourceMap(outputFile, result);

				return new Tuple<string, string>[0];
			}
		}
	}
}
