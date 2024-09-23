grammar Ca21;

// Parser
compilationUnit
    : Definitions+=topLevelDefinition+ EOF
    ;

topLevelDefinition
    : Function=functionDefinition #TopLevelFunctionDefinition
    | Structure=structureDefinition #TopLevelStructureDefinition
    ;

structureDefinition
    : 'struct' Name=Identifier '{' (Fields+=fieldDefinition (',' Fields+=fieldDefinition)*) '}'
    ;

fieldDefinition
    : Name=Identifier Type=typeReference
    ;

functionDefinition
    : ExportModifier='export'? ExternModifier=externModifier? Signature=functionSignature (EndOfDeclaration=';' | Body=block)
    ;

externModifier
    : 'extern' ExternName=String?
    ;

functionSignature
    : 'func' Name=Identifier '(' ParameterList=parameterList? ')' ReturnType=typeReference?
    ;

parameterList
    : Parameters+=parameterDefinition (',' Parameters+=parameterDefinition)*
    ;

parameterDefinition
    : MutModifier='mut'? Name=Identifier Type=typeReference
    ;

typeReference
    : TypeKeyword=typeKeyword #KeywordTypeReference
    | Name=Identifier #SimpleNameTypeReference
    ;

typeKeyword
    : Keyword=Int32Keyword
    | Keyword=StringKeyword
    | Keyword=BoolKeyword
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
    | Callee=expression '(' ArgumentList=argumentList? ')' #CallExpression
    | Left=expression '.' Right=Identifier #AccessExpression
    | Structure=typeReference '{' (Fields+=fieldInitializer (',' Fields+=fieldInitializer)) '}' #StructureLiteralExpression
    | Left=expression Operator=('*' | '/' | '%') Right=expression #FactorExpression
    | Left=expression Operator=('+' | '-') Right=expression #TermExpression
    | Left=expression Operator=('<' | '<=' | '>' | '>=') Right=expression #ComparisonExpression
    | Assignee=expression '=' Value=expression #AssignmentExpression
    ;

fieldInitializer
    : Name=Identifier '=' Value=expressionOrBlock #AssignmentFieldInitializer
    | Name=Identifier #NameOnlyFieldInitializer
    ;

argumentList
    : Arguments+=expression (',' Arguments+=expression)*
    ;

literal
    : Value=Integer #IntegerLiteral
    | Value=TrueKeyword #TrueLiteral
    | Value=FalseKeyword #FalseLiteral
    | Value=String #StringLiteral
    ;

// Lexer
// Keywords
ExternKeyword: 'extern';
ExportKeyword: 'export';
FuncKeyword: 'func';
LetKeyword: 'let';
MutKeyword: 'mut';
Int32Keyword: 'int32';
BoolKeyword: 'bool';
StringKeyword: 'string';
StructKeyword: 'struct';
TrueKeyword: 'true';
FalseKeyword: 'false';
WhileKeyword: 'while';
ReturnKeyword: 'return';

// Literals
String: '"' .*? '"';
Integer: [0-9]+ ('_' [0-9]+)*;
Identifier: [a-zA-Z_][a-zA-Z_0-9]*;

// Symbols
Semicolon: ';';
Comma: ',';
LeftParenthesis: '(';
RightParenthesis: ')';
LeftBrace: '{';
RightBrace: '}';
Dot: '.';

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