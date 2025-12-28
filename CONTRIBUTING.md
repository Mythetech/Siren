# Information and Guidelines for Contributors

Thank you for contributing to Siren and making it even better. We are happy about every contribution! Issues, bug-fixes, new features...

## Code of Conduct

Please make sure that you follow our [code of conduct](CODE_OF_CONDUCT.md).

## Minimal Prerequisites to Compile from Source

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Pull Requests

- Your Pull Request (PR) must only consist of one topic. It is better to split Pull Requests with more than one feature or bug fix into separate Pull Requests
- First fork the repository and clone your fork locally to make your changes. (The main repository is protected and does not accept direct commits.)
- You should work on a separate branch with a descriptive name. The following naming convention can be used: `feature/my-new-feature` for new features and enhancements, `fix/my-bug-fix` for bug fixes. For example, `fix/request-history-persistence` if your PR is about a bug involving request history persistence
- You should build, test and run the application locally to confirm your changes give the expected result
- Choose `main` as the target branch (or `dev` if we have a dev branch)
- All tests must pass. When you push, they will be executed on the CI server. You can also execute them locally for quicker feedback using `dotnet test`
- You must include tests when your Pull Requests alter any logic. This also ensures that your feature will not break in the future under changes from other contributors
- If there are new changes in the main repo, you should merge the main repo's (upstream) main branch or rebase your branch onto it
- Before working on a large change, it is recommended to first open an issue to discuss it with others
- If your Pull Request is still in progress, convert it to a draft Pull Request
- The PR Title should follow the following format:
```
<component/area>: <short description of changes in imperative> (<linked issue>)
```

For example:

```
Collections: Fix importing OpenAPI spec with circular references (#123)
```

- Your Pull Request should not include any unnecessary refactoring
- If there are visual changes, you should include a screenshot, gif or video
- If there are any corresponding issues, link them to the Pull Request. Include `Fixes #<issue nr>` for bug fixes and `Closes #<issue nr>` for other issues in the description ([Link issues guide](https://docs.github.com/en/github/managing-your-work-on-github/linking-a-pull-request-to-an-issue#linking-a-pull-request-to-an-issue-using-a-keyword))
- Your code should be formatted correctly ([Format documentation](https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/formatting-rules))

## Project Structure

Siren is divided into different projects. Understanding where code belongs is important:

- **Siren**: Contains all core logic, services, repositories, and data models. This is where business logic, data persistence, and application infrastructure should live.
- **Siren.Components**: Contains all UI components (Blazor `.razor` files), UI interfaces, and presentation logic. This is where all user-facing components and their interfaces belong.
- **Siren.Test**: Contains all unit tests and integration tests for the project.

### Where to put your code

- **Core logic, services, repositories**: `Siren` project
- **UI Components, UI Interfaces**: `Siren.Components` project
- **Tests**: `Siren.Test` project

## Unit Testing and Continuous Integration

We strive for good test coverage to keep things from breaking and deliver a reliable application. For every component that has C# logic we require tests that check its logic.

### How not to break stuff

When you are making changes to any components and preparing a PR make sure you run the entire test suite to see if anything broke:

```bash
dotnet test
```

### Make your code break-safe

When you are writing non-trivial logic, please add a unit test for it. By adding a test for everything you fear could break, you make sure your work is not undone by accident by future additions.

### How to write a unit test?

Simply follow the example of existing tests in the `Siren.Test` project. Look at the test structure and patterns used there.

### What does not need to be tested?

We don't need to test the complete rendered HTML of a component, or the appearance of a component. Test the logic, not the HTML. When checking changes in the HTML do simple checks like "does the HTML element exist that depends on a state".

### Continuous Integration

We have a GitHub action which runs against all Pull Requests.

It performs the following checks:
- Builds the project
- Runs the test suite

We generally require all these checks to pass before merging contributions.

