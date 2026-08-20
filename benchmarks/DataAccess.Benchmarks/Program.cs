using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using DataAccess.Benchmarks;

// One PostgreSQL container is started in [GlobalSetup]; the in-process toolchain keeps every
// benchmark in this single process, so they share that one container instead of starting three.
var config = DefaultConfig.Instance
    .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

BenchmarkRunner.Run<OrderReadBenchmarks>(config);
