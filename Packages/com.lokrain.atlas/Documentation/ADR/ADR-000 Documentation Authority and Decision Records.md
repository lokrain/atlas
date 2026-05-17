# ADR-000 Documentation Authority and Decision Records

Status: Accepted  
Date: 2026-05-17

## Context

Atlas is being rebuilt around strict domain invariants. The project needs documentation that prevents vocabulary drift, accidental architecture changes, and implementation-first design debt.

The package must distinguish:

```text
architecture decisions
design specifications
implementation plans
source code
tests
```

Documentation must also distinguish implemented facts from planned execution-layer work.

## Decision

Atlas uses this documentation authority order:

```text
Architecture rules
-> accepted ADRs
-> design specifications
-> implementation plans
-> source code and tests
```

ADRs record architectural decisions and rejected alternatives.

Design specifications explain ownership, validation, data models, compilation behavior, and failure behavior.

Implementation plans define order of work and completion criteria.

Source code implements accepted decisions.

Tests enforce source-level invariants.

## Documentation Rules

An ADR must contain:

```text
Status
Context
Decision
Consequences
Rejected alternatives
```

A design specification must contain:

```text
Purpose
Ownership
Data model
Validation / compilation behavior
Failure behavior
Testing requirements
Open questions
```

An implementation plan must contain:

```text
Scope
File order
Expected tests
Rejected shortcuts
Completion criteria
```

Docs must not describe deferred execution features as implemented facts.

Docs must use current architecture vocabulary:

```text
GenerationRequestDescriptor
GenerationCatalog
GenerationRequestResolver
GenerationRequest
GenerationPlanCompiler
GenerationPlan
```

Docs must not describe the old flow:

```text
GenerationRequest + GenerationCatalog -> GenerationPlanCompilerResult
```

## Consequences

Design changes must be recorded before code changes when they alter ownership, vocabulary, layering, or deterministic behavior.

Documents must be rewritten when source architecture changes materially.

Tests prove behavior, but ADRs explain why the behavior exists.

## Rejected Alternatives

### Source-only architecture

Rejected because source code alone does not preserve vocabulary intent and makes later refactors harder to audit.

### Large prose-only architecture document

Rejected because ADRs are easier to audit when decisions evolve.

### Validation by tests only

Rejected because tests prove behavior but do not explain architectural authority.
