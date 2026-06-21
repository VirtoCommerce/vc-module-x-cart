using System.Reflection;
using VirtoCommerce.XCart.Benchmark;

// Upstream (un-extended XCart) runner. The benchmark logic lives in the Core library's *BenchmarksBase
// abstract classes; the concrete subclasses that bake the upstream module setup are source-generated
// into THIS exe from the [assembly: BenchmarkSetup] below, so FromAssembly(this exe) discovers them.
[assembly: BenchmarkSetup(typeof(UpstreamCartBenchmarkSetup))]

BenchmarkProgram.Run(Assembly.GetExecutingAssembly(), args);
