# WebLLMChatClient Unit Tests

This directory contains comprehensive unit tests for the `WebLLMChatClient` class and related components in the Portfolio Viewer WebAssembly AI project.

## Test Files Overview

### 1. `WebLLMChatClientTests.cs`
- **Purpose**: Core functionality tests for the `WebLLMChatClient` class
- **Coverage**:
  - Constructor initialization and default values
  - ChatMode property get/set operations
  - Service resolution via `GetService()`
  - Initialization process via `InitializeAsync()`
  - Client cloning functionality via `Clone()`
  - Static method `LoadJsModuleAsync()`
  - Disposal pattern testing

### 2. `WebLLMChatClientStreamingTests.cs`
- **Purpose**: Tests for streaming functionality and basic chat operations
- **Coverage**:
  - Streaming response handling with empty messages
  - Error handling for uninitialized clients
  - Clone operations and configuration preservation
  - JavaScript module initialization
  - Function calling mode configuration

### 3. `InteropInstanceTests.cs`
- **Purpose**: Tests for the `InteropInstance` class and WebLLM record types
- **Coverage**:
  - Constructor initialization and queue setup
  - Progress reporting via `ReportProgress()`
  - Completion handling via `ReceiveChunkCompletion()`
  - Argument validation and error handling
  - WebLLM record types (`WebLLMCompletion`, `Usage`, `Choice`, etc.)
  - Stream completion detection logic

### 4. `WebLLMChatClientEdgeCaseTests.cs`
- **Purpose**: Edge cases, error scenarios, and internal method testing
- **Coverage**:
  - JavaScript module loading failures
  - JSON parsing error scenarios
  - Tool call parsing with malformed data
  - Function argument extraction edge cases
  - Private method behavior via reflection
  - Concurrency and threading scenarios
  - Error logging verification

### 5. `WebLLMChatClientIntegrationTests.cs`
- **Purpose**: Integration tests demonstrating complete functionality
- **Coverage**:
  - Interface implementation verification
  - Cross-mode compatibility testing
  - Service provider integration
  - Module loading integration
  - Disposal pattern verification

## Test Architecture

### Testing Frameworks Used
- **xUnit**: Primary testing framework
- **Moq**: Mocking framework for dependencies
- **AwesomeAssertions**: Enhanced assertion library (inherited from project)

### Key Dependencies Mocked
- `IJSRuntime`: JavaScript interop runtime
- `ILogger<WebLLMChatClient>`: Logging functionality
- `IJSObjectReference`: JavaScript module references
- `IProgress<InitializeProgress>`: Progress reporting

### Test Categories

#### Public API Tests
- Constructor behavior
- Property getters/setters
- Public method functionality
- Interface compliance

#### Integration Tests
- JavaScript interop scenarios
- Progress reporting workflows
- Module loading and initialization

#### Edge Case Tests
- Error conditions and exception handling
- Malformed data scenarios
- Null/empty input validation
- Resource cleanup and disposal

#### Internal Logic Tests
- Private method behavior (via reflection)
- JSON parsing and tool call extraction
- Message preparation and transformation
- Streaming response processing

## Key Testing Patterns

### 1. Arrange-Act-Assert (AAA)
All tests follow the standard AAA pattern for clarity and consistency.

### 2. Dependency Injection Mocking
Dependencies are injected via constructor and mocked using Moq for isolation.

### 3. Reflection-Based Testing
Private methods are tested using reflection when necessary for comprehensive coverage.

### 4. Theory-Based Testing
Parameterized tests using `[Theory]` and `[InlineData]` for testing multiple scenarios.

### 5. Async/Await Testing
Proper handling of asynchronous operations with cancellation token support.

## Coverage Areas

### ? Covered Functionality
- Basic client operations (constructor, properties, disposal)
- JavaScript module loading and initialization
- Progress reporting and completion handling
- JSON parsing and tool call extraction
- Error handling and logging
- Clone operations and state preservation
- Interface implementations and service resolution

### ?? Areas for Future Enhancement
- Full streaming workflow integration testing
- Real JavaScript interop testing (requires browser environment)
- Performance and stress testing
- Complex tool calling scenarios with actual functions

## Running the Tests

To run all WebLLMChatClient tests:

```bash
dotnet test PortfolioViewer.WASM.AI.UnitTests --filter "WebLLMChatClient*"
```

To run specific test categories:

```bash
# Core functionality tests
dotnet test --filter "WebLLMChatClientTests"

# Streaming tests
dotnet test --filter "WebLLMChatClientStreamingTests"

# Edge case tests
dotnet test --filter "WebLLMChatClientEdgeCaseTests"

# Integration tests
dotnet test --filter "WebLLMChatClientIntegrationTests"
```

## Test Data and Scenarios

The tests cover various scenarios including:
- Empty and null inputs
- Malformed JSON data
- Network failures and timeouts
- Invalid configuration states
- Resource disposal and cleanup
- Cross-mode compatibility
- Error propagation and logging

These comprehensive tests ensure the WebLLMChatClient is robust, reliable, and maintainable for the Portfolio Viewer WebAssembly application.