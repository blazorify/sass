using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Blazorify.Sass {
	public class SassWatcher : IDisposable {
		private readonly SassCompiler compiler;
		private readonly ILogger<SassWatcher> logger;

		private FileSystemWatcher? watcher;
		private Timer? debounceTimer;
		private String? watchedEntry;
		private String? watchedOutput;
		private SassCompilerOptions? watchedOptions;

		public SassWatcher(
			SassCompiler compiler,
			ILogger<SassWatcher> logger
		) {
			this.compiler = compiler;
			this.logger = logger;
		}

		public void Watch(String entryFile, String outputFile, SassCompilerOptions options) {
			this.watchedEntry = Path.GetFullPath(entryFile);
			this.watchedOutput = Path.GetFullPath(outputFile);
			this.watchedOptions = options;

			var directory = Path.GetDirectoryName(this.watchedEntry)!;

			this.logger.LogInformation("[Watch] Watching: {Entry}", this.watchedEntry);

			this.watcher = new FileSystemWatcher {
				Path = directory,
				Filter = "*.scss",
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
			};

			this.watcher.Changed += this.OnChanged;
			this.watcher.Created += this.OnChanged;
			this.watcher.Deleted += this.OnChanged;
			this.watcher.Renamed += this.OnChanged;

			this.watcher.EnableRaisingEvents = true;
		}
		private void OnChanged(Object sender, FileSystemEventArgs e) {
			if (this.watchedEntry == null || this.watchedOutput == null || this.watchedOptions == null) {
				return;
			}

			// Debounce 300ms
			this.debounceTimer?.Dispose();
			this.debounceTimer = new Timer(_ => {
				try {
					this.compiler.CompileFile(this.watchedEntry, this.watchedOutput, this.watchedOptions);
					this.logger.LogInformation("[OnChanged] Recompiled: {Output}", this.watchedOutput);
				} catch (Exception ex) {
					this.logger.LogError(ex, "[OnChanged] Compilation failed");
				}
			}, null, 300, Timeout.Infinite);
		}

		public void Dispose() {
			this.watcher?.Dispose();
			this.debounceTimer?.Dispose();
		}
	}
}
