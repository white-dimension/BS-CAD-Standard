# BS Max Studio

Professional 3ds Max studio framework for long-term tool development.

Phase 1 is intentionally a framework-only MVP:

- one always-on-top floating capsule toolbar
- compact modern dark UI shell
- first-row module capsules
- second-row command capsules that expand/collapse
- persisted window position and size
- clean module registration structure
- future-friendly boundaries for C#/.NET/3ds Max SDK migration

## Run

In 3ds Max:

1. Open `Scripting > Run Script...`
2. Select `startup.ms`
3. The `BS Max Studio` window opens.

Every command currently shows:

```text
Coming Soon
```

## Current Structure

```text
BS-Max-Studio
├── startup.ms
├── UI
│   ├── BSMS_MainWindow.ms
│   └── BSMS_Theme.ms
├── Modules
│   ├── Layer
│   ├── Material
│   ├── Corona
│   ├── Model
│   ├── Camera
│   └── More
├── Resources
├── Icons
├── Core
│   ├── BSMS_Config.ms
│   └── BSMS_ModuleRegistry.ms
├── Utils
└── Docs
```

## Design Direction

The UI avoids default Autodesk-style buttons. It uses a compact floating capsule-bar interface inspired by Adobe, Figma, JetBrains Rider, and modern web app top bars.

Theme values:

- Background: `#2B2B2B`
- Hover: `#383838`
- Pressed: `#4A4A4A`
- Accent: `#4EA1FF`
- Text: light gray

## Architecture Notes

The UI shell does not know the internal implementation of each tool. Modules register metadata through:

```maxscript
BSMS_RegisterModule "layer" "图层" #("新建归层", "移动图层")
```

Later, each item can point to an action handler, C# command class, icon, permission rule, or context requirement.

Recommended future C# direction:

- `Core`: command bus, settings, logging, service registration
- `UI`: WPF or WinForms dockable panel hosted through 3ds Max SDK
- `Modules`: independent feature assemblies
- `Resources`: theme tokens, localization, templates
- `Icons`: vector icons exported as XAML/SVG/PNG

## Phase 1 Scope

Completed:

- framework shell
- navigation modules
- placeholder tools
- always-on-top floating toolbar
- draggable borderless window
- expand/collapse second-row commands
- size and position persistence
- project directory layout

Not included yet:

- production command implementations
- Corona SDK integration
- material library logic
- layer operations
- installer
- compiled C# plugin
