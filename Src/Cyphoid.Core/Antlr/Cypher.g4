grammar Cypher;

query
  : matchClause whereClause? returnClause limitClause? EOF
  ;

matchClause
  : MATCH pattern
  ;

whereClause
  : WHERE expression
  ;

returnClause
  : RETURN returnItem (COMMA returnItem)*
  ;

limitClause
  : LIMIT integerLiteral
  ;

returnItem
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

nodeLabel
  : COLON identifier
  ;

relationshipTypes
  : COLON identifier
  ;

// -------------------------
// Expressions
// -------------------------

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
    | primaryExpression
    ;

// (operators not here yet)

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
  ;

propertyMap
  : LBRACE propertyEntry (COMMA propertyEntry)* RBRACE
  ;

propertyEntry
  : identifier COLON expression
  ;

variable
  : identifier
  ;

// -------------------------
// Literals
// -------------------------

literal
  : stringLiteral
  | integerLiteral
  ;

stringLiteral
  : STRING
  ;

integerLiteral
  : INTEGER
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

identifier
  : IDENTIFIER
  ;

WS
  : [ \t\r\n]+ -> skip
  ;