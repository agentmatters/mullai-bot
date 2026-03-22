# Mullai Documentation

Welcome to the technical documentation for Mullai, an advanced AI Assistant framework built on .NET. This directory contains detailed information about the project's architecture, components, and specialized features.

## Index

- [Project Overview](../README.md): High-level introduction, features, and quick start guide.
- [Contributing Guidelines](../CONTRIBUTING.md): Standards and workflows for contributing to Mullai.
- [Observability Stack](../docker/observability/README.md): Details on the OpenTelemetry, Jaeger, and Prometheus integration.
- [Architecture Docs](./architecture/README.md): Runtime architecture deep dives and operational guidance.
- [Mullai Web Task Runtime](./architecture/runtime/README.md): Detailed architecture, flow diagrams, and extension points.
- [Mullai Web Task Runtime API](./architecture/runtime/API.md): Endpoint contracts and payload examples.
- [Mullai Web Task Runtime Operations](./architecture/runtime/OPERATIONS.md): Capacity, reliability, and troubleshooting runbook.

## Project Structure & Components

Mullai is designed with a modular architecture to ensure flexibility and scalability. Key components include:

- **Core Framework**:
    - `Mullai.Abstractions`: Fundamental interfaces and base classes.
    - `Mullai.Agents`: Agent personalities and factory logic.
    - `Mullai.Middleware`: Pipeline for function calling, security, and guardrails.
- **Capabilities & State**:
    - `Mullai.Tools`: Built-in capabilities like File System and CLI tools.
    - `Mullai.Memory` & `Mullai.Skills`: Persistent context and dynamic agent skills.
- **Infrastructure**:
    - `Mullai.Providers`: Adapters for various LLM backends (Gemini, Groq, Mistral, etc.).
    - `Mullai.Telemetry`: Shared OpenTelemetry configuration.
    - `Mullai.Global.ServiceConfiguration`: Centralized catalog for models and API keys.
- **Interfaces**:
    - `Mullai.CLI`: Rich console application for interactive debugging.
    - `Mullai.Web.Wasm`: Blazor-based web interface.

## Documentation Assets

Visual assets such as architecture diagrams and demo screenshots are stored in the [assets](./assets) directory.

---

> [!TIP]
> For a deep dive into how agents are orchestrated, refer to the source code in `src/Mullai.Agents`.
