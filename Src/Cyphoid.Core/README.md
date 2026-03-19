# C# Cyphoid parser for Cypher query language

This project implements a Cypher parser that can be used in C# to query any sort of graph backend.

Cyphoid expects developers to implement various operators that interact with their own backend.


## Features

- Implements a subset of Cyhper.
- Fast parser implementation with Antlr.
- In-memory implementation of query plans for any backend.

## Example usage


```
// This is your implementation of the backend
IOperatorFactory factory = new OperatorFactory(Graph);

// Query input
var input = "MATCH (n:city) RETURN n.name LIMIT 1";

// Parse query
ICypherParser parser = new CypherAstParser();
var queryNode = parser.ParseQuery(input);

// Print parsed query
var prettyPrint = queryNode.PrettyPrint();

// Build logical query plan
var plan = queryNode.BuildQueryPlan();

// Build executable query plan (using your backend)
var execution = plan.BuildExecutionPlan(factory);

// Prepare query context
var context = new QueryContext(queryNode.RowSize);

// Execute actual query
var result = await execution.ExecuteAsync(context).ToListAsync();
```


## Architecture

```
Source Code
   ↓
ANTLR Lexer/Parser
   ↓
AST Builder (Visitor)
   ↓
Abstract Syntax Tree (AST)
   ↓
Cypher logical plan
   ↓
Cypher executable query plan
   ↓
<Your implementation>
```


# Developer instructions

For developing the Cyphoid library.

## ANTLR
The compiler is built using Antlr. See https://github.com/antlr/antlr4/blob/master/doc/getting-started.md

Antlr is made with Java. There are some Windows tools that make setup easier. See https://github.com/antlr/antlr4-tools

## Run ANTLR Compiler

cd into "Cyphoid.Core/Antlr".

java org.antlr.v4.Tool -Dlanguage=CSharp -visitor -o Generated Cypher.g4


## Manual Antlr installation

Install Java (version 1.7 or higher) - NOT from java.com but rather one of the newer SDKs.

Download antlr-4.13.2-complete.jar (or whatever version) from https://www.antlr.org/download.html Save to your directory for 3rd party Java libraries, say C:\Javalib

Add antlr-4.13.2-complete.jar to CLASSPATH, either:
Permanently: Using System Properties dialog > Environment variables > Create or append to CLASSPATH variable
Temporarily, at command line.

