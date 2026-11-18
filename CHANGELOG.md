# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2026-11-18

### Changed

- Replaced the short-window instantaneous speed estimate with a 15-second sliding throughput window.
- Reduced UI progress churn to one update every 500 ms.
- Reworked ETA to use sustained throughput and reset the sampling window after pause/resume.
- Added throughput window regression tests and updated the on-screen speed labels.

## [1.0.0] - 2026-11-18

### Added

- `.NET Framework 4.8` WinForms desktop application with no third-party runtime dependencies.
- Rotational-media detection and protection for system, program, and current-working volumes.
- Sequential 4 MiB non-zero or zero-fill writer with FAT32-aware segmentation.
- Pause, resume, stop, crash recovery, and safe cleanup flows.
- Real-time throughput, elapsed time, ETA, and capacity UI.
- Explicit drive-letter confirmation before a new fill operation.
- Automated regression tests and open-source documentation.
