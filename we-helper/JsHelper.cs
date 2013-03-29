using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace we_helper {
	class JsHelper : IFileHelper {
		public static Func<string, string> MinifyJs { get; set; }
		public static Action<string, string> MinifyJsWithSourceMap { get; set; }
		public Helper Helper { get; set; }

		public bool IsHelperFor(string file) {
			return file.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase);
		}

		public string[] FindDepenencies(string code) { return new string[0]; }

		public async Task<IEnumerable<Tuple<string, string>>> TransformAsync(string file, string content) {
			var basefile = file.Substring(0, file.Length - ".js".Length);
			var minFile = basefile + ".min.js";
			var mapFile = minFile + ".map";

			return await Task.Run(() => {
				if (System.IO.File.Exists(mapFile)) {
					MinifyJsWithSourceMap(file, content);
				} else if (System.IO.File.Exists(minFile)) {
					return new[] {
						Tuple.Create(minFile, MinifyJs(content))
					};
				}
				return new Tuple<string, string>[0];
			});
		}
	}
}
