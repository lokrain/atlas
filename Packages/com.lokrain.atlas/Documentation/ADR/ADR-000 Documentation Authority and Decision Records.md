# ADR-000 Documentation Authority and Decision Records

Status: Accepted  
Date: 2026-05-17

## Context

Atlas is being restarted with a new package architecture. The project needs a documentation system that prevents vocabulary drift, accidental architecture changes, and implementation-first design debt.

The package must distinguish:

```text
architecture decisions
design specifications
implementation plans
source code
tests
```

## Decision

Atlas uses this documentation authority order:

```text
Architecture rules
-> accepted ADRs
-> design specs
-> implementation plans
-> source code and tests
```

ADRs freeze architectural decisions and rejected alternatives.

Design specs explain algorithms, data ownership, compiler behavior, and runtime representation.

Implementation plans define order of work and test gates.

Source code implements accepted decisions.

Tests enforce source-level invariants.

## Rules

An ADR must contain:

```text
Status
Context
Decision
Consequences
Rejected alternatives
```

A design spec must contain:

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

## Consequences

Design changes must be recorded before code is changed when they alter ownership, vocabulary, layering, or deterministic behavior.

Docs must not describe future execution features as implemented facts.

## Rejected Alternatives

### Source-only architecture

Rejected because source alone does not preserve vocabulary intent and makes later refactors harder.

### Large prose-only architecture document

Rejected because ADRs are easier to audit when decisions evolve.

### Validation by tests only

Rejected because tests prove behavior but do not explain architectural authority.
