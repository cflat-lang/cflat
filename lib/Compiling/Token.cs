public enum TokenKind
{
	IntLiteral, FloatLiteral, StringLiteral, True, False, Identifier,
	Function, Struct, For, If, Else, While, Return, Break,
	Dot, Comma, Colon, Not, And, Or, Is,

	Let, Mut, Ref,
	Bool, Int, Float, String,

	Print,

	OpenParenthesis, CloseParenthesis, OpenCurlyBrackets, CloseCurlyBrackets,

	Plus, Minus, Asterisk, Slash,
	Equal, Less, Greater, LessEqual, GreaterEqual,

	COUNT,
	End,
	Error,
}

public enum Precedence
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

public readonly struct Token
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
