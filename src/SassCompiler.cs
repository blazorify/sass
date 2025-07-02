using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Blazorify.Sass {
	public class SassCompiler {
		private readonly ILogger<SassCompiler> logger;

		private readonly String sassPath;

		public SassCompiler(
			ILogger<SassCompiler> logger
		) {
			this.logger = logger;

			var runtimeID = OperatingSystem.IsWindows() ? "win-x64"
			  : OperatingSystem.IsLinux() ? "linux-x64"
			  : OperatingSystem.IsMacOS() ? "osx-x64"
			  : throw new NotSupportedException();

			this.sassPath = Path.Combine(
				AppContext.BaseDirectory,
				"runtimes",
				runtimeID,
				"native",
				"dart-sass",
				RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "sass.bat" : "sass"
			);

			if (!File.Exists(this.sassPath)) {
				this.logger.LogError("Could not find Dart Sass executable at: {sassPath}", this.sassPath);
			}
		}

		public String Compile(String scss) {
			return this.Compile(scss, new SassCompilerOptions());
		}

		public String Compile(String scss, SassCompilerOptions options) {
			var inputPath = $"{Path.GetTempFileName()}.scss";
			var outputPath = $"{Path.GetTempFileName()}.css";

			this.logger.LogDebug("[Compile] Write {size} to {tempScss}", scss.Length, inputPath);

			File.WriteAllText(inputPath, scss);

			this.CompileFile(inputPath, outputPath, options);

			var result = File.ReadAllText(outputPath);

			File.Delete(inputPath);
			File.Delete(outputPath);

			return result;
		}

		public void CompileFile(String inputPath, String outputPath, SassCompilerOptions options) {
			var args = new List<String>() {
				$"\"{inputPath}\" \"{outputPath}\""
			};

			foreach (var includePath in options.IncludePaths) {
				args.Add($"--load-path=\"{includePath}\"");
			}

			if (options.Style == "compressed") {
				args.Add("--style=compressed");
			}

			if (!options.SourceMap) {
				args.Add("--no-source-map");
			}

			if (options.Quiet) {
				args.Add("--quiet");
			}

			this.logger.LogDebug("[Compile] {sassPath} {sassArgs}", this.sassPath, String.Join(" ", args));

			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = this.sassPath,
					Arguments = String.Join(" ", args),
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			process.Start();
			var stderr = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (process.ExitCode != 0) {
				throw new Exception($"SCSS compilation failed:\n{stderr}");
			}
		}
	}
}
