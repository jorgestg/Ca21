//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.13.2
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from Ca21.g4 by ANTLR 4.13.2

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

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.2")]
[System.CLSCompliant(false)]
public partial class Ca21Lexer : Lexer {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		ExternKeyword=1, ExportKeyword=2, FuncKeyword=3, LetKeyword=4, MutKeyword=5, 
		Int32Keyword=6, Int64Keyword=7, BoolKeyword=8, StringKeyword=9, StructKeyword=10, 
		TrueKeyword=11, FalseKeyword=12, WhileKeyword=13, ReturnKeyword=14, String=15, 
		Integer=16, Identifier=17, Semicolon=18, Comma=19, LeftParenthesis=20, 
		RightParenthesis=21, LeftBrace=22, RightBrace=23, Dot=24, Star=25, Slash=26, 
		Percentage=27, Plus=28, Minus=29, LessThan=30, LessThanOrEqual=31, GreaterThan=32, 
		GreaterThanOrEqual=33, Equal=34, Whitespace=35;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE"
	};

	public static readonly string[] ruleNames = {
		"ExternKeyword", "ExportKeyword", "FuncKeyword", "LetKeyword", "MutKeyword", 
		"Int32Keyword", "Int64Keyword", "BoolKeyword", "StringKeyword", "StructKeyword", 
		"TrueKeyword", "FalseKeyword", "WhileKeyword", "ReturnKeyword", "String", 
		"Integer", "Identifier", "Semicolon", "Comma", "LeftParenthesis", "RightParenthesis", 
		"LeftBrace", "RightBrace", "Dot", "Star", "Slash", "Percentage", "Plus", 
		"Minus", "LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual", 
		"Equal", "Whitespace"
	};


	public Ca21Lexer(ICharStream input)
	: this(input, Console.Out, Console.Error) { }

	public Ca21Lexer(ICharStream input, TextWriter output, TextWriter errorOutput)
	: base(input, output, errorOutput)
	{
		Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	private static readonly string[] _LiteralNames = {
		null, "'extern'", "'export'", "'func'", "'let'", "'mut'", "'int32'", "'int64'", 
		"'bool'", "'string'", "'struct'", "'true'", "'false'", "'while'", "'return'", 
		null, null, null, "';'", "','", "'('", "')'", "'{'", "'}'", "'.'", "'*'", 
		"'/'", "'%'", "'+'", "'-'", "'<'", "'<='", "'>'", "'>='", "'='"
	};
	private static readonly string[] _SymbolicNames = {
		null, "ExternKeyword", "ExportKeyword", "FuncKeyword", "LetKeyword", "MutKeyword", 
		"Int32Keyword", "Int64Keyword", "BoolKeyword", "StringKeyword", "StructKeyword", 
		"TrueKeyword", "FalseKeyword", "WhileKeyword", "ReturnKeyword", "String", 
		"Integer", "Identifier", "Semicolon", "Comma", "LeftParenthesis", "RightParenthesis", 
		"LeftBrace", "RightBrace", "Dot", "Star", "Slash", "Percentage", "Plus", 
		"Minus", "LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual", 
		"Equal", "Whitespace"
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
		4,0,35,225,6,-1,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,7,
		6,2,7,7,7,2,8,7,8,2,9,7,9,2,10,7,10,2,11,7,11,2,12,7,12,2,13,7,13,2,14,
		7,14,2,15,7,15,2,16,7,16,2,17,7,17,2,18,7,18,2,19,7,19,2,20,7,20,2,21,
		7,21,2,22,7,22,2,23,7,23,2,24,7,24,2,25,7,25,2,26,7,26,2,27,7,27,2,28,
		7,28,2,29,7,29,2,30,7,30,2,31,7,31,2,32,7,32,2,33,7,33,2,34,7,34,1,0,1,
		0,1,0,1,0,1,0,1,0,1,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,1,2,1,2,1,2,1,2,
		1,3,1,3,1,3,1,3,1,4,1,4,1,4,1,4,1,5,1,5,1,5,1,5,1,5,1,5,1,6,1,6,1,6,1,
		6,1,6,1,6,1,7,1,7,1,7,1,7,1,7,1,8,1,8,1,8,1,8,1,8,1,8,1,8,1,9,1,9,1,9,
		1,9,1,9,1,9,1,9,1,10,1,10,1,10,1,10,1,10,1,11,1,11,1,11,1,11,1,11,1,11,
		1,12,1,12,1,12,1,12,1,12,1,12,1,13,1,13,1,13,1,13,1,13,1,13,1,13,1,14,
		1,14,5,14,156,8,14,10,14,12,14,159,9,14,1,14,1,14,1,15,4,15,164,8,15,11,
		15,12,15,165,1,15,1,15,4,15,170,8,15,11,15,12,15,171,5,15,174,8,15,10,
		15,12,15,177,9,15,1,16,1,16,5,16,181,8,16,10,16,12,16,184,9,16,1,17,1,
		17,1,18,1,18,1,19,1,19,1,20,1,20,1,21,1,21,1,22,1,22,1,23,1,23,1,24,1,
		24,1,25,1,25,1,26,1,26,1,27,1,27,1,28,1,28,1,29,1,29,1,30,1,30,1,30,1,
		31,1,31,1,32,1,32,1,32,1,33,1,33,1,34,1,34,1,34,1,34,1,157,0,35,1,1,3,
		2,5,3,7,4,9,5,11,6,13,7,15,8,17,9,19,10,21,11,23,12,25,13,27,14,29,15,
		31,16,33,17,35,18,37,19,39,20,41,21,43,22,45,23,47,24,49,25,51,26,53,27,
		55,28,57,29,59,30,61,31,63,32,65,33,67,34,69,35,1,0,4,1,0,48,57,3,0,65,
		90,95,95,97,122,4,0,48,57,65,90,95,95,97,122,3,0,9,10,13,13,32,32,229,
		0,1,1,0,0,0,0,3,1,0,0,0,0,5,1,0,0,0,0,7,1,0,0,0,0,9,1,0,0,0,0,11,1,0,0,
		0,0,13,1,0,0,0,0,15,1,0,0,0,0,17,1,0,0,0,0,19,1,0,0,0,0,21,1,0,0,0,0,23,
		1,0,0,0,0,25,1,0,0,0,0,27,1,0,0,0,0,29,1,0,0,0,0,31,1,0,0,0,0,33,1,0,0,
		0,0,35,1,0,0,0,0,37,1,0,0,0,0,39,1,0,0,0,0,41,1,0,0,0,0,43,1,0,0,0,0,45,
		1,0,0,0,0,47,1,0,0,0,0,49,1,0,0,0,0,51,1,0,0,0,0,53,1,0,0,0,0,55,1,0,0,
		0,0,57,1,0,0,0,0,59,1,0,0,0,0,61,1,0,0,0,0,63,1,0,0,0,0,65,1,0,0,0,0,67,
		1,0,0,0,0,69,1,0,0,0,1,71,1,0,0,0,3,78,1,0,0,0,5,85,1,0,0,0,7,90,1,0,0,
		0,9,94,1,0,0,0,11,98,1,0,0,0,13,104,1,0,0,0,15,110,1,0,0,0,17,115,1,0,
		0,0,19,122,1,0,0,0,21,129,1,0,0,0,23,134,1,0,0,0,25,140,1,0,0,0,27,146,
		1,0,0,0,29,153,1,0,0,0,31,163,1,0,0,0,33,178,1,0,0,0,35,185,1,0,0,0,37,
		187,1,0,0,0,39,189,1,0,0,0,41,191,1,0,0,0,43,193,1,0,0,0,45,195,1,0,0,
		0,47,197,1,0,0,0,49,199,1,0,0,0,51,201,1,0,0,0,53,203,1,0,0,0,55,205,1,
		0,0,0,57,207,1,0,0,0,59,209,1,0,0,0,61,211,1,0,0,0,63,214,1,0,0,0,65,216,
		1,0,0,0,67,219,1,0,0,0,69,221,1,0,0,0,71,72,5,101,0,0,72,73,5,120,0,0,
		73,74,5,116,0,0,74,75,5,101,0,0,75,76,5,114,0,0,76,77,5,110,0,0,77,2,1,
		0,0,0,78,79,5,101,0,0,79,80,5,120,0,0,80,81,5,112,0,0,81,82,5,111,0,0,
		82,83,5,114,0,0,83,84,5,116,0,0,84,4,1,0,0,0,85,86,5,102,0,0,86,87,5,117,
		0,0,87,88,5,110,0,0,88,89,5,99,0,0,89,6,1,0,0,0,90,91,5,108,0,0,91,92,
		5,101,0,0,92,93,5,116,0,0,93,8,1,0,0,0,94,95,5,109,0,0,95,96,5,117,0,0,
		96,97,5,116,0,0,97,10,1,0,0,0,98,99,5,105,0,0,99,100,5,110,0,0,100,101,
		5,116,0,0,101,102,5,51,0,0,102,103,5,50,0,0,103,12,1,0,0,0,104,105,5,105,
		0,0,105,106,5,110,0,0,106,107,5,116,0,0,107,108,5,54,0,0,108,109,5,52,
		0,0,109,14,1,0,0,0,110,111,5,98,0,0,111,112,5,111,0,0,112,113,5,111,0,
		0,113,114,5,108,0,0,114,16,1,0,0,0,115,116,5,115,0,0,116,117,5,116,0,0,
		117,118,5,114,0,0,118,119,5,105,0,0,119,120,5,110,0,0,120,121,5,103,0,
		0,121,18,1,0,0,0,122,123,5,115,0,0,123,124,5,116,0,0,124,125,5,114,0,0,
		125,126,5,117,0,0,126,127,5,99,0,0,127,128,5,116,0,0,128,20,1,0,0,0,129,
		130,5,116,0,0,130,131,5,114,0,0,131,132,5,117,0,0,132,133,5,101,0,0,133,
		22,1,0,0,0,134,135,5,102,0,0,135,136,5,97,0,0,136,137,5,108,0,0,137,138,
		5,115,0,0,138,139,5,101,0,0,139,24,1,0,0,0,140,141,5,119,0,0,141,142,5,
		104,0,0,142,143,5,105,0,0,143,144,5,108,0,0,144,145,5,101,0,0,145,26,1,
		0,0,0,146,147,5,114,0,0,147,148,5,101,0,0,148,149,5,116,0,0,149,150,5,
		117,0,0,150,151,5,114,0,0,151,152,5,110,0,0,152,28,1,0,0,0,153,157,5,34,
		0,0,154,156,9,0,0,0,155,154,1,0,0,0,156,159,1,0,0,0,157,158,1,0,0,0,157,
		155,1,0,0,0,158,160,1,0,0,0,159,157,1,0,0,0,160,161,5,34,0,0,161,30,1,
		0,0,0,162,164,7,0,0,0,163,162,1,0,0,0,164,165,1,0,0,0,165,163,1,0,0,0,
		165,166,1,0,0,0,166,175,1,0,0,0,167,169,5,95,0,0,168,170,7,0,0,0,169,168,
		1,0,0,0,170,171,1,0,0,0,171,169,1,0,0,0,171,172,1,0,0,0,172,174,1,0,0,
		0,173,167,1,0,0,0,174,177,1,0,0,0,175,173,1,0,0,0,175,176,1,0,0,0,176,
		32,1,0,0,0,177,175,1,0,0,0,178,182,7,1,0,0,179,181,7,2,0,0,180,179,1,0,
		0,0,181,184,1,0,0,0,182,180,1,0,0,0,182,183,1,0,0,0,183,34,1,0,0,0,184,
		182,1,0,0,0,185,186,5,59,0,0,186,36,1,0,0,0,187,188,5,44,0,0,188,38,1,
		0,0,0,189,190,5,40,0,0,190,40,1,0,0,0,191,192,5,41,0,0,192,42,1,0,0,0,
		193,194,5,123,0,0,194,44,1,0,0,0,195,196,5,125,0,0,196,46,1,0,0,0,197,
		198,5,46,0,0,198,48,1,0,0,0,199,200,5,42,0,0,200,50,1,0,0,0,201,202,5,
		47,0,0,202,52,1,0,0,0,203,204,5,37,0,0,204,54,1,0,0,0,205,206,5,43,0,0,
		206,56,1,0,0,0,207,208,5,45,0,0,208,58,1,0,0,0,209,210,5,60,0,0,210,60,
		1,0,0,0,211,212,5,60,0,0,212,213,5,61,0,0,213,62,1,0,0,0,214,215,5,62,
		0,0,215,64,1,0,0,0,216,217,5,62,0,0,217,218,5,61,0,0,218,66,1,0,0,0,219,
		220,5,61,0,0,220,68,1,0,0,0,221,222,7,3,0,0,222,223,1,0,0,0,223,224,6,
		34,0,0,224,70,1,0,0,0,6,0,157,165,171,175,182,1,6,0,0
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace Ca21.Antlr
