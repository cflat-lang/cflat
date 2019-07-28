using System.Collections.Generic;

public readonly struct CompileError
{
	public readonly int sourceIndex;
	public readonly string message;

	public CompileError(int sourceIndex, string message)
	{
		this.sourceIndex = sourceIndex;
		this.message = message;
	}
}

public sealed class Compiler
{
	public readonly struct Convertible
	{
		public readonly string source;
		public readonly Token token;

		public Convertible(string source, Token token)
		{
			this.source = source;
			this.token = token;
		}
	}

	public readonly List<CompileError> errors = new List<CompileError>();
	public Token previousToken;
	private Token currentToken;

	internal ITokenizer tokenizer;
	private ParseRule[] parseRules;
	private bool panicMode;
	private ByteCodeChunk chunk;
	private Buffer<ValueType> typeStack = new Buffer<ValueType>(256);

	public void Begin(ITokenizer tokenizer, ParseRule[] parseRules)
	{
		errors.Clear();
		this.tokenizer = tokenizer;
		this.parseRules = parseRules;
		previousToken = new Token(Token.EndKind, 0, 0);
		currentToken = new Token(Token.EndKind, 0, 0);
		panicMode = false;
		chunk = new ByteCodeChunk();
		typeStack.count = 0;
	}

	public Compiler AddSoftError(int sourceIndex, string message)
	{
		errors.Add(new CompileError(sourceIndex, message));
		return this;
	}

	public Compiler AddHardError(int sourceIndex, string message)
	{
		if (panicMode)
			return this;

		panicMode = true;
		errors.Add(new CompileError(sourceIndex, message));

		return this;
	}

	public ByteCodeChunk GetByteCodeChunk()
	{
		var c = chunk;
		chunk = null;
		return c;
	}

	public int GetTokenPrecedence(int tokenKind)
	{
		return parseRules[tokenKind].precedence;
	}

	public void ParseWithPrecedence(int precedence)
	{
		Next();
		if (previousToken.kind == Token.EndKind)
			return;

		var prefixRule = parseRules[previousToken.kind].prefixRule;
		if (prefixRule == null)
		{
			AddHardError(previousToken.index, "Expected expression");
			return;
		}
		prefixRule(this);

		while (
			currentToken.kind != Token.EndKind &&
			precedence <= parseRules[currentToken.kind].precedence
		)
		{
			Next();
			var infixRule = parseRules[previousToken.kind].infixRule;
			infixRule(this);
		}
	}

	public void Next()
	{
		previousToken = currentToken;

		while (true)
		{
			currentToken = tokenizer.Next();
			if (currentToken.kind != Token.ErrorKind)
				break;

			AddHardError(currentToken.index, "Invalid char");
		}
	}

	public bool Match(int tokenKind)
	{
		if (currentToken.kind != tokenKind)
			return false;

		Next();
		return true;
	}

	public void Consume(int tokenKind, string errorMessage)
	{
		if (currentToken.kind == tokenKind)
			Next();
		else
			AddHardError(currentToken.index, errorMessage);
	}

	public void PushType(ValueType type)
	{
		typeStack.PushBack(type);
	}

	public ValueType PopType()
	{
		return typeStack.PopLast();
	}

	public Convertible Convert()
	{
		return new Convertible(tokenizer.Source, previousToken);
	}

	public Compiler EmitByte(byte value)
	{
		chunk.WriteByte(value, previousToken.index);
		return this;
	}

	public Compiler EmitInstruction(Instruction instruction)
	{
		EmitByte((byte)instruction);
		return this;
	}

	public Compiler EmitLoadLiteral(ValueData value, ValueType type)
	{
		var index = System.Array.IndexOf(chunk.literalData.buffer, value);
		if (index < 0)
			index = chunk.AddValueLiteral(value, type);

		EmitInstruction(Instruction.LoadLiteral);
		EmitByte((byte)index);

		return this;
	}

	public Compiler EmitLoadStringLiteral(string value)
	{
		var stringIndex = System.Array.IndexOf(chunk.stringLiterals.buffer, value);

		var constantIndex = stringIndex < 0 ?
			chunk.AddStringLiteral(value) :
			System.Array.IndexOf(
				chunk.literalData.buffer,
				new ValueData(stringIndex)
			);

		EmitInstruction(Instruction.LoadLiteral);
		EmitByte((byte)constantIndex);

		return this;
	}
}