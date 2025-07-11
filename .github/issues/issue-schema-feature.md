# Feature Request: Schema-Based Project Configuration

## Summary
Replace the current markdown table-based approach with a defined schema system to improve extensibility, maintainability, and provide better structure for project and task management workflows.

## Current State Problems
The current implementation relies on parsing markdown tables in `status.md` files, which presents several limitations:

1. **Limited Extensibility**: Adding new fields or data types requires parsing changes
2. **Fragile Parsing**: Markdown table parsing is brittle and error-prone
3. **No Validation**: No schema validation for data integrity
4. **Poor Tooling Support**: No IDE support for autocompletion or validation
5. **Difficult to Extend**: Hard to add complex data structures or relationships

### Real-World Issues Discovered During Development
**Recent testing improvements (achieving 100% test pass rate) revealed critical limitations:**

#### CommentTaskCommand Parsing Brittleness
- **Fixed Table Structure**: The current implementation hardcodes exactly 4 columns: `| Project | Status | Jira Task | Comments |`
- **Column Order Dependency**: Parser expects columns in exact order, fails if users rearrange
- **Header Case Sensitivity**: Parser may fail if headers have different capitalization
- **No Flexible Field Addition**: Cannot add new columns without code changes
- **Test Isolation Issues**: Markdown parsing created test interference due to file system dependencies

#### Parsing Logic Fragility
```csharp
// Current brittle implementation in CommentTaskCommand
if (line.StartsWith("|") && line.Split('|').Length >= 5)
{
    var parts = line.Split('|');
    var project = parts[1].Trim();
    var status = parts[2].Trim(); 
    var jiraTask = parts[3].Trim();
    var comments = parts[4].Trim();
    // ... hardcoded parsing logic
}
```

**Problems discovered:**
- No validation that headers match expected format
- Array index errors if table format changes
- No graceful degradation for malformed tables
- Difficult to unit test due to file system dependencies
- Zero flexibility for different project structures

#### Test Environment Challenges
- **File System Dependencies**: Tests require creating temporary markdown files
- **Parsing Inconsistencies**: Different markdown readers might interpret tables differently
- **State Management**: Shared state between tests caused interference
- **Limited Mockability**: Hard to mock file-based configuration for isolated testing

## Proposed Solution
Implement a schema-based configuration system that addresses the discovered limitations:

### How Schema Approach Solves Current Problems

#### 1. **Eliminates Parsing Brittleness**
Instead of fragile string parsing:
```csharp
// Current fragile approach
var parts = line.Split('|');
var project = parts[1].Trim(); // Breaks if column order changes

// Schema-based approach
var config = await configLoader.LoadAsync<ProjectConfig>("config.json");
foreach (var project in config.Projects)
{
    var jiraTask = project.JiraTaskId; // Type-safe, always available
    var status = project.Status; // Validated enum values
}
```

#### 2. **Provides Flexible Structure**
```json
// Users can add custom fields without code changes
{
  "projects": [
    {
      "id": "frontend-redesign",
      "name": "Frontend Redesign",
      "jiraTaskId": "FRONT-123", 
      "status": "in-progress",
      "priority": "high",
      "assignee": "john.doe",
      "customFields": {
        "workClassification": "Development",
        "estimatedHours": 40,
        "tags": ["ui", "responsive", "accessibility"]
      }
    }
  ]
}
```

#### 3. **Enables Comprehensive Testing**
```csharp
// Easy to mock and test
var mockConfig = new ProjectConfig
{
    Projects = new[]
    {
        new Project { Id = "test", JiraTaskId = "TEST-123", Status = ProjectStatus.InProgress }
    }
};
var command = new CommentTaskCommand(mockJiraClient, mockConfig, logger);
// No file system dependencies, perfect isolation
```

### Configuration File Format Options
**Option A: JSON Schema + JSON Files**
```json
{
  "$schema": "./schemas/project-config.schema.json",
  "projects": [
    {
      "id": "proj-1",
      "name": "Frontend Redesign",
      "jiraTaskId": "FRONT-123",
      "status": "in-progress",
      "priority": "high",
      "assignee": "john.doe",
      "comments": [
        {
          "date": "2025-01-15",
          "text": "Initial setup complete",
          "author": "john.doe"
        }
      ],
      "customFields": {
        "workClassification": "Development",
        "components": ["UI", "UX"]
      }
    }
  ]
}
```

**Option B: YAML + Schema Validation**
```yaml
# project-config.yaml
projects:
  - id: proj-1
    name: "Frontend Redesign"
    jiraTaskId: "FRONT-123"
    status: in-progress
    priority: high
    assignee: john.doe
    comments:
      - date: "2025-01-15"
        text: "Initial setup complete"
        author: john.doe
    customFields:
      workClassification: Development
      components: [UI, UX]
```

**Option C: TOML Configuration**
```toml
[[projects]]
id = "proj-1"
name = "Frontend Redesign"
jiraTaskId = "FRONT-123"
status = "in-progress"
priority = "high"
assignee = "john.doe"

[[projects.comments]]
date = "2025-01-15"
text = "Initial setup complete"
author = "john.doe"

[projects.customFields]
workClassification = "Development"
components = ["UI", "UX"]
```

### 2. Schema Definition
Define schemas for validation and tooling support:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "JiraTools Project Configuration",
  "type": "object",
  "properties": {
    "projects": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "id": {"type": "string", "pattern": "^[a-zA-Z0-9-_]+$"},
          "name": {"type": "string", "minLength": 1},
          "jiraTaskId": {"type": "string", "pattern": "^[A-Z]+-[0-9]+$"},
          "status": {
            "type": "string",
            "enum": ["todo", "in-progress", "review", "done", "blocked"]
          },
          "priority": {
            "type": "string",
            "enum": ["low", "medium", "high", "critical"]
          },
          "comments": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "date": {"type": "string", "format": "date"},
                "text": {"type": "string"},
                "author": {"type": "string"}
              },
              "required": ["date", "text", "author"]
            }
          }
        },
        "required": ["id", "name", "status"]
      }
    }
  },
  "required": ["projects"]
}
```

### 3. Implementation Plan

#### Phase 1: Core Infrastructure (Addresses Testing Issues)
- [ ] Create schema definition files with comprehensive validation rules
- [ ] Implement configuration file parser (JSON/YAML/TOML) with proper error handling
- [ ] Add schema validation with detailed error messages
- [ ] Create configuration model classes with strong typing
- [ ] **Implement dependency injection for configuration** (enables better testing)
- [ ] **Add configuration abstractions** (eliminates file system dependencies in tests)

#### Phase 2: Command Refactoring (Fixes Parsing Brittleness)
- [ ] **Refactor `CommentTaskCommand` to eliminate hardcoded table parsing**
- [ ] **Replace string splitting with type-safe configuration access**
- [ ] Update project selection logic with schema-validated data
- [ ] Implement backwards compatibility with markdown tables (transition period)
- [ ] Add migration utilities for existing markdown files
- [ ] **Create comprehensive unit tests with mocked configuration**

#### Phase 3: Enhanced Validation & Flexibility
- [ ] Add support for custom fields through schema extensions
- [ ] Implement project templates with validation
- [ ] Add configuration validation commands (`jiratools validate-config`)
- [ ] Support for multiple configuration files/workspaces
- [ ] **Add real-time configuration validation during editing**

#### Phase 3: Extended Features
- [ ] Add support for custom fields through schema extensions
- [ ] Implement project templates
- [ ] Add configuration validation commands
- [ ] Support for multiple configuration files/workspaces

#### Phase 4: Developer Experience
- [ ] Create VS Code extension for schema validation
- [ ] Add CLI commands for config management
- [ ] Generate documentation from schema
- [ ] Add auto-completion support

## Benefits

### 1. **Eliminates Testing Challenges** ⭐ *Critical for 100% Test Coverage*
- **Perfect Test Isolation**: No file system dependencies in unit tests
- **Easy Mocking**: Configuration can be injected and mocked cleanly
- **Parallel Test Execution**: No shared state between tests
- **Comprehensive Coverage**: All edge cases can be tested with synthetic data
- **Faster Test Execution**: No I/O operations in unit tests

### 2. Extensibility
- Easy to add new fields or data types via schema updates
- Support for complex nested data structures
- Plugin/extension system for custom fields

### 3. Validation & Reliability
- Schema validation ensures data integrity
- IDE support for autocompletion and error detection
- Clear error messages for invalid configurations
- **Compile-time safety** with strongly-typed configuration models

### 4. Developer Experience
- Better tooling support (IntelliSense, validation)
- Self-documenting through schema definitions
- Version control friendly (structured text files)
- **No more parsing debugging** - eliminate string manipulation errors

### 5. Future-Proofing
- Easier to add integrations with other tools
- Support for multiple project management methodologies
- API-friendly structure for potential web interfaces

## Technical Implementation Notes

### Configuration Loading Priority
1. Command-line specified config file
2. `.jiratools/config.json` in current directory
3. `.jiratools/config.json` in parent directories (git-style search)
4. User home directory config
5. Fallback to markdown parsing for backwards compatibility

### Migration Strategy
- Provide `jiratools migrate` command to convert markdown tables to schema format
- Support both formats during transition period
- Clear deprecation timeline for markdown approach

### Performance Considerations
- Cache parsed configuration files
- Validate schema only when files change
- Lazy loading for large configuration files

## Success Criteria
- [ ] Schema-based configuration system implemented
- [ ] All existing functionality works with new system
- [ ] Migration path from markdown tables provided
- [ ] Documentation and examples created
- [ ] Performance is equal or better than current implementation
- [ ] Developer tooling support available

## Future Enhancements
- Web-based configuration editor
- Integration with popular project management tools
- Real-time collaboration features
- Configuration templates and sharing
- Advanced reporting and analytics based on structured data
