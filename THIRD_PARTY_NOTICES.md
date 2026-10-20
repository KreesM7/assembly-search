# Third-party dependencies and evidence

Product dependencies are pinned in Directory.Packages.props and project packages.lock.json files. Upstream copyright notices and license terms continue to apply; consult each package's bundled notices for transitive/native components.

- [Avalonia](https://github.com/AvaloniaUI/Avalonia), including Desktop, Fluent and remote protocol packages — MIT.
- [dnlib](https://github.com/0xd4d/dnlib) — MIT.
- [SkiaSharp](https://github.com/mono/SkiaSharp) and native assets — MIT wrapper; native Skia includes its own third-party notices.
- [HarfBuzzSharp](https://github.com/mono/SkiaSharp) and native assets — MIT wrapper; native HarfBuzz includes its upstream notices.
- [MicroCom](https://github.com/kekekeks/MicroCom) and [Tmds.DBus](https://github.com/tmds/Tmds.DBus) — MIT.
- [.NET](https://github.com/dotnet/runtime) — MIT; the self-contained runtime includes its license and third-party notices.
- xUnit, Microsoft.NET.Test.Sdk, and Avalonia.Headless.XUnit are test dependencies and are not product entry points.

The user-supplied dnSpy build was examined as the search-behavior reference. Its binaries are not redistributed in this repository or Linux package. Static evidence identifies the exact examined assemblies by hashes and MVIDs. See docs/evidence/findings.md and parity-audit.md for observations, limitations and attribution context. This project does not claim to be an official dnSpy distribution or a complete dnSpy port.
