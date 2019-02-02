using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Toolchains.MsBuildSdkExtras
{
    /// <summary>
    /// A tool chain which uses the MSBuild.Sdk.Extras SDK to allow for greater frameworks including Xamarin.
    /// </summary>
    public class MsBuildSdkExtrasToolchain : Toolchain
    {
        public static readonly IToolchain XamarinIOS10 = From(new NetCoreAppSettings("Xamarin.iOS10", null, "Xamarin iOS 1.0"));
        public static readonly IToolchain XamarinMac20 = From(new NetCoreAppSettings("Xamarin.Mac20", null, "Xamarin Mac 2.0"));
        public static readonly IToolchain XamarinTVOS10 = From(new NetCoreAppSettings("Xamarin.TVOS10", null, "Xamarin TV OS 1.0"));
        public static readonly IToolchain XamarinWatchOS10 = From(new NetCoreAppSettings("Xamarin.WatchOS10", null, "Xamarin Watch OS 1.0"));
        public static readonly IToolchain XamarinMonoAndroid10 = From(new NetCoreAppSettings("MonoAndroid10", null, "Xamarin Mono Android 1.0"));
        public static readonly IToolchain XamarinMonoAndroid23 = From(new NetCoreAppSettings("MonoAndroid23", null, "Xamarin Mono Android 2.3"));
        public static readonly IToolchain XamarinMonoAndroid403 = From(new NetCoreAppSettings("MonoAndroid403", null, "Xamarin Mono Android 4.0.3"));
        public static readonly IToolchain XamarinMonoAndroid41 = From(new NetCoreAppSettings("MonoAndroid41", null, "Xamarin Mono Android 4.1"));
        public static readonly IToolchain XamarinMonoAndroid42 = From(new NetCoreAppSettings("MonoAndroid42", null, "Xamarin Mono Android 4.2"));
        public static readonly IToolchain XamarinMonoAndroid43 = From(new NetCoreAppSettings("MonoAndroid43", null, "Xamarin Mono Android 4.3"));
        public static readonly IToolchain XamarinMonoAndroid44 = From(new NetCoreAppSettings("MonoAndroid44", null, "Xamarin Mono Android 4.4"));
        public static readonly IToolchain XamarinMonoAndroid4487 = From(new NetCoreAppSettings("MonoAndroid4487", null, "Xamarin Mono Android 4.4.87"));
        public static readonly IToolchain XamarinMonoAndroid50 = From(new NetCoreAppSettings("MonoAndroid50", null, "Xamarin Mono Android 5.0"));
        public static readonly IToolchain XamarinMonoAndroid51 = From(new NetCoreAppSettings("MonoAndroid51", null, "Xamarin Mono Android 5.1"));
        public static readonly IToolchain XamarinMonoAndroid60 = From(new NetCoreAppSettings("MonoAndroid60", null, "Xamarin Mono Android 6.0"));
        public static readonly IToolchain XamarinMonoAndroid70 = From(new NetCoreAppSettings("MonoAndroid70", null, "Xamarin Mono Android 7.0"));
        public static readonly IToolchain XamarinMonoAndroid71 = From(new NetCoreAppSettings("MonoAndroid71", null, "Xamarin Mono Android 7.1"));
        public static readonly IToolchain XamarinMonoAndroid80 = From(new NetCoreAppSettings("MonoAndroid80", null, "Xamarin Mono Android 8.0"));
        public static readonly IToolchain XamarinMonoAndroid81 = From(new NetCoreAppSettings("MonoAndroid81", null, "Xamarin Mono Android 8.1"));
        public static readonly IToolchain XamarinMonoAndroid90 = From(new NetCoreAppSettings("MonoAndroid90", null, "Xamarin Mono Android 9.0"));
        public static readonly IToolchain XamarinTouch10 = From(new NetCoreAppSettings("MonoTouch10", null, "Xamarin Mono Touch 1.0"));
        public static readonly IToolchain Tizen40 = From(new NetCoreAppSettings("Tizen40", null, "Tizen 4.0"));
        public static readonly IToolchain Tizen50 = From(new NetCoreAppSettings("Tizen50", null, "Tizen 5.0"));

        public static readonly IToolchain NetCoreApp20 = From(NetCoreAppSettings.NetCoreApp20);
        public static readonly IToolchain NetCoreApp21 = From(NetCoreAppSettings.NetCoreApp21);
        public static readonly IToolchain NetCoreApp22 = From(NetCoreAppSettings.NetCoreApp22);
        public static readonly IToolchain NetCoreApp30 = From(NetCoreAppSettings.NetCoreApp30);

        protected MsBuildSdkExtrasToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor)
        {
            CustomDotNetCliPath = customDotNetCliPath;
        }

        internal string CustomDotNetCliPath { get; }

        public override bool IsSupported(BenchmarkCase benchmarkCase, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmarkCase, logger, resolver))
                return false;

            if (InvalidCliPath(CustomDotNetCliPath, benchmarkCase, logger))
                return false;

            if (benchmarkCase.Job.HasValue(EnvironmentMode.JitCharacteristic) && benchmarkCase.Job.ResolveValue(EnvironmentMode.JitCharacteristic, resolver) == Jit.LegacyJit)
            {
                logger.WriteLineError($"Currently dotnet cli toolchain supports only RyuJit, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmarkCase.Job.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently project.json does not support CpuGroups (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }
            if (benchmarkCase.Job.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver))
            {
                logger.WriteLineError($"Currently project.json does not support gcAllowVeryLargeObjects (app.config does), benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }

            var benchmarkAssembly = benchmarkCase.Descriptor.Type.GetTypeInfo().Assembly;
            if (benchmarkAssembly.IsLinqPad())
            {
                logger.WriteLineError($"Currently LINQPad does not support .NET Core benchmarks (see dotnet/BenchmarkDotNet#975), benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return false;
            }

            return true;
        }

        public static IToolchain From(NetCoreAppSettings settings)
            => new MsBuildSdkExtrasToolchain(settings.Name,
                new MsBuildSdkExtrasGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion),
                new DotNetCliBuilder(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.Timeout),
                new DotNetCliExecutor(settings.CustomDotNetCliPath),
                settings.CustomDotNetCliPath);

        internal static bool InvalidCliPath(string customDotNetCliPath, BenchmarkCase benchmarkCase, ILogger logger)
        {
            if (string.IsNullOrEmpty(customDotNetCliPath) && !HostEnvironmentInfo.GetCurrent().IsDotNetCliInstalled())
            {
                logger.WriteLineError($"BenchmarkDotNet requires dotnet cli to be installed or path to local dotnet cli provided in explicit way using `--cli` argument, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return true;
            }

            if (!string.IsNullOrEmpty(customDotNetCliPath) && !File.Exists(customDotNetCliPath))
            {
                logger.WriteLineError($"Provided custom dotnet cli path does not exist, benchmark '{benchmarkCase.DisplayInfo}' will not be executed");
                return true;
            }

            return false;
        }
    }
}
