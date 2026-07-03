# Modules

Each module registers itself through `BSMS_RegisterModule`.

Phase 1 modules only provide navigation labels and placeholder tools. Future C# modules should keep the same boundary:

- module identity
- display title
- tool/action list
- implementation handler
- optional icon/resource references

This keeps the UI shell independent from business logic.
