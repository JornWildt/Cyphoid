

# ANTLR
See https://github.com/antlr/antlr4/blob/master/doc/getting-started.md

Install Java (version 1.7 or higher)
Download antlr-4.13.2-complete.jar (or whatever version) from https://www.antlr.org/download.html Save to your directory for 3rd party Java libraries, say C:\Javalib
Add antlr-4.13.2-complete.jar to CLASSPATH, either:
Permanently: Using System Properties dialog > Environment variables > Create or append to CLASSPATH variable
Temporarily, at command line:

## Run ANTLR Compiler

java org.antlr.v4.Tool -Dlanguage=CSharp -visitor -o Generated Cypher.g4

