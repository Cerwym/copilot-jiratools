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

### Current Implementation Issues
- `CommentTaskCommand` parses markdown tables with hardcoded column expectations
- Table structure is fixed: `| Project | Status | Jira Task | Comments |`
- No validation of data types or constraints
- Error handling is limited to "table not found" scenarios

## Proposed Solution
Implement a schema-based configuration system that supports:

### 1. Configuration File Format Options
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

#### Phase 1: Core Infrastructure
- [ ] Create schema definition files
- [ ] Implement configuration file parser (JSON/YAML/TOML)
- [ ] Add schema validation
- [ ] Create configuration model classes

#### Phase 2: Command Refactoring
- [ ] Refactor `CommentTaskCommand` to use schema-based config
- [ ] Update project selection logic
- [ ] Implement backwards compatibility with markdown tables
- [ ] Add migration utilities for existing markdown files

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

### 1. Extensibility
- Easy to add new fields or data types via schema updates
- Support for complex nested data structures
- Plugin/extension system for custom fields

### 2. Validation & Reliability
- Schema validation ensures data integrity
- IDE support for autocompletion and error detection
- Clear error messages for invalid configurations

### 3. Developer Experience
- Better tooling support (IntelliSense, validation)
- Self-documenting through schema definitions
- Version control friendly (structured text files)

### 4. Future-Proofing
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
