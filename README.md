# Drewsteinacher.Analyzer

A personal collection of Roslyn Analyzer rules.

[//]: # (## Installation)
[//]: # ()
[//]: # (Install the NuGet package [Drewsteinacher.Analyzer]&#40;https://www.nuget.org/packages/Drewsteinacher.Analyzer/&#41;)
[//]: # ()
[//]: # (```bash)
[//]: # (dotnet package add Drewsteinacher.Analyzer)
[//]: # (```)

## Rules

| Id                                                                                                         | Category | Description                                           | Severity | Is enabled | Code fix |
|------------------------------------------------------------------------------------------------------------|----------|-------------------------------------------------------|:--------:|:----------:|:--------:|
| [DRWSTR0001](https://github.com/drewsteinacher/Drewsteinacher.Analyzer/blob/main/docs/DRWSTR0001.md) | Usage    | Uninitialized property assigned in member initializer |    ❌     |     ✔️     |    ❌     |
