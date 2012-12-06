using System;
using System.Linq;

namespace we_helper {
	class Program {

		static void Main(string[] args) {
			var dir = args.FirstOrDefault(x => !x.StartsWith("/") && !x.StartsWith("-")).NotEmpty(Environment.CurrentDirectory);
			var buildAll = args.Any(x => x.ToLower().TrimStart('/', '-') == "build");

			var helper = new Helper(dir);
			helper.IndexFiles();
			helper.OnChanged = file => {
				Console.WriteLine(file);
			};

			var minifier1 = new Microsoft.Ajax.Utilities.Minifier();
			CssBundleHelper.MinifyCss = css => {
				return minifier1.MinifyStyleSheet(css);
			};

			var vsExtDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\VisualStudio\11.0\Extensions");
			var lessc_wsf = Utilities.GetFiles(vsExtDir, file => file.Split('\\').Last().ToLower() == "lessc.wsf").FirstOrDefault();
			if (string.IsNullOrEmpty(lessc_wsf)) {
				Console.Error.WriteLine("lessc.wsf could not be found under {0}", vsExtDir);
				return;
			}

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
					mre.Wait(500);
					if (!System.IO.File.Exists(e.FullPath)) return;
					helper.FileChanged(e.FullPath);
				};
				fileSystem.Deleted += (s, e) => {
					mre.Wait(500);
					helper.FileDeleted(e.FullPath);
				};
				fileSystem.Renamed += (s, e) => {
					mre.Wait(500);
					helper.FileRenamed(e.OldFullPath, e.FullPath);
				};

				Utilities.Async(() => {
					if (buildAll) {
						var alldependencies = helper.Files.SelectMany(x => x.Dependencies).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
						foreach (var dep in alldependencies)
							helper.FileChanged(dep);
					}
				});

				while (true) {
					mre.Wait(500);
				}
			}
		}
	}
}
