# F# Testing Strategy & Best Practices

This document outlines the testing strategy for our full-stack F# applications. It serves as a tutorial and a reference for writing effective, maintainable, and high-value tests. Our approach is aligned with modern agile methodologies and the ISTQB curriculum to ensure we build robust and reliable software.

## Core Philosophy: The Test Pyramid

Our testing strategy is guided by the **Test Pyramid**. This model emphasizes writing tests with different levels of granularity, prioritizing faster, more isolated tests over slower, broader ones.

-   **Unit Tests (Many & Fast):** The foundation of our pyramid. They are fast to write, fast to run, and precisely pinpoint failures in our core logic.
-   **Integration Tests (Fewer & Slower):** The middle layer. They verify that different components of our system work together correctly (e.g., API handler + database).
-   **End-to-End (E2E) Tests (Very Few & Slowest):** The peak. They validate a complete user flow from the browser to the database and back.

This approach gives us the best return on investment: a fast feedback loop for day-to-day development and high confidence that the system works as a whole.

## Test Types in Our Stack

Here are the specific types of tests we write, how they align with the ISTQB curriculum, and the tools we use for them.

### 1. Unit Tests (Component Testing)

*   **ISTQB Alignment:** Component Testing.
*   **What it is:** A test that verifies a single, small, and isolated piece of software—typically a pure function—without involving external dependencies like databases, networks, or file systems.
*   **Why we write them:** To verify our core business logic, validation rules, and data transformations are correct. They are the fastest and most reliable tests we have.
*   **Tools:** **Expecto** (for test structure and assertions).
*   **Example:** Testing the `isValidName` validator.

    ```fsharp
    // in tests/Server.Tests/ValidatorTests.fs
    module ValidatorTests =
        open Expecto
        open Shared.Validators // The module we are testing

        [<Tests>]
        let tests =
            testList "Validators.isValidName" [
                testCase "should return true for a simple name" <| fun _ ->
                    Expect.isTrue (isValidName "Vegard") "A simple valid name should pass"

                testCase "should return false for a name with numbers (boundary)" <| fun _ ->
                    Expect.isFalse (isValidName "Vegard123") "A name containing numbers should fail"

                testCase "should return false for an empty string (boundary)" <| fun _ ->
                    Expect.isFalse (isValidName "") "An empty string should be invalid"
            ]
    ```

### 2. Property-Based Tests

*   **ISTQB Alignment:** A powerful, data-driven technique within Component Testing.
*   **What it is:** A test where you define a *property* or *invariant* (a rule that must always be true) and let the testing framework generate hundreds of random inputs to try and falsify that property.
*   **Why we write them:** To find edge cases and bugs we would never think to write manual tests for. They are perfect for testing parsers, serializers, and any function that must be robust against a wide range of data. This automates **Equivalence Partitioning** and **Boundary Value Analysis**.
*   **Tools:** **FsCheck** integrated with **Expecto**.
*   **Example (`link-sharing-app`):** Testing that the routing functions `pageToPath` and `pathParser` are inverses of each other.

    ```fsharp
    // in tests/Client.Tests/RoutingTests.fs
    testProperty "Routing functions should round-trip correctly" (fun (page: Page) ->
        // The property: for any given page, converting it to a path and back
        // should result in the original page.
        let result = page |> pageToPath |> pathParser
        Expect.equal result page "The round-trip should yield the original page"
    )
    ```

### 3. Integration Tests (Component Integration Testing)

*   **ISTQB Alignment:** Component Integration Testing.
*   **What it is:** A test that verifies the interaction and communication between two or more components of our application, including real external infrastructure like a database.
*   **Why we write them:** To ensure that our unit-tested components are wired together correctly and can successfully communicate with real dependencies. We use them to test a thin vertical slice of our API endpoints.
*   **Tools:** **Expecto**, **Testcontainers** (to run a real PostgreSQL database in Docker for each test run), and `Microsoft.AspNetCore.Mvc.Testing` (to host our server in-memory).
*   **Example (`link-sharing-app`):** Testing the "Save Links" API endpoint.

    ```fsharp
    // Conceptual example in tests/Server.IntegrationTests/LinksApiTests.fs
    testAsync "PUT /api/links saves links and returns 204" {
        // 1. Arrange: Start a real PostgreSQL container and a test server.
        let! postgresContainer = startDatabaseContainer ()
        let! client = createAuthenticatedTestClient postgresContainer.ConnectionString

        // 2. Act: Make a real HTTP request to the in-memory server.
        let! response = client.PutAsJsonAsync("/api/links", someLinksPayload)

        // 3. Assert: Verify the HTTP response and the database state.
        Expect.isSuccessStatusCode response "The API call should succeed"
        let! savedLinksInDb = queryDatabaseForLinks postgresContainer.ConnectionString
        Expect.equal (savedLinksInDb.Length) 1 "The link should be saved in the database"
    }
    ```

## Our Development Workflow (TDD/BDD Hybrid)

To get the best of both worlds (fast feedback and high confidence), we follow this workflow for new features:

1.  **Write One Failing Integration Test:** Start by writing a single, high-level integration test that describes the desired outcome of the feature (e.g., "POSTing to `/api/tracks` creates a new track"). This is your "acceptance criterion." Run it; it will fail.

2.  **Drop Down to Unit Tests:** Switch to the "inner loop." Use classic Test-Driven Development (TDD) with Expecto and FsCheck to build the pure business logic needed to make the feature work. Write a failing unit/property test, then the code to make it pass, then refactor.

3.  **Make the Integration Test Pass:** Wire up your unit-tested components in your API handler. Re-run the integration test from step 1. It should now pass.

4.  **Refactor with Confidence:** With your full suite of tests passing, you can now clean up your code, confident that you haven't broken anything.

## Getting Started

### Project Setup

1.  Create a new test project: `dotnet new console -lang F# -o tests/Server.Tests`
2.  Add packages: `dotnet add package Expecto`, `dotnet add package FsCheck`, `dotnet add package Expecto.FsCheck`
3.  Reference your implementation project: `dotnet add reference ../src/Server/Server.fsproj`
4.  Add the test project to the solution: `dotnet sln add tests/Server.Tests/Server.Tests.fsproj`

### Running Tests

Run all tests from the root of the repository with:
```bash
dotnet test
```

### Checking Code Coverage

To generate a code coverage report, run:
```bash
dotnet test --collect:"XPlat Code Coverage"
```
This will generate a `coverage.cobertura.xml` file in the `TestResults` directory for each test project.
