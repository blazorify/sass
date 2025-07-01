using System;
using Blazorify.Sass;

namespace Microsoft.Extensions.DependencyInjection {
	public static class AddBlazorifySassExtension {
		public static IServiceCollection AddBlazorifySass(this IServiceCollection services) {
			return services.AddBlazorifySass((configure) => {
				// Do nothing
			});
		}

		public static IServiceCollection AddBlazorifySass(this IServiceCollection services, Action<SassBuildManager> configure) {
			var manager = new SassBuildManager();
			configure.Invoke(manager);

			services.AddSingleton<SassCompiler>();
			services.AddSingleton<SassWatcher>();
			services.AddSingleton(manager);

			return services;
		}
	}
}
