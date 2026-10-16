# Map values

Maps are immutable, unordered collections of string keys and MixedValue values.
Keys are ordinal and case-sensitive. Values can include nested maps, nodes,
scalars, and null. Lists and paths are not introduced by this feature.

## Constructing and reading maps

```cypher
WITH 20 AS iterations
RETURN {iterations: iterations, dampingFactor: 0.85, settings: {enabled: true}} AS config
```

An empty map is `{}`. Values can be expressions; duplicate literal keys are
rejected during parsing. Keys use unquoted symbolic names; keyword keys such as
`limit` are supported. String-literal keys and backtick-escaped keys are not
supported by this subset.

Use dot access for static keys and brackets for expression-based keys:

```cypher
WITH {settings: {enabled: true}, iterations: 20} AS config, 'iterations' AS key
RETURN config.settings.enabled AS enabled, config[key] AS iterations
```

Both forms support nested access, including `config['settings'].enabled`.
A missing key, null target, or null key returns null. A non-string key or a
non-map/non-node target raises a located runtime type error. Bracket access also
works for node properties. The existing map-shaped syntax in MATCH patterns
remains a property filter, separate from runtime map construction.

## Map projections

Select properties from nodes or maps, copy all properties, add values, or override
existing ones:

```cypher
MATCH (n:Person)
RETURN n {.name, .age, nextAge: n.age + 1} AS person
```

```cypher
WITH {iterations: 20, enabled: true} AS config, 0.85 AS dampingFactor
RETURN config {.*, iterations: 30, dampingFactor} AS updated
```

`.name` selects a property, `.*` copies all properties, and `dampingFactor` copies
an in-scope variable using its name as the key. Explicit entries override copied
properties regardless of where `.*` appears. Later explicit entries with the
same key override earlier ones. A null source produces null; a scalar source
raises a type error. The original map or node is never modified.

Aggregate results must be projected in WITH before embedding them in a map:

```cypher
MATCH (n:Person)
WITH count(*) AS total
RETURN {total: total} AS summary
```

## Equality, grouping, and ordering

Map equality compares keys and values recursively, independently of insertion
order. Query equality involving null map entries can produce null; a definite
key/value mismatch produces false. `<>` and IN preserve these unknown results.
Non-null scalar entries follow Cyphoid's existing scalar equality rules,
including distinct integer and double types.

Grouping, DISTINCT, and UNION use structural equality and consistent hashes,
including nested maps, null entries, and backend-defined node identities. Unlike
query equality, structural equality treats matching null entries as equal.

Ordering complete maps is not supported; order by a selected scalar field such
as `ORDER BY config.iterations`. Scalar conversion functions reject maps.

## Host API

```csharp
var config = MixedValue.Map(new Dictionary<string, MixedValue>
{
    ["iterations"] = MixedValue.Int(20),
    ["dampingFactor"] = MixedValue.Double(0.85)
});

IReadOnlyDictionary<string, MixedValue> entries = config.AsMap();
long iterations = entries["iterations"].AsInt();
```

`MixedValue.ValueType.Map` identifies the type. `Map(...)` takes a defensive
snapshot; mutating the original dictionary cannot change the value.
`AsMap()` returns a read-only dictionary and `TryGetMap(out var map)` supports
conditional access. `Map(null)` produces null.

`FromObject` accepts string-keyed MixedValue dictionaries or object dictionaries,
including nested dictionaries. `AsObject` exports maps as read-only dictionaries
whose values are recursively converted to host objects. `Match` has an overload
with a final map callback; the original overload remains available for callers
that do not handle map values.

## Procedure configuration

Declare a map parameter using the existing procedure signature system:

```csharp
public IReadOnlyList<ProcedureField> Parameters { get; } =
    new ProcedureField[] { new("config", MixedValue.ValueType.Map) };
```

Read configuration inside `ExecuteAsync`:

```csharp
var config = arguments[0].AsMap();
var iterations = config.TryGetValue("iterations", out var value)
    ? value.AsInt()
    : 20L;
```

The host-installed procedure can then be called with a literal or a map variable:

```cypher
CALL analytics.pageRank({iterations: 20, dampingFactor: 0.85})
YIELD node, score
RETURN node.name AS name, score
ORDER BY score DESC
```

This example assumes the host has installed `analytics.pageRank`; Cyphoid does
not include a PageRank implementation. Procedures can also declare Map output
fields and return `MixedValue.Map(...)` in host rows. YIELD aliases, subsequent
map access, schema validation, cancellation, and host metadata merging work as
for other procedure values. The procedure validates required keys, defaults,
and individual configuration value types; Cyphoid validates the outer Map type
and declared nullability.
