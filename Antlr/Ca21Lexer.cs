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
		FuncKeyword=1, LetKeyword=2, MutKeyword=3, Int32Keyword=4, StringKeyword=5, 
		TrueKeyword=6, FalseKeyword=7, WhileKeyword=8, ReturnKeyword=9, String=10, 
		Integer=11, Identifier=12, Comma=13, LeftParenthesis=14, RightParenthesis=15, 
		LeftBrace=16, RightBrace=17, Semicolon=18, Star=19, Slash=20, Percentage=21, 
		Plus=22, Minus=23, LessThan=24, LessThanOrEqual=25, GreaterThan=26, GreaterThanOrEqual=27, 
		Equal=28, Whitespace=29;
	public static string[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static string[] modeNames = {
		"DEFAULT_MODE"
	};

	public static readonly string[] ruleNames = {
		"FuncKeyword", "LetKeyword", "MutKeyword", "Int32Keyword", "StringKeyword", 
		"TrueKeyword", "FalseKeyword", "WhileKeyword", "ReturnKeyword", "String", 
		"Integer", "Identifier", "Comma", "LeftParenthesis", "RightParenthesis", 
		"LeftBrace", "RightBrace", "Semicolon", "Star", "Slash", "Percentage", 
		"Plus", "Minus", "LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual", 
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
		null, "'func'", "'let'", "'mut'", "'int32'", "'string'", "'true'", "'false'", 
		"'while'", "'return'", null, null, null, "','", "'('", "')'", "'{'", "'}'", 
		"';'", "'*'", "'/'", "'%'", "'+'", "'-'", "'<'", "'<='", "'>'", "'>='", 
		"'='"
	};
	private static readonly string[] _SymbolicNames = {
		null, "FuncKeyword", "LetKeyword", "MutKeyword", "Int32Keyword", "StringKeyword", 
		"TrueKeyword", "FalseKeyword", "WhileKeyword", "ReturnKeyword", "String", 
		"Integer", "Identifier", "Comma", "LeftParenthesis", "RightParenthesis", 
		"LeftBrace", "RightBrace", "Semicolon", "Star", "Slash", "Percentage", 
		"Plus", "Minus", "LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual", 
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
		4,0,29,179,6,-1,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,7,
		6,2,7,7,7,2,8,7,8,2,9,7,9,2,10,7,10,2,11,7,11,2,12,7,12,2,13,7,13,2,14,
		7,14,2,15,7,15,2,16,7,16,2,17,7,17,2,18,7,18,2,19,7,19,2,20,7,20,2,21,
		7,21,2,22,7,22,2,23,7,23,2,24,7,24,2,25,7,25,2,26,7,26,2,27,7,27,2,28,
		7,28,1,0,1,0,1,0,1,0,1,0,1,1,1,1,1,1,1,1,1,2,1,2,1,2,1,2,1,3,1,3,1,3,1,
		3,1,3,1,3,1,4,1,4,1,4,1,4,1,4,1,4,1,4,1,5,1,5,1,5,1,5,1,5,1,6,1,6,1,6,
		1,6,1,6,1,6,1,7,1,7,1,7,1,7,1,7,1,7,1,8,1,8,1,8,1,8,1,8,1,8,1,8,1,9,1,
		9,5,9,112,8,9,10,9,12,9,115,9,9,1,9,1,9,1,10,4,10,120,8,10,11,10,12,10,
		121,1,10,1,10,4,10,126,8,10,11,10,12,10,127,5,10,130,8,10,10,10,12,10,
		133,9,10,1,11,1,11,5,11,137,8,11,10,11,12,11,140,9,11,1,12,1,12,1,13,1,
		13,1,14,1,14,1,15,1,15,1,16,1,16,1,17,1,17,1,18,1,18,1,19,1,19,1,20,1,
		20,1,21,1,21,1,22,1,22,1,23,1,23,1,24,1,24,1,24,1,25,1,25,1,26,1,26,1,
		26,1,27,1,27,1,28,1,28,1,28,1,28,1,113,0,29,1,1,3,2,5,3,7,4,9,5,11,6,13,
		7,15,8,17,9,19,10,21,11,23,12,25,13,27,14,29,15,31,16,33,17,35,18,37,19,
		39,20,41,21,43,22,45,23,47,24,49,25,51,26,53,27,55,28,57,29,1,0,4,1,0,
		48,57,3,0,65,90,95,95,97,122,4,0,48,57,65,90,95,95,97,122,3,0,9,10,13,
		13,32,32,183,0,1,1,0,0,0,0,3,1,0,0,0,0,5,1,0,0,0,0,7,1,0,0,0,0,9,1,0,0,
		0,0,11,1,0,0,0,0,13,1,0,0,0,0,15,1,0,0,0,0,17,1,0,0,0,0,19,1,0,0,0,0,21,
		1,0,0,0,0,23,1,0,0,0,0,25,1,0,0,0,0,27,1,0,0,0,0,29,1,0,0,0,0,31,1,0,0,
		0,0,33,1,0,0,0,0,35,1,0,0,0,0,37,1,0,0,0,0,39,1,0,0,0,0,41,1,0,0,0,0,43,
		1,0,0,0,0,45,1,0,0,0,0,47,1,0,0,0,0,49,1,0,0,0,0,51,1,0,0,0,0,53,1,0,0,
		0,0,55,1,0,0,0,0,57,1,0,0,0,1,59,1,0,0,0,3,64,1,0,0,0,5,68,1,0,0,0,7,72,
		1,0,0,0,9,78,1,0,0,0,11,85,1,0,0,0,13,90,1,0,0,0,15,96,1,0,0,0,17,102,
		1,0,0,0,19,109,1,0,0,0,21,119,1,0,0,0,23,134,1,0,0,0,25,141,1,0,0,0,27,
		143,1,0,0,0,29,145,1,0,0,0,31,147,1,0,0,0,33,149,1,0,0,0,35,151,1,0,0,
		0,37,153,1,0,0,0,39,155,1,0,0,0,41,157,1,0,0,0,43,159,1,0,0,0,45,161,1,
		0,0,0,47,163,1,0,0,0,49,165,1,0,0,0,51,168,1,0,0,0,53,170,1,0,0,0,55,173,
		1,0,0,0,57,175,1,0,0,0,59,60,5,102,0,0,60,61,5,117,0,0,61,62,5,110,0,0,
		62,63,5,99,0,0,63,2,1,0,0,0,64,65,5,108,0,0,65,66,5,101,0,0,66,67,5,116,
		0,0,67,4,1,0,0,0,68,69,5,109,0,0,69,70,5,117,0,0,70,71,5,116,0,0,71,6,
		1,0,0,0,72,73,5,105,0,0,73,74,5,110,0,0,74,75,5,116,0,0,75,76,5,51,0,0,
		76,77,5,50,0,0,77,8,1,0,0,0,78,79,5,115,0,0,79,80,5,116,0,0,80,81,5,114,
		0,0,81,82,5,105,0,0,82,83,5,110,0,0,83,84,5,103,0,0,84,10,1,0,0,0,85,86,
		5,116,0,0,86,87,5,114,0,0,87,88,5,117,0,0,88,89,5,101,0,0,89,12,1,0,0,
		0,90,91,5,102,0,0,91,92,5,97,0,0,92,93,5,108,0,0,93,94,5,115,0,0,94,95,
		5,101,0,0,95,14,1,0,0,0,96,97,5,119,0,0,97,98,5,104,0,0,98,99,5,105,0,
		0,99,100,5,108,0,0,100,101,5,101,0,0,101,16,1,0,0,0,102,103,5,114,0,0,
		103,104,5,101,0,0,104,105,5,116,0,0,105,106,5,117,0,0,106,107,5,114,0,
		0,107,108,5,110,0,0,108,18,1,0,0,0,109,113,5,34,0,0,110,112,9,0,0,0,111,
		110,1,0,0,0,112,115,1,0,0,0,113,114,1,0,0,0,113,111,1,0,0,0,114,116,1,
		0,0,0,115,113,1,0,0,0,116,117,5,34,0,0,117,20,1,0,0,0,118,120,7,0,0,0,
		119,118,1,0,0,0,120,121,1,0,0,0,121,119,1,0,0,0,121,122,1,0,0,0,122,131,
		1,0,0,0,123,125,5,95,0,0,124,126,7,0,0,0,125,124,1,0,0,0,126,127,1,0,0,
		0,127,125,1,0,0,0,127,128,1,0,0,0,128,130,1,0,0,0,129,123,1,0,0,0,130,
		133,1,0,0,0,131,129,1,0,0,0,131,132,1,0,0,0,132,22,1,0,0,0,133,131,1,0,
		0,0,134,138,7,1,0,0,135,137,7,2,0,0,136,135,1,0,0,0,137,140,1,0,0,0,138,
		136,1,0,0,0,138,139,1,0,0,0,139,24,1,0,0,0,140,138,1,0,0,0,141,142,5,44,
		0,0,142,26,1,0,0,0,143,144,5,40,0,0,144,28,1,0,0,0,145,146,5,41,0,0,146,
		30,1,0,0,0,147,148,5,123,0,0,148,32,1,0,0,0,149,150,5,125,0,0,150,34,1,
		0,0,0,151,152,5,59,0,0,152,36,1,0,0,0,153,154,5,42,0,0,154,38,1,0,0,0,
		155,156,5,47,0,0,156,40,1,0,0,0,157,158,5,37,0,0,158,42,1,0,0,0,159,160,
		5,43,0,0,160,44,1,0,0,0,161,162,5,45,0,0,162,46,1,0,0,0,163,164,5,60,0,
		0,164,48,1,0,0,0,165,166,5,60,0,0,166,167,5,61,0,0,167,50,1,0,0,0,168,
		169,5,62,0,0,169,52,1,0,0,0,170,171,5,62,0,0,171,172,5,61,0,0,172,54,1,
		0,0,0,173,174,5,61,0,0,174,56,1,0,0,0,175,176,7,3,0,0,176,177,1,0,0,0,
		177,178,6,28,0,0,178,58,1,0,0,0,6,0,113,121,127,131,138,1,6,0,0
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace Ca21.Antlr
