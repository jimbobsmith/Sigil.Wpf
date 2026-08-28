# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-28

### Added

- Fluent rules engine for any WPF dependency property (`TemporaryRule`, `GlobalRule`, `PropertyRule`, defaults).
- `AutoBind` attached property to attach indexer bindings from a value path.
- `SigilViewModel` base class for the engine, indexer, and `INotifyPropertyChanged`.
- `RuleResult.FallThrough` sentinel so `null` is a real `resultIfNoMatch` value.
- `net8.0-windows` and `net10.0-windows` target frameworks.

### Notes

- This is the first public release. The assembly and package id are `Sigil.Wpf` so they do not collide with the Sigil IL-emitter package.
