grammar Ca21;

// Parser
compilationUnit
    : Functions+=functionDefinition+ EOF
    ;

functionDefinition
    : Signature=functionSignature Body=block
    ;

functionSignature
    : 'func' Name=Identifier '(' ')' ReturnType=typeReference
    ;

typeReference
    : NativeType=typeKeyword #NativeTypeReference
    ;

typeKeyword
    : Keyword='int32'
    ;

block
    : '{' Statements+=statement* '}'
    ;

statement
    : Declaration=localDeclaration #LocalDeclarationStatement
    | 'while' Condition=expression Body=block #WhileStatement
    | 'return' Value=expressionOrBlock ';' #ReturnStatement
    | Block=block #BlockStatement
    | Expression=expression ';' #ExpressionStatement
    ;

localDeclaration
    : 'let' MutModifier='mut'? Name=Identifier '=' Value=expressionOrBlock ';'
    ;

expressionOrBlock
    : '{' Statements+=statement* Tail=expression? '}' #BlockExpression
    | Expression=expression #NonBlockExpression
    ;

expression
    : Literal=literal #LiteralExpression
    | Name=Identifier #NameExpression
    | Callee=expression '('')' #CallExpression
    | Left=expression Operator=('*' | '/' | '%') Right=expression #FactorExpression
    | Left=expression Operator=('+' | '-') Right=expression #TermExpression
    | Left=expression Operator=('<' | '<=' | '>' | '>=') Right=expression #ComparisonExpression
    | Assignee=expression '=' Value=expression #AssignmentExpression
    ;

literal
    : Value=Integer #IntegerLiteral
    | Value=TrueKeyword #TrueLiteral
    | Value=FalseKeyword #FalseLiteral
    ;

// Lexer
// Keywords
FuncKeyword: 'func';
LetKeyword: 'let';
MutKeyword: 'mut';
Int32Keyword: 'int32';
TrueKeyword: 'true';
FalseKeyword: 'false';
WhileKeyword: 'while';
ReturnKeyword: 'return';

// Literals
Integer: [0-9]+ ('_' [0-9]+)*;
Identifier: [a-zA-Z_][a-zA-Z_0-9]*;

// Symbols
LeftParenthesis: '(';
RightParenthesis: ')';
LeftBrace: '{';
RightBrace: '}';
Semicolon: ';';

// Operators
Star: '*';
Slash: '/';
Percentage: '%';
Plus: '+';
Minus: '-';
LessThan: '<';
LessThanOrEqual: '<=';
GreaterThan: '>';
GreaterThanOrEqual: '>=';
Equal: '=';

// Skip
Whitespace: [ \r\n\t] -> skip;