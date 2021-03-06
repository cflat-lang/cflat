namespace cflat
{
	internal enum TokenKind
	{
		IntLiteral, FloatLiteral, StringLiteral, True, False, Identifier,
		Mod, Pub, Function, Struct, If, Else, Repeat, While, Return, Break,
		Dot, Comma, Colon, Bang, And, Or, Length,

		Let, Mut, Ampersand, Set,
		Bool, Int, Float, String,

		Print,

		OpenParenthesis, CloseParenthesis, OpenCurlyBrackets, CloseCurlyBrackets,
		OpenSquareBrackets, CloseSquareBrackets,

		Plus, Minus, Asterisk, Slash,
		EqualEqual, BangEqual, Equal, Less, Greater, LessEqual, GreaterEqual,

		COUNT,
		End,
		Error,
	}

	internal enum Precedence
	{
		None,
		Assignment, // =
		Or, // or
		And, // and
		Equality, // == !=
		Comparison, // < > <= >=
		Term,// + -
		Factor, // * /
		Unary, // ! -
		Call, // . () []
		Primary
	}

	internal readonly struct Token
	{
		public readonly TokenKind kind;
		public readonly Slice slice;

		public Token(TokenKind kind, Slice slice)
		{
			this.kind = kind;
			this.slice = slice;
		}

		public bool IsValid()
		{
			return kind >= 0;
		}
	}
}