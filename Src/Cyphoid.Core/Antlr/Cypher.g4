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
  : ORDER_BY orderByItem (COMMA orderByItem)*
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
  : additiveExpression comparisonOperator additiveExpression 
  | additiveExpression 
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
ORDER_BY   : [Oo][Rr][Dd][Ee][Rr][ \t\r\n]+[Bb][Yy];
ASC        : [Aa][Ss][Cc];
DESC       : [Dd][Ee][Ss][Cc];


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