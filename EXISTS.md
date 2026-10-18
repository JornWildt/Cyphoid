# EXISTS subqueries

EXISTS evaluates a read-only subquery and returns true when it produces at least
one row, otherwise false. NOT EXISTS reverses the result. Inner matches never
multiply outer rows:

```cypher
MATCH (person:Person)
WHERE EXISTS {
  MATCH (person)-[:KNOWS]->(friend:Person)
  WHERE friend.age >= $minimumAge
}
RETURN person.name
```

Outer variables are automatically available in the subquery. A reference to
`person` above constrains the match to that same node. Variables introduced
inside the subquery, such as friend, remain local. Independent subqueries can
reuse local names. A null correlated node has no pattern matches.

## Supported bodies

The body supports the engine's existing read clauses: MATCH, OPTIONAL MATCH,
WHERE, WITH, CALL/YIELD, optional RETURN, ORDER BY, LIMIT, and UNION/UNION ALL.
RETURN can be omitted, including in UNION branches. If one UNION branch uses
RETURN, all branches must use it. Mixed UNION/UNION ALL chains are rejected.
The MATCH keyword can be omitted for a single pattern with an optional WHERE.
Empty bodies are not supported.

```cypher
MATCH (person:Person)
WHERE EXISTS {
  MATCH (person)-[:OWNS]->(pet)
  UNION
  MATCH (person)-[:KNOWS]->(friend)
}
RETURN person.name
```

WITH may introduce local variables. Outer variables remain visible throughout
the subquery, even if WITH omits them. WITH cannot alias another value to an
outer variable's name. Identity projections such as WITH person are allowed.

```cypher
MATCH (person:Person)
WHERE EXISTS {
  WITH 30 AS minimumAge
  MATCH (person)-[:KNOWS]->(friend)
  WHERE friend.age >= minimumAge
}
RETURN person.name
```

Existence depends on the presence of a row, not its values. `EXISTS { RETURN null }`
is true, while `EXISTS { RETURN 1 LIMIT 0 }` is false. A global count over no matches
still produces a row, so `EXISTS { MATCH (n:Missing) RETURN count(*) }` is true.
OPTIONAL MATCH can also produce a fallback row and therefore a true result.

## Expressions and nesting

EXISTS is a boolean expression and can appear in WHERE, WITH/RETURN projections,
map values, grouping keys, aggregate arguments, ORDER BY, and procedure arguments.
Subqueries may contain nested EXISTS expressions. For example:

```cypher
MATCH (person:Person)
RETURN person.name,
       EXISTS {
         MATCH (person)-[:KNOWS]->(friend)
         WHERE NOT EXISTS { MATCH (friend)-[:OWNS]->(pet) }
       } AS hasFriendWithoutPets
```

ORDER BY supports boolean results with false before true in ascending order.
Temporary values used for async evaluation are removed from result schemas.

## Execution, parameters, and cancellation

Subqueries use the same backend factory, execution context, parameter snapshot,
and cancellation token as the outer query. Host contexts are not wrapped or
replaced. Required parameters inside subqueries are validated before execution,
including references in a branch that lazy evaluation might otherwise skip.

Evaluation stops as soon as a subquery produces its first row, then disposes the
enumerator. AND, OR, coalesce, and IN retain their existing lazy evaluation:
an unnecessary EXISTS operand does not execute. Operations that inherently need
all input rows, such as sorting or aggregation, can still buffer or consume all
their input before producing the first row.

EXISTS evaluation uses asynchronous enumeration throughout; it does not block
on async backend calls. Cancellation propagates into nested queries and disposes
active enumerators. Query errors retain the location of the failing expression.
Mutable state is local to each evaluation, so a reused execution plan does not
share subquery bindings across concurrent executions.

Only a boolean leaves the subquery. Local values, schemas, and result metadata
are not added to the outer row. The host's existing copy and row-factory hooks
preserve outer metadata when async expression values are materialized internally.

Procedure calls follow the host's installed procedure contract. EXISTS does not
add write clauses or rollback, and early termination means a procedure may not
be fully enumerated. Use read-only procedures for existence checks.
