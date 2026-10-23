grammar Cypher;

query
  : singleQuery (unionClause singleQuery)* EOF
  ;

singleQuery
  : repeatableClause* returnLimitClause
  ;

unionClause
  : UNION (ALL | DISTINCT)?
  ;

repeatableClause
  : matchWhereClause
  | withLimitClause
  | callClause
  ;

callClause
  : CALL procedureName LPAREN (expression (COMMA expression)*)? RPAREN
    YIELD yieldItem (COMMA yieldItem)* whereClause?
  ;

procedureName
  : identifier (DOT identifier)+
  ;

yieldItem
  : identifier (AS identifier)?
  ;

matchWhereClause
  : matchClause whereClause?
  ;

matchClause
  : MATCH pattern
  | OPTIONAL MATCH pattern
  ;

whereClause
  : WHERE expression
  ;

withLimitClause
  : withClause orderingClause? skipClause? limitClause? whereClause?
  ;

withClause
  : WITH projectionList
  ;

returnLimitClause
  : returnClause orderingClause? skipClause? limitClause?
  ;

returnClause
  : RETURN DISTINCT? projectionList
  ;

orderingClause
  : ORDER_BY orderByItem (COMMA orderByItem)*
  ;

orderByItem
  : expression
  | expression DESC
  | expression ASC
  ;

skipClause
  : SKIP_KW (integerLiteral | parameter)
  ;

limitClause
  : LIMIT (integerLiteral | parameter)
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
  | DASH ARROW_RIGHT
  | ARROW_LEFT LBRACK relationshipDetail? RBRACK DASH
  | ARROW_LEFT DASH
  | DASH LBRACK relationshipDetail? RBRACK DASH
  | DASH DASH
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
  : identifier COLON (literal | parameter)
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
  : additiveExpression 
  | comparisonExpression comparisonOperator additiveExpression
  | comparisonExpression IS NOT? NULL
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

additiveExpression
    : multiplicativeExpression
    | additiveExpression PLUS multiplicativeExpression
    | additiveExpression DASH multiplicativeExpression
    ;

multiplicativeExpression
    : unaryExpression 
    | multiplicativeExpression ASTERIX unaryExpression 
    | multiplicativeExpression SLASH unaryExpression 
    | multiplicativeExpression PERCENT unaryExpression 
    ;

unaryExpression
    : PLUS unaryExpression
    | DASH unaryExpression
    | inExpression
    ;

inExpression
  : postfixExpression IN LBRACK expressionList RBRACK
  | postfixExpression
  ;

postfixExpression
  : primaryExpression accessSuffix*
  ;

accessSuffix
  : DOT mapKey
  | LBRACK expression RBRACK
  | LBRACE (mapProjectionItem (COMMA mapProjectionItem)*)? RBRACE
  ;

mapProjectionItem
  : DOT (mapKey | ASTERIX)
  | mapKey COLON expression
  | variable
  ;

mapLiteral
  : LBRACE (mapEntry (COMMA mapEntry)*)? RBRACE
  ;

mapEntry
  : mapKey COLON expression
  ;

mapKey
  : identifier
  | MATCH | CALL | YIELD | UNION | ALL | OPTIONAL | WHERE | RETURN | SKIP_KW | LIMIT
  | AS | AND | OR | NOT | IS | CONTAINS | STARTS | ENDS | IN | WITH
  | TRUE | FALSE | NULL | ASC | DESC | DISTINCT | EXISTS
  ;

// (all operators not here yet)

primaryExpression
  : literal
  | existsSubquery
  | parameter
  | mapLiteral
  | labelPredicate
  | propertyAccess
  | functionCall
  | variable
  | LPAREN expression RPAREN
  ;

parameter
  : PARAMETER
  ;

existsSubquery
  : EXISTS LBRACE existsBranch (unionClause existsBranch)* RBRACE
  ;

existsBranch
  : repeatableClause+ returnLimitClause?
  | pattern whereClause? returnLimitClause?
  | returnLimitClause
  ;

labelPredicate
  : variable COLON identifier
  ;

propertyAccess
  : variable (DOT mapKey)+
  ;

functionCall
  : identifier LPAREN (expression (COMMA expression)*)? RPAREN
  | identifier LPAREN ASTERIX RPAREN
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
  | decimalLiteral
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

decimalLiteral
  : DECIMAL
  ;

stringLiteral
  : STRING
  ;

// =========================
// Lexer rules
// =========================

PARAMETER  : '$' LETTER (LETTER | DIGIT | '_')*;
EXISTS     : [Ee][Xx][Ii][Ss][Tt][Ss];
MATCH      : [Mm][Aa][Tt][Cc][Hh];
CALL       : [Cc][Aa][Ll][Ll];
YIELD      : [Yy][Ii][Ee][Ll][Dd];
UNION      : [Uu][Nn][Ii][Oo][Nn];
ALL        : [Aa][Ll][Ll];
OPTIONAL   : [Oo][Pp][Tt][Ii][Oo][Nn][Aa][Ll];
WHERE      : [Ww][Hh][Ee][Rr][Ee];
RETURN     : [Rr][Ee][Tt][Uu][Rr][Nn];
SKIP_KW       : [Ss][Kk][Ii][Pp];
LIMIT      : [Ll][Ii][Mm][Ii][Tt];
AS         : [Aa][Ss];
AND        : [Aa][Nn][Dd];
OR         : [Oo][Rr];
NOT        : [Nn][Oo][Tt];
IS         : [Ii][Ss];
CONTAINS   : [Cc][Oo][Nn][Tt][Aa][Ii][Nn][Ss];
STARTS     : [Ss][Tt][Aa][Rr][Tt][Ss];
ENDS       : [Ee][Nn][Dd][Ss];
IN         : [Ii][Nn];
WITH       : [Ww][Ii][Tt][Hh];
TRUE       : [Tt][Rr][Uu][Ee];
FALSE      : [Ff][Aa][Ll][Ss][Ee];
NULL       : [Nn][Uu][Ll][Ll];
ORDER_BY   : [Oo][Rr][Dd][Ee][Rr][ \t\r\n]+[Bb][Yy];
ASC        : [Aa][Ss][Cc];
DESC       : [Dd][Ee][Ss][Cc];
DISTINCT   : [Dd][Ii][Ss][Tt][Ii][Nn][Cc][Tt];


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

PLUS       : '+';
ASTERIX    : '*';
SLASH      : '/';
PERCENT    : '%';

EQ         : '=';
NEQ        : '<>';
LTE        : '<=';
GTE        : '>=';
LT         : '<';
GT         : '>';

DECIMAL    : DIGIT+ '.' DIGIT+;
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
