using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace we_helper {
	class Program {

		static void Main(string[] args) {
			var dir = args.FirstOrDefault(x => !x.StartsWith("/") && !x.StartsWith("-") && System.IO.Directory.Exists(x))
				.NotEmpty(Environment.CurrentDirectory);
			var options = new HashSet<string>(args.Where(x => x != dir).Select(x => x.TrimStart('/', '-').ToLower()).Distinct());
			var buildAll = options.Contains("build");

			var helper = new Helper(dir);
			HashSet<string> files_in_project = null;
			Action reindex_project_files = () => {
				files_in_project =
					helper.ProjectFiles =
						new HashSet<string>(FindProjectFiles(dir, false).SelectMany(x => GetFilesInProject(x)), StringComparer.OrdinalIgnoreCase);
				helper.IndexFiles();
			};
			reindex_project_files();
			helper.OnChanged = file => Console.WriteLine(DateTime.Now.TimeOfDay + " - " + file);

			var minifier1 = new Microsoft.Ajax.Utilities.Minifier();
			CssBundleHelper.MinifyCss = (file, css) => {
				var cssParser = new Microsoft.Ajax.Utilities.CssParser();
				cssParser.FileContext = file;
				cssParser.Settings = new Microsoft.Ajax.Utilities.CssSettings {
					CommentMode = Microsoft.Ajax.Utilities.CssComment.None
				};
				return cssParser.Parse(css);
			};

			var vsExtDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\VisualStudio\11.0\Extensions");
			var lessc_wsf = Utilities.GetFiles(vsExtDir, file => file.Split('\\').Last().ToLower() == "lessc.wsf").FirstOrDefault();
			if (string.IsNullOrEmpty(lessc_wsf)) {
				Console.Error.WriteLine("lessc.wsf could not be found under {0}", vsExtDir);
				return;
			}

			JsHelper.MinifyJs = minifier1.MinifyJavaScript;
			JsHelper.MinifyJsWithSourceMap = (file, js) => {
				var baseFile = file.Substring(0, file.Length - ".js".Length);
				var min_file = baseFile + ".min.js";
				var map_file = min_file + ".map";
				var min_filename = Path.GetFileName(min_file);
				var map_filename = Path.GetFileName(map_file);

				using (var min_writer = new System.IO.StreamWriter(min_file, false, new System.Text.UTF8Encoding(true)))
				using (var map_writer = new System.IO.StreamWriter(map_file, false, new System.Text.UTF8Encoding(false)))
				using (var v3SourceMap = new Microsoft.Ajax.Utilities.V3SourceMap(map_writer)) {
					v3SourceMap.StartPackage(min_file, map_file);
					var jsParser = new Microsoft.Ajax.Utilities.JSParser(js);
					jsParser.FileContext = file;
					var block = jsParser.Parse(new Microsoft.Ajax.Utilities.CodeSettings {
						SymbolsMap = v3SourceMap, PreserveImportantComments = false, TermSemicolons = true
					});
					min_writer.Write(block.ToCode() + Environment.NewLine + "//@ sourceMappingURL=" + map_filename);
					v3SourceMap.EndPackage();
				}
			};

			LessHelper.CompileLess = (fileName, less) => {
				var tempFileName = System.IO.Path.GetTempFileName();
				var processStartInfo = new System.Diagnostics.ProcessStartInfo("cscript",
				 "/nologo /s \"" + lessc_wsf + "\" \"" + fileName + "\" \"" + tempFileName + "\"") {
					 CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
					 UseShellExecute = false
				 };

				using (var proc = new System.Diagnostics.Process {
					StartInfo = processStartInfo
				}) {
					proc.Start();
					proc.WaitForExit();
					var content = System.IO.File.ReadAllText(tempFileName);
					System.IO.File.Delete(tempFileName);
					return content;
				}
			};

			var mre = new System.Threading.ManualResetEventSlim(false);
			using (var fileSystem = new System.IO.FileSystemWatcher(dir) {
				EnableRaisingEvents = true, IncludeSubdirectories = true
			}) {
				fileSystem.Changed += (s, e) => {
					if (rx_project_file.IsMatch(e.FullPath)) {
						reindex_project_files();
						return;
					}
					if (!files_in_project.Contains(e.FullPath)) return;
					mre.Wait(500);
					if (!System.IO.File.Exists(e.FullPath)) return;
					helper.FileChangedAsync(e.FullPath).Wait();
				};
				fileSystem.Deleted += (s, e) => {
					if (!files_in_project.Contains(e.FullPath)) return;
					mre.Wait(500);
					helper.FileDeleted(e.FullPath);
				};
				fileSystem.Renamed += (s, e) => {
					if (!files_in_project.Contains(e.FullPath)) return;
					mre.Wait(500);
					helper.FileRenamed(e.OldFullPath, e.FullPath);
				};

				((Action)(async () => {
					if (buildAll) {
						var alldependencies = helper.Files.SelectMany(x => x.Dependencies).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
						await System.Threading.Tasks.Task.WhenAll(alldependencies.Select(x => helper.FileChangedAsync(x)).ToArray());
					}
					Console.Beep();
					Console.WriteLine("Build Complete");
				}))();

				while (true) {
					mre.Wait(500);
				}
			}
		}

		static Regex rx_project_file = new Regex(@"\.(cs|vb)proj$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		public static IEnumerable<string> FindProjectFiles(string dir, bool recurse = false) {
			foreach (var file in System.IO.Directory.GetFiles(dir))
				if (rx_project_file.IsMatch(file))
					yield return file;

			if (recurse)
				foreach (var sub_dir in System.IO.Directory.GetDirectories(dir))
					foreach (var item in FindProjectFiles(sub_dir))
						yield return item;
		}

		public static string[] GetFilesInProject(string project) {
			var project_uri = System.IO.Path.GetDirectoryName(project).ToUri();
			var xdoc = XDocument.Load(project);
			return xdoc.Descendants().Where(x => x.Attribute("Include") != null)
				.Select(x => (string)x.Attribute("Include"))
				.Select(x => x.ToUri(project_uri).LocalPath)
				.ToArray();
		}
	}
}
