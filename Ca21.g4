grammar Ca21;

// Parser
compilationUnit
    : UseDirectives+=useDirectives* Definitions+=topLevelDefinition+ EOF
    ;

useDirectives
    : 'use' Path=String Alias=Identifier? ';'
    ;

topLevelDefinition
    : Enumeration=enumerationDefinition #TopLevelEnumerationDefinition
    | Function=functionDefinition #TopLevelFunctionDefinition
    | Structure=structureDefinition #TopLevelStructureDefinition
    ;

enumerationDefinition
    : 'enum' Name=Identifier '{'
        (Cases+=enumerationCaseDefinition (',' Cases+=enumerationCaseDefinition)*) 
      '}'
    ;

enumerationCaseDefinition
    : Name=Identifier ('(' Value=typeName ')')?
    ;

functionSignature
    : 'func' Name=Identifier '(' ParameterList=parameterList? ')' ReturnType=typeName?
    ;

parameterList
    : Parameters+=parameterDefinition (',' Parameters+=parameterDefinition)*
    ;

parameterDefinition
    : MutModifier='mut'? Name=Identifier Type=typeName
    ;

structureDefinition
    : 'struct' Name=Identifier '{' (Fields+=fieldDefinition (',' Fields+=fieldDefinition)*) '}'
    ;

fieldDefinition
    : Name=Identifier Type=typeName
    ;

functionDefinition
    : ExportModifier='export'? ExternModifier=externModifier? Signature=functionSignature (EndOfDeclaration=';' | Body=block)
    ;

externModifier
    : 'extern' ExternName=String?
    ;

typeName
    : TypeKeyword=typeKeyword #KeywordTypeName
    | Name=Identifier         #SimpleNameTypeName
    | '&' InnerType=typeName  #ReferenceTypeName
    | '[]' InnerType=typeName #SliceTypeName
    ;

typeKeyword
    : Keyword=Int32Keyword
    | Keyword=Int64Keyword
    | Keyword=StringKeyword
    | Keyword=BoolKeyword
    | Keyword=USizeKeyword
    ;

block
    : '{' Statements+=statement* '}'
    ;

statement
    : Declaration=localDeclaration #LocalDeclarationStatement
    | 'if' Condition=expression Body=block ('else' ElseClause=block)? #IfStatement
    | 'while' Condition=expression Body=block #WhileStatement
    | 'return' Expression=expressionOrBlock ';' #ReturnStatement
    | Block=block #BlockStatement
    | Expression=expression ';' #ExpressionStatement
    ;

localDeclaration
    : 'let' MutModifier='mut'? Name=Identifier '=' Expression=expressionOrBlock ';'
    ;

expressionOrBlock
    : '{' Statements+=statement* Tail=expression? '}' #BlockExpression
    | Expression=expression #NonBlockExpression
    ;

expression
    : Literal=literal                                                     #LiteralExpression
    | Name=Identifier                                                     #NameExpression
    | Callee=expression '(' ArgumentList=argumentList? ')'                #CallExpression
    | Left=expression '.' Right=Identifier                                #AccessExpression
    | Structure=typeName '{'
        (Fields+=fieldInitializer (',' Fields+=fieldInitializer)*)
      '}'                                                                 #StructureLiteralExpression
    | Operator=('!'|'-') Operand=expression                               #UnaryExpression
    | Left=expression Operator=('*' | '/' | '%') Right=expression         #FactorExpression
    | Left=expression Operator=('+' | '-') Right=expression               #TermExpression
    | Left=expression Operator=('<' | '<=' | '>' | '>=') Right=expression #ComparisonExpression
    | Left=expression Operator=('==' | '!=') Right=expression             #EqualityExpression
    | Left=expression Operator='&&' Right=expression                      #LogicalAndExpression
    | Left=expression Operator='||' Right=expression                      #LogicalOrExpression
    | Assignee=expression '=' Expression=expression                       #AssignmentExpression
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
EnumKeyword: 'enum';
ExternKeyword: 'extern';
ExportKeyword: 'export';
FuncKeyword: 'func';
ElseKeyword: 'else';
LetKeyword: 'let';
MutKeyword: 'mut';
IfKeyword: 'if';
Int32Keyword: 'int32';
Int64Keyword: 'int64';
BoolKeyword: 'bool';
StringKeyword: 'string';
StructKeyword: 'struct';
TrueKeyword: 'true';
FalseKeyword: 'false';
WhileKeyword: 'while';
ReturnKeyword: 'return';
USizeKeyword: 'usize';
UseKeyword: 'use';

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
Bang: '!';
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
DoubleEqual: '==';
BangEqual: '!=';
Ampersand: '&';
DoubleAmpersand: '&&';
DoublePipe: '||';

// Skip
Whitespace: [ \r\n\t] -> skip;