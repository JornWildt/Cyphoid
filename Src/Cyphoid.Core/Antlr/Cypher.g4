grammar Cypher;

query
  : repeatableClause* returnLimitClause EOF
  ;

repeatableClause
  : matchWhereClause
  | withClause
  ;

matchWhereClause
  : matchClause whereClause?
  ;

matchClause
  : MATCH pattern
  ;

whereClause
  : WHERE expression
  ;

withClause
  : WITH projectionList
  ;

returnLimitClause
  : returnClause orderingClause? limitClause?
  ;

returnClause
  : RETURN projectionList
  ;

orderingClause
  : ORDER BY orderByItem (COMMA orderByItem)*
  ;

orderByItem
  : expression
  | expression DESC
  | expression ASC
  ;

limitClause
  : LIMIT integerLiteral
  ;

projectionList
  : projectionItem (COMMA projectionItem)*
  ;

projectionItem
  : expression (AS identifier)?
  ;

pattern
  : patternPart (COMMA patternPart)*
  ;

patternPart
  : nodePattern patternChain*
  ;

patternChain
  : relationshipPattern nodePattern
  ;

nodePattern
  : LPAREN variable? nodeLabel? propertyMap? RPAREN
  ;

relationshipPattern
  : DASH LBRACK relationshipDetail? RBRACK ARROW_RIGHT
  | ARROW_LEFT LBRACK relationshipDetail? RBRACK DASH
  | DASH LBRACK relationshipDetail? RBRACK DASH
  ;

relationshipDetail
  : variable relationshipTypes? propertyMap?
  | relationshipTypes propertyMap?
  | propertyMap
  ;

relationshipTypes
  : COLON identifier
  ;

nodeLabel
  : COLON identifier
  ;

propertyMap
  : LBRACE propertyEntry (COMMA propertyEntry)* RBRACE
  ;

propertyEntry
  : identifier COLON literal
  ;

// -------------------------
// Expressions
// -------------------------

expressionList
  : expression (COMMA expression)*
  | expression?
  ;

expression
  : orExpression
  ;

orExpression
  : andExpression (OR andExpression)*
  ;

andExpression
  : notExpression (AND notExpression)*
  ;

notExpression
  : NOT notExpression
  | comparisonExpression
  ;

comparisonExpression
  : inExpression comparisonOperator inExpression
  | inExpression
  ;

comparisonOperator
  : EQ
  | NEQ
  | LT
  | LTE
  | GT
  | GTE
  | CONTAINS
  | STARTS WITH
  | ENDS WITH
  ;

inExpression
  : primaryExpression IN LBRACK expressionList RBRACK
  | primaryExpression
  ;

// (all operators not here yet)

primaryExpression
  : literal
  | propertyAccess
  | functionCall
  | variable
  | LPAREN expression RPAREN
  ;

propertyAccess
  : variable (DOT identifier)+
  ;

functionCall
  : identifier LPAREN (expression (COMMA expression)*)? RPAREN
  | COUNT LPAREN ASTERIX RPAREN
  ;

variable
  : identifier
  ;

identifier
  : IDENTIFIER
  ;


// -------------------------
// Literals
// -------------------------

literal
  : boolLiteral
  | integerLiteral
  | stringLiteral
  | NULL
  ;

boolLiteral
  : TRUE
  | FALSE
  ;

integerLiteral
  : INTEGER
  ;

stringLiteral
  : STRING
  ;

// =========================
// Lexer rules
// =========================

MATCH      : [Mm][Aa][Tt][Cc][Hh];
WHERE      : [Ww][Hh][Ee][Rr][Ee];
RETURN     : [Rr][Ee][Tt][Uu][Rr][Nn];
LIMIT      : [Ll][Ii][Mm][Ii][Tt];
AS         : [Aa][Ss];
AND        : [Aa][Nn][Dd];
OR         : [Oo][Rr];
NOT        : [Nn][Oo][Tt];
CONTAINS   : [Cc][Oo][Nn][Tt][Aa][Ii][Nn][Ss];
STARTS     : [Ss][Tt][Aa][Rr][Tt][Ss];
ENDS       : [Ee][Nn][Dd][Ss];
IN         : [Ii][Nn];
WITH       : [Ww][Ii][Tt][Hh];
TRUE       : [Tt][Rr][Uu][Ee];
FALSE      : [Ff][Aa][Ll][Ss][Ee];
NULL       : [Nn][Uu][Ll][Ll];
ORDER      : [Oo][Rr][Dd][Ee][Rr];
BY         : [Bb][Yy];
ASC        : [Aa][Ss][Cc];
DESC       : [Dd][Ee][Ss][Cc];
COUNT      : [Cc][Oo][Uu][Nn][Tt];


ARROW_RIGHT: '->';
ARROW_LEFT : '<-';
DASH       : '-';

LPAREN     : '(';
RPAREN     : ')';
LBRACK     : '[';
RBRACK     : ']';
LBRACE     : '{';
RBRACE     : '}';
COMMA      : ',';
COLON      : ':';
DOT        : '.';

ASTERIX    : '*';

EQ         : '=';
NEQ        : '<>';
LTE        : '<=';
GTE        : '>=';
LT         : '<';
GT         : '>';

INTEGER    : DIGIT+;

STRING
  : '\'' ( ~['\\] | '\\' . )* '\''
  | '"'  ( ~["\\] | '\\' . )* '"'
  ;

IDENTIFIER
  : LETTER (LETTER | DIGIT | '_')*
  ;

fragment LETTER : [a-zA-Z_];
fragment DIGIT  : [0-9];

WS
  : [ \t\r\n]+ -> skip
  ;