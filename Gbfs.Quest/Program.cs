using Gbfs.Quest.Auth;
using Gbfs.Quest.Data;
using Gbfs.Quest.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;

[assembly: InternalsVisibleTo("Gbfs.Quest.Tests")]

var services = new ServiceCollection();

//services.AddHybridCache(); // Can be extended to support L2 cache (eg Redis, SQL Server) to add persistency / distributed storage
services.AddHttpClient(); // Need for IHttpClientFactory
services.AddSingleton<IAuthProvider, BasicConsoleAuthProvider>();

// There is little point in abstracting these visual components. They're leafs on the dependency tree and the only way they can be tested is in an E2E scenario.
services.AddSingleton<AuthVisual>();
services.AddSingleton<GameVisual>();
services.AddSingleton<GbfsService>();
services.AddSingleton<DashboardVisual>();

await using var provider = services.BuildServiceProvider();
DashboardVisual dashboard = provider.GetService<DashboardVisual>()!;

// XenoAtom.Terminal doesn't use the native HostBuilder. It instead creates a new OS-dependent console window and redirects the input/output.
Terminal.Run(dashboard.GetVisual(), () => TerminalLoopResult.Continue);