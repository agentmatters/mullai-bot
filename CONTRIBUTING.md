# Contributing to Mullai — Advanced AI Assistant

🎉 **First off, thanks for taking the time to contribute!** 🎉

Mullai is an open-source project built on **.NET**, and we welcome contributions from the community. Whether you're fixing bugs, adding new features, improving documentation, or suggesting enhancements, your help is invaluable.

This document outlines the best practices and guidelines for contributing to Mullai, with a focus on **C#**, **.NET**, and **GitHub** workflows.

---

## 📜 Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [How Can I Contribute?](#how-can-i-contribute)
   - [Reporting Bugs](#reporting-bugs)
   - [Suggesting Enhancements](#suggesting-enhancements)
   - [Pull Requests](#pull-requests)
3. [Development Best Practices](#development-best-practices)
   - [C# and .NET Guidelines](#c-and-net-guidelines)
   - [Code Style and Formatting](#code-style-and-formatting)
   - [Testing](#testing)
   - [Documentation](#documentation)
   - [Logging and Observability](#logging-and-observability)
   - [Error Handling](#error-handling)
   - [Performance Considerations](#performance-considerations)
4. [GitHub Workflow](#github-workflow)
   - [Branching Strategy](#branching-strategy)
   - [Commit Messages](#commit-messages)
   - [Pull Request Guidelines](#pull-request-guidelines)
   - [Code Reviews](#code-reviews)
5. [Adding New Features](#adding-new-features)
   - [Adding a New LLM Provider](#adding-a-new-llm-provider)
   - [Adding a New Tool](#adding-a-new-tool)
   - [Adding a New Middleware](#adding-a-new-middleware)
   - [Adding a New Agent Personality](#adding-a-new-agent-personality)
6. [Security Considerations](#security-considerations)
7. [License](#license)

---

## 🤝 Code of Conduct

By participating in this project, you agree to abide by the [Contributor Covenant Code of Conduct](https://www.contributor-covenant.org/version/2/1/code_of_conduct/). Please read it to understand the expected behavior.

---

## 🙋 How Can I Contribute?

### 🐛 Reporting Bugs

If you find a bug, please open an issue on GitHub with the following details:

- **Description**: A clear and concise description of the bug.
- **Steps to Reproduce**: How can we reproduce the issue?
- **Expected Behavior**: What did you expect to happen?
- **Actual Behavior**: What actually happened?
- **Screenshots/Logs**: If applicable, add screenshots or logs to help explain the issue.
- **Environment**: 
  - OS
  - .NET version
  - Mullai version (if applicable)

### 💡 Suggesting Enhancements

We welcome suggestions for new features or improvements! Open an issue on GitHub with:

- **Description**: What problem does this enhancement solve?
- **Proposed Solution**: How should it be implemented?
- **Alternatives**: Are there other ways to solve this?
- **Additional Context**: Any other relevant information.

### 🛠️ Pull Requests

Pull Requests (PRs) are the primary way to contribute code to Mullai. Here’s how to get started:

1. **Fork the Repository**: Create your own fork of Mullai.
2. **Clone Your Fork**: Clone your fork to your local machine.
3. **Create a Branch**: Create a new branch for your changes (`git checkout -b feature/your-feature-name`).
4. **Make Changes**: Implement your changes following the best practices below.
5. **Commit Changes**: Commit your changes with a clear message.
6. **Push to Your Fork**: Push your changes to your fork on GitHub.
7. **Open a PR**: Open a Pull Request from your fork to the main Mullai repository.

---

## 🛠️ Development Best Practices

### C# and .NET Guidelines

1. **Use Modern C# Features**: Mullai is built on **.NET 10**, so leverage modern C# features like:
   - Records
   - Pattern matching
   - Top-level statements (where appropriate)
   - Nullable reference types
   - Async/await

2. **Follow .NET Naming Conventions**: 
   - Use `PascalCase` for classes, methods, and properties.
   - Use `camelCase` for local variables and method parameters.
   - Use `_camelCase` for private fields.
   - Use `UPPER_CASE` for constants.

3. **Dependency Injection (DI)**: 
   - Use the built-in .NET DI container (`IServiceCollection`)
   - Avoid static classes where DI can be used instead.
   - Register services with appropriate lifetimes (`Singleton`, `Scoped`, or `Transient`).

4. **Async/Await**: 
   - Prefer async/await for I/O-bound operations.
   - Avoid `.Result` or `.Wait()` to prevent deadlocks.
   - Use `ConfigureAwait(false)` in library code where appropriate.

5. **Disposable Resources**: 
   - Implement `IDisposable` or `IAsyncDisposable` for resources that need cleanup.
   - Use `using` statements to ensure proper disposal.

### Code Style and Formatting

1. **Consistent Indentation**: Use 4 spaces (no tabs).
2. **Brace Style**: Use Allman style (braces on new lines) for consistency with the existing codebase.
   ```csharp
   if (condition)
   {
       // Code here
   }
   ```
3. **Line Length**: Aim for lines no longer than 120 characters.
4. **File Organization**: 
   - Group related classes in the same file if they are small and closely related.
   - Otherwise, use one class per file.
5. **XML Documentation**: 
   - Use XML comments for public APIs.
   - Document parameters, return values, and exceptions.
   ```csharp
   /// <summary>
   /// Gets the weather for a specific location.
   /// </summary>
   /// <param name="location">The location to get weather for.</param>
   /// <returns>A <see cref="WeatherResult"/> containing weather data.</returns>
   /// <exception cref="ArgumentNullException">Thrown if <paramref name="location"/> is null.</exception>
   public async Task<WeatherResult> GetWeatherAsync(string location)
   {
       // Implementation
   }
   ```

### Testing

1. **Unit Tests**: 
   - Write unit tests for new features and bug fixes.
   - Use `xUnit` as the testing framework.
   - Follow the Arrange-Act-Assert (AAA) pattern.
   - Aim for high test coverage, especially for critical paths.

2. **Integration Tests**: 
   - For features that interact with external services (e.g., LLM providers), write integration tests.
   - Use mocking frameworks like `Moq` or `NSubstitute` where appropriate.

3. **Test Naming**: 
   - Use descriptive test names (e.g., `GetWeatherAsync_WithValidLocation_ReturnsWeatherResult`).

### Documentation

1. **Code Documentation**: 
   - Document public APIs with XML comments.
   - Add comments for complex logic or non-obvious code.

2. **README Updates**: 
   - Update the `README.md` if your changes introduce new features or modify existing behavior.

3. **Architecture Decisions**: 
   - Document significant design decisions in the `docs/architecture-decisions` folder (if applicable).

### Logging and Observability

1. **Structured Logging**: 
   - Use `ILogger<T>` for logging.
   - Log meaningful events (e.g., method entry/exit, errors, warnings).
   - Include relevant context in logs (e.g., correlation IDs, timestamps).

2. **Log Levels**: 
   - `Trace`: Very detailed logs (e.g., debugging complex issues).
   - `Debug`: Debugging information.
   - `Information`: General runtime information.
   - `Warning`: Unexpected but non-critical issues.
   - `Error`: Critical failures.

3. **OpenTelemetry**: 
   - Instrument code with OpenTelemetry for tracing and metrics.
   - Use the `Mullai.Telemetry` namespace for consistent telemetry.

### Error Handling

1. **Exception Handling**: 
   - Catch specific exceptions rather than using generic `catch (Exception)`.
   - Log exceptions with meaningful context.
   - Consider using custom exceptions for domain-specific errors.

2. **Validation**: 
   - Validate method inputs with `ArgumentNullException` or `ArgumentException`.
   - Use `Guard` clauses (e.g., `Guard.Against.Null`) for consistency.

3. **Retry Policies**: 
   - For transient failures (e.g., network issues), implement retry logic with exponential backoff.
   - Use `Polly` for resilience policies.

### Performance Considerations

1. **Avoid Blocking Calls**: 
   - Prefer async I/O operations over synchronous ones.
   - Use `Task.Run` for CPU-bound work to avoid blocking threads.

2. **Memory Management**: 
   - Dispose of `IDisposable` objects properly.
   - Avoid memory leaks (e.g., event handlers, caches).

3. **Efficient Data Structures**: 
   - Choose the right data structure for the job (e.g., `Dictionary` for lookups, `List` for sequential access).
   - Use `Span<T>` or `Memory<T>` for high-performance scenarios.

---

## 🌐 GitHub Workflow

### Branching Strategy

1. **Main Branch**: 
   - The `main` branch contains production-ready code.
   - Direct pushes to `main` are restricted.

2. **Feature Branches**: 
   - Create a new branch for each feature or bug fix.
   - Use descriptive names (e.g., `feature/add-groq-provider`, `bugfix/weather-tool-null-reference`).

3. **Pull Requests**: 
   - Open a PR from your feature branch to `main`.
   - Ensure your PR is up-to-date with the latest `main` branch.

### Commit Messages

1. **Format**: 
   - Use the following format: `<type>(<scope>): <subject>`
   - Example: `feat(providers): add support for Groq LLM`

2. **Types**: 
   - `feat`: A new feature.
   - `fix`: A bug fix.
   - `docs`: Documentation changes.
   - `style`: Code style changes (e.g., formatting, missing semicolons).
   - `refactor`: Code refactoring (no functional changes).
   - `test`: Adding or modifying tests.
   - `chore`: Maintenance tasks (e.g., updating dependencies).

3. **Subject**: 
   - Use imperative mood (e.g., "Add feature" instead of "Added feature").
   - Keep it concise (50-72 characters).

### Pull Request Guidelines

1. **Description**: 
   - Provide a clear description of the changes.
   - Include the motivation behind the changes.
   - Reference any related issues (e.g., "Fixes #123").

2. **Scope**: 
   - Keep PRs focused and small. If a PR grows too large, consider breaking it into smaller PRs.

3. **Checks**: 
   - Ensure all CI checks pass (e.g., build, tests, linting).
   - Address any review comments before merging.

### Code Reviews

1. **Review Process**: 
   - At least one approval is required for merging.
   - Reviewers will check for code quality, correctness, and adherence to guidelines.

2. **Addressing Feedback**: 
   - Be responsive to review comments.
   - Update the PR with requested changes or provide a rationale if you disagree.

---

## ✨ Adding New Features

### Adding a New LLM Provider

1. **Implement `IMullaiProvider`**: 
   - Create a new class that implements `IMullaiProvider`.
   - Implement the required methods (e.g., `GenerateChatResponseAsync`).

2. **Register the Provider**: 
   - Add your provider to the DI container in `Mullai.Providers`.
   - Update `models.json` with the new provider and its models.

3. **Configuration**: 
   - Add a configuration section for the provider in `appsettings.json`.

4. **Testing**: 
   - Write unit and integration tests for the new provider.

### Adding a New Tool

1. **Implement `ITool`**: 
   - Create a new class that implements `ITool`.
   - Define the tool’s name, description, and parameters.

2. **Register the Tool**: 
   - Add the tool to the `ToolRegistry` in `Mullai.Tools`.

3. **Documentation**: 
   - Update the `README.md` to document the new tool.

### Adding a New Middleware

1. **Implement `IMiddleware`**: 
   - Create a new class that implements `IMiddleware`.
   - Define the `InvokeAsync` method to process requests/responses.

2. **Register the Middleware**: 
   - Add the middleware to the pipeline in `Mullai.Middleware`.

3. **Configuration**: 
   - If the middleware requires configuration, add it to `appsettings.json`.

### Adding a New Agent Personality

1. **Define the Agent**: 
   - Create a new class that inherits from `AgentBase`.
   - Define the agent’s instructions, tools, and skills.

2. **Register the Agent**: 
   - Add the agent to the `AgentFactory` in `Mullai.Agents`.

3. **Testing**: 
   - Test the agent in different scenarios to ensure it behaves as expected.

---

## 🔒 Security Considerations

1. **API Keys**: 
   - Never hardcode API keys in the codebase.
   - Use `appsettings.json` or environment variables for sensitive data.

2. **Input Validation**: 
   - Validate all user inputs to prevent injection attacks.
   - Sanitize inputs where necessary.

3. **Dependencies**: 
   - Keep dependencies up-to-date to avoid security vulnerabilities.
   - Use `Dependabot` to automate dependency updates.

4. **Logging**: 
   - Avoid logging sensitive information (e.g., API keys, PII).

---

## 📄 License

By contributing to Mullai, you agree that your contributions will be licensed under the **MIT License**. See the `LICENSE` file for details.

---

## 🙌 Thank You!

Your contributions make Mullai better for everyone. We appreciate your time and effort!

If you have any questions, feel free to reach out by opening an issue or joining our community discussions.

Happy coding! 🚀