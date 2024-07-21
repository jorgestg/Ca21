//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.13.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from Ca21.g4 by ANTLR 4.13.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace Ca21.Antlr {
using System;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.1")]
[System.CLSCompliant(false)]
public partial class Ca21Lexer : Lexer {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		T__0=1, T__1=2, FuncKeyword=3, LetKeyword=4, MutKeyword=5, Int32Keyword=6, 
		TrueKeyword=7, FalseKeyword=8, Integer=9, Identifier=10, LeftParenthesis=11, 
		RightParenthesis=12, LeftBrace=13, RightBrace=14, Semicolon=15, Star=16, 
		Slash=17, Percentage=18, Plus=19, Minus=20, LessThan=21, LessThanOrEqual=22, 
		GreaterThan=23, GreaterThanOrEqual=24, Equal=25, Whitespace=26;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE"
	};

	public static readonly string[] ruleNames = {
		"T__0", "T__1", "FuncKeyword", "LetKeyword", "MutKeyword", "Int32Keyword", 
		"TrueKeyword", "FalseKeyword", "Integer", "Identifier", "LeftParenthesis", 
		"RightParenthesis", "LeftBrace", "RightBrace", "Semicolon", "Star", "Slash", 
		"Percentage", "Plus", "Minus", "LessThan", "LessThanOrEqual", "GreaterThan", 
		"GreaterThanOrEqual", "Equal", "Whitespace"
	};


	public Ca21Lexer(ICharStream input)
	: this(input, Console.Out, Console.Error) { }

	public Ca21Lexer(ICharStream input, TextWriter output, TextWriter errorOutput)
	: base(input, output, errorOutput)
	{
		Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	private static readonly string[] _LiteralNames = {
		null, "'while'", "'return'", "'func'", "'let'", "'mut'", "'int32'", "'true'", 
		"'false'", null, null, "'('", "')'", "'{'", "'}'", "';'", "'*'", "'/'", 
		"'%'", "'+'", "'-'", "'<'", "'<='", "'>'", "'>='", "'='"
	};
	private static readonly string[] _SymbolicNames = {
		null, null, null, "FuncKeyword", "LetKeyword", "MutKeyword", "Int32Keyword", 
		"TrueKeyword", "FalseKeyword", "Integer", "Identifier", "LeftParenthesis", 
		"RightParenthesis", "LeftBrace", "RightBrace", "Semicolon", "Star", "Slash", 
		"Percentage", "Plus", "Minus", "LessThan", "LessThanOrEqual", "GreaterThan", 
		"GreaterThanOrEqual", "Equal", "Whitespace"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "Ca21.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override string[] ChannelNames { get { return channelNames; } }

	public override string[] ModeNames { get { return modeNames; } }

	public override int[] SerializedAtn { get { return _serializedATN; } }

	static Ca21Lexer() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}
	private static int[] _serializedATN = {
		4,0,26,155,6,-1,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,7,
		6,2,7,7,7,2,8,7,8,2,9,7,9,2,10,7,10,2,11,7,11,2,12,7,12,2,13,7,13,2,14,
		7,14,2,15,7,15,2,16,7,16,2,17,7,17,2,18,7,18,2,19,7,19,2,20,7,20,2,21,
		7,21,2,22,7,22,2,23,7,23,2,24,7,24,2,25,7,25,1,0,1,0,1,0,1,0,1,0,1,0,1,
		1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,2,1,2,1,2,1,2,1,3,1,3,1,3,1,3,1,4,1,4,
		1,4,1,4,1,5,1,5,1,5,1,5,1,5,1,5,1,6,1,6,1,6,1,6,1,6,1,7,1,7,1,7,1,7,1,
		7,1,7,1,8,4,8,98,8,8,11,8,12,8,99,1,8,1,8,4,8,104,8,8,11,8,12,8,105,5,
		8,108,8,8,10,8,12,8,111,9,8,1,9,1,9,5,9,115,8,9,10,9,12,9,118,9,9,1,10,
		1,10,1,11,1,11,1,12,1,12,1,13,1,13,1,14,1,14,1,15,1,15,1,16,1,16,1,17,
		1,17,1,18,1,18,1,19,1,19,1,20,1,20,1,21,1,21,1,21,1,22,1,22,1,23,1,23,
		1,23,1,24,1,24,1,25,1,25,1,25,1,25,0,0,26,1,1,3,2,5,3,7,4,9,5,11,6,13,
		7,15,8,17,9,19,10,21,11,23,12,25,13,27,14,29,15,31,16,33,17,35,18,37,19,
		39,20,41,21,43,22,45,23,47,24,49,25,51,26,1,0,4,1,0,48,57,3,0,65,90,95,
		95,97,122,4,0,48,57,65,90,95,95,97,122,3,0,9,10,13,13,32,32,158,0,1,1,
		0,0,0,0,3,1,0,0,0,0,5,1,0,0,0,0,7,1,0,0,0,0,9,1,0,0,0,0,11,1,0,0,0,0,13,
		1,0,0,0,0,15,1,0,0,0,0,17,1,0,0,0,0,19,1,0,0,0,0,21,1,0,0,0,0,23,1,0,0,
		0,0,25,1,0,0,0,0,27,1,0,0,0,0,29,1,0,0,0,0,31,1,0,0,0,0,33,1,0,0,0,0,35,
		1,0,0,0,0,37,1,0,0,0,0,39,1,0,0,0,0,41,1,0,0,0,0,43,1,0,0,0,0,45,1,0,0,
		0,0,47,1,0,0,0,0,49,1,0,0,0,0,51,1,0,0,0,1,53,1,0,0,0,3,59,1,0,0,0,5,66,
		1,0,0,0,7,71,1,0,0,0,9,75,1,0,0,0,11,79,1,0,0,0,13,85,1,0,0,0,15,90,1,
		0,0,0,17,97,1,0,0,0,19,112,1,0,0,0,21,119,1,0,0,0,23,121,1,0,0,0,25,123,
		1,0,0,0,27,125,1,0,0,0,29,127,1,0,0,0,31,129,1,0,0,0,33,131,1,0,0,0,35,
		133,1,0,0,0,37,135,1,0,0,0,39,137,1,0,0,0,41,139,1,0,0,0,43,141,1,0,0,
		0,45,144,1,0,0,0,47,146,1,0,0,0,49,149,1,0,0,0,51,151,1,0,0,0,53,54,5,
		119,0,0,54,55,5,104,0,0,55,56,5,105,0,0,56,57,5,108,0,0,57,58,5,101,0,
		0,58,2,1,0,0,0,59,60,5,114,0,0,60,61,5,101,0,0,61,62,5,116,0,0,62,63,5,
		117,0,0,63,64,5,114,0,0,64,65,5,110,0,0,65,4,1,0,0,0,66,67,5,102,0,0,67,
		68,5,117,0,0,68,69,5,110,0,0,69,70,5,99,0,0,70,6,1,0,0,0,71,72,5,108,0,
		0,72,73,5,101,0,0,73,74,5,116,0,0,74,8,1,0,0,0,75,76,5,109,0,0,76,77,5,
		117,0,0,77,78,5,116,0,0,78,10,1,0,0,0,79,80,5,105,0,0,80,81,5,110,0,0,
		81,82,5,116,0,0,82,83,5,51,0,0,83,84,5,50,0,0,84,12,1,0,0,0,85,86,5,116,
		0,0,86,87,5,114,0,0,87,88,5,117,0,0,88,89,5,101,0,0,89,14,1,0,0,0,90,91,
		5,102,0,0,91,92,5,97,0,0,92,93,5,108,0,0,93,94,5,115,0,0,94,95,5,101,0,
		0,95,16,1,0,0,0,96,98,7,0,0,0,97,96,1,0,0,0,98,99,1,0,0,0,99,97,1,0,0,
		0,99,100,1,0,0,0,100,109,1,0,0,0,101,103,5,95,0,0,102,104,7,0,0,0,103,
		102,1,0,0,0,104,105,1,0,0,0,105,103,1,0,0,0,105,106,1,0,0,0,106,108,1,
		0,0,0,107,101,1,0,0,0,108,111,1,0,0,0,109,107,1,0,0,0,109,110,1,0,0,0,
		110,18,1,0,0,0,111,109,1,0,0,0,112,116,7,1,0,0,113,115,7,2,0,0,114,113,
		1,0,0,0,115,118,1,0,0,0,116,114,1,0,0,0,116,117,1,0,0,0,117,20,1,0,0,0,
		118,116,1,0,0,0,119,120,5,40,0,0,120,22,1,0,0,0,121,122,5,41,0,0,122,24,
		1,0,0,0,123,124,5,123,0,0,124,26,1,0,0,0,125,126,5,125,0,0,126,28,1,0,
		0,0,127,128,5,59,0,0,128,30,1,0,0,0,129,130,5,42,0,0,130,32,1,0,0,0,131,
		132,5,47,0,0,132,34,1,0,0,0,133,134,5,37,0,0,134,36,1,0,0,0,135,136,5,
		43,0,0,136,38,1,0,0,0,137,138,5,45,0,0,138,40,1,0,0,0,139,140,5,60,0,0,
		140,42,1,0,0,0,141,142,5,60,0,0,142,143,5,61,0,0,143,44,1,0,0,0,144,145,
		5,62,0,0,145,46,1,0,0,0,146,147,5,62,0,0,147,148,5,61,0,0,148,48,1,0,0,
		0,149,150,5,61,0,0,150,50,1,0,0,0,151,152,7,3,0,0,152,153,1,0,0,0,153,
		154,6,25,0,0,154,52,1,0,0,0,5,0,99,105,109,116,1,6,0,0
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace Ca21.Antlr
