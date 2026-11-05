# 6. Validation in layers

Status: accepted, 2026-11-05

## Context

The API is public and called by mobile apps. Input is untrusted, and old app versions stay in the field for a long
time. Bad input must be rejected with errors a developer can act on, and never half-processed.

## Decision

Four layers, each catching what the next one can't:

1. **Strict JSON.** A 400 that names the JSON path rejects any of these:
   - unknown fields;
   - missing or null required fields;
   - missing or unknown item types;
   - enum numbers;
   - numbers sent as strings;
   - duplicate keys.

   The contract types declare what is required and what may be null, and the serializer enforces exactly that
   (`RespectRequiredConstructorParameters`, `RespectNullableAnnotations`), so the OpenAPI document states the same
   rules the API applies.
2. **FluentValidation, through [`ValidationFilter<T>`](../../src/FunBooksAndVideos.Api/Validation/ValidationFilter.cs).**
   Values: the id format and length, a total of at least zero with two decimals, 1 to 100 lines, titles of up to 200
   characters. Every error comes back at once, keyed by JSON path (`items[2].title`).
3. **Domain invariants.** The constructors refuse invalid state whoever calls them. That is defence in depth: a 400 if
   it is ever reached.
4. **Stored state.** A reused id with different contents is a 409 (see ADR 3).

The contract types are separate from the domain and mapped by hand: an anti-corruption layer, so the domain can be
refactored without breaking a client.

FluentValidation, rather than .NET 10's attribute-based validation, keeps the rules in code next to each other,
including per-type rules for the polymorphic lines and the decimal precision of the total.

## Consequences

- A client gets one error format for each kind of mistake, and every 400 says where the problem is.
- The serializer's messages name the contract's .NET types, as ASP.NET Core MVC's do by default. Hiding them would
  take custom wording.
- A 256 KB body limit and a rate limit (30 submissions a minute, configurable) bound what one client can cost.
