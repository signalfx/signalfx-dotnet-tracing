# Changelog

<<<<<<< HEAD
All notable changes to this project will be documented in this file.
=======
## [Release 1.28.8](https://github.com/DataDog/dd-trace-dotnet/releases/tag/v1.28.8)

## Changes
* Adds automatic instrumentation for GraphQL 3 and 4 (#1751)
* Adds automatic instrumentation for Elasticsearch 7 (#1760, #1821)
* Adds initial beta Azure Functions automatic instrumentation (#1613)
* Remove CallTarget Integrations from json file. Integrations are now loaded directly from the dll. (#1693, #1780, #1771)
* Add exceptions to active span during ASP.NET Web API 2 message handler exception (#1734 #1772)
* Refactor ILogger integration (#1740, #1798, #1770)
* Refactor repository folder locations (#1748, #1762, #1762, #1806, #1759, #1810)
* Updates to the shared native loader (#1755, #1825, #1826, #1815, #1729)
* AppSec updates (#1757, #1758, #1768, #1777, #1778, #1796)
* Improve DuckType generic methods support (#1733)
* Rename ADO.NET providers integration names (#1781)
* Enable shared logger for managed log file (#1788)
* Remove unused ISpan/IScope (#1746, #1749)

## Fixes
* Restore Tracer.ActiveScope in ASP.NET when request switches to a different thread (#1783)
* Fix duplicating integrations due to multiple Initialize calls from different AppDomains. (#1794)
* Fix reference to mscorlib causing failures with reflection (#1797)
* Propagate sampling priority to all spans during partial flush (#1803)
* JITInline callback refactor to fix race condition. (#1823)

## Build / Test
* Update .NET SDK to 5.0.401 (#1782)
* Improvements to build process and automations (#1812, #1704, #1773, #1792, #1793, #1799, #1801, #1808, #1814)
* Disable memory dumps in CI (#1822)
* Fix compilation directive for NET5.0 (#1731)
* Restore the original env-var value before asserting. (#1816)
* Catch object disposed exception in Samples.HttpMessageHandler (#1774)
* Add minimal test applications that use service bus libraries (#1690)
* Synchronously wait for tasks in HttpClient sample (#1703)
* Update test spans from Crank runs (#1592)
* Update code owners (#1750, #1785)
* Exclude liblog from code coverage by filepath (#1753)
* Move tracer snapshots to /tracer/test/snapshots directory (#1766)
* Increase timeout in MassTransit smoke tests (#1779)
* Fix CIEnvironmentVariable test (#1765)

[Changes since 1.28.7](https://github.com/DataDog/dd-trace-dotnet/compare/v1.28.7...v1.28.8)

## [Release 1.28.6](https://github.com/DataDog/dd-trace-dotnet/releases/tag/v1.28.6)
>>>>>>> ed0e465a7 ([Version Bump] 1.28.8 (#1827))

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.1] - 2021-05-06

Initial pre-alpha release.

[Unreleased]: https://github.com/opentelemetry/opentelemetry-dotnet-instrumentation/compare/v0.0.1...HEAD
[0.0.1]: https://github.com/opentelemetry/opentelemetry-dotnet-instrumentation/releases/tag/v0.0.1
