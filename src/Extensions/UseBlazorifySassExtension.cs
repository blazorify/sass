using Blazorify.Sass;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder {
	public static class UseBlazorifySassExtension {
		public static IApplicationBuilder UseBlazorifySass(this IApplicationBuilder app) {
			var manager = app.ApplicationServices.GetRequiredService<SassBuildManager>();
			var compiler = app.ApplicationServices.GetRequiredService<SassCompiler>();
			var watcher = app.ApplicationServices.GetRequiredService<SassWatcher>();

			foreach (var file in manager.RegisteredFiles) {
				compiler.CompileFile(file.InputPath, file.OutputPath, file.Options);

				if (file.Watch) {
					watcher.Watch(file.InputPath, file.OutputPath, file.Options);
				}
			}

			return app;
		}
	}
}
