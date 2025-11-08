Here's a summary you can feed to your Claude Code agent:

---

**Project: Periodic Events System - Protocol Buffers Schema**

**Background:**
We designed a system for managing periodic/recurring events where the next occurrence is scheduled based on the *completion date* of the previous occurrence, rather than a fixed schedule. This is useful for maintenance tasks, habit tracking, or any recurring activity where the next instance should be triggered by when you actually complete the previous one (e.g., "change oil every 90 days after the last oil change").

**Architecture:**
The system uses a two-table design:
1. **PeriodicEvent** - The template/definition of the recurring event (what it is, how often it repeats)
2. **EventOccurrence** - Individual instances that get scheduled and completed

**Key Logic:**
When an occurrence is marked complete:
- Record the completion date
- Calculate next scheduled date = completion_date + period_days
- Create a new pending occurrence with that scheduled date

**Schema Format:**
We chose Protocol Buffers (proto3) for the schema definition because:
- You wanted to learn protobuf
- It provides compact binary serialization
- Type-safe code generation for C#
- Language-agnostic (same schema works across platforms)
- Excellent for microservices/gRPC if needed later

**File Location:**
`periodic_events.proto` - Contains complete schema including:
- Core data models (PeriodicEvent, EventOccurrence)
- Supporting types (Date, enums for PeriodType and OccurrenceStatus)
- Request/Response messages for API operations
- Optional gRPC service definition
- Flexible period types (days/weeks/months/years) beyond just period_days

**Next Implementation Steps:**
1. Generate C# classes from the .proto file using protoc compiler
2. Implement the business logic for completing occurrences and scheduling next ones
3. Add persistence layer (database or file storage)
4. Build UI for managing events and marking completions

**Target Stack:** C#/.NET with potential integration into existing Blazor/WinUI applications.

---