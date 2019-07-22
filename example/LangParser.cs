﻿using System.Collections.Generic;

public sealed class LangParser : Parser<Expression>
{
	protected override Expression TryParse()
	{
		return Expression();
	}

	private Expression Expression()
	{
		var token = Peek();
		switch ((TokenKind)token.kind)
		{
		case TokenKind.OpenCurlyBrackets:
			return Block();
		case TokenKind.If:
			return If();
		case TokenKind.While:
			return While();
		case TokenKind.Return:
			Next();
			return new ReturnExpression { expression = Expression() };
		case TokenKind.Break:
			Next();
			return new BreakExpression();
		default:
			return Assignment();
		}
	}

	private BlockExpression Block()
	{
		Consume((int)TokenKind.OpenCurlyBrackets, "Expected '{' before block");
		var statements = new List<Expression>();

		while (!Check((int)TokenKind.CloseCurlyBrackets))
			statements.Add(Expression());

		Consume((int)TokenKind.CloseCurlyBrackets, "Expected '}' after block");
		return new BlockExpression { expressions = statements };
	}

	private IfExpression If()
	{
		Consume((int)TokenKind.If, "Expected 'if' keyword");
		var condition = Expression();
		var thenBlock = Block();

		Option<Expression> elseBlock = Option.None;
		if (Match((int)TokenKind.Else))
		{
			elseBlock = Option.Some(Check((int)TokenKind.If) ?
				If() as Expression :
				Block() as Expression
			);
		}

		return new IfExpression
		{
			condition = condition,
			thenBlock = thenBlock,
			elseBlock = elseBlock
		};
	}

	private WhileExpression While()
	{
		Consume((int)TokenKind.While, "Expected 'while' keyword");
		var condition = Expression();
		var body = Block();

		return new WhileExpression
		{
			condition = condition,
			body = body
		};
	}

	private Expression Assignment()
	{
		var exp = LogicOr();

		if (!Match((int)TokenKind.Equal))
			return exp;

		var value = LogicOr();

		if (exp is VariableExpression varExp)
			return new AssignmentExpression(varExp, value);

		throw new ParseException("Invalid assignment target");
	}

	private Expression LogicOr()
	{
		var exp = LogicAnd();

		while (Check((int)TokenKind.Or))
		{
			var op = Next();
			var right = LogicAnd();
			exp = new LogicOperationExpression(op, exp, right);
		}

		return exp;
	}

	private Expression LogicAnd()
	{
		var exp = Equality();

		while (Check((int)TokenKind.And))
		{
			var op = Next();
			var right = Equality();
			exp = new LogicOperationExpression(op, exp, right);
		}

		return exp;
	}

	private Expression Equality()
	{
		var exp = Comparison();

		while (CheckAny((int)TokenKind.EqualEqual, (int)TokenKind.BangEqual))
		{
			var op = Next();
			var right = Comparison();
			exp = new BinaryOperationExpression(op, exp, right);
		}

		return exp;
	}

	private Expression Comparison()
	{
		var exp = Addition();

		while (CheckAny(
			(int)TokenKind.Lesser,
			(int)TokenKind.LesserEqual,
			(int)TokenKind.Greater,
			(int)TokenKind.GreaterEqual
		))
		{
			var op = Next();
			var right = Addition();
			exp = new BinaryOperationExpression(op, exp, right);
		}

		return exp;
	}

	private Expression Addition()
	{
		var exp = Multiplication();

		while (CheckAny((int)TokenKind.Sum, (int)TokenKind.Minus))
		{
			var op = Next();
			var right = Multiplication();
			exp = new BinaryOperationExpression(op, exp, right);
		}

		return exp;
	}

	private Expression Multiplication()
	{
		var exp = Unary();

		while (CheckAny((int)TokenKind.Asterisk, (int)TokenKind.Slash))
		{
			var op = Next();
			var right = Unary();
			exp = new BinaryOperationExpression(op, exp, right);
		}

		return exp;
	}

	private Expression Unary()
	{
		if (!CheckAny((int)TokenKind.Minus, (int)TokenKind.Bang))
			return Call();

		var op = Next();
		var exp = Unary();
		return new UnaryOperationExpression(op, exp);
	}

	private Expression Call()
	{
		var exp = Primary();

		while (Match((int)TokenKind.OpenParenthesis))
			exp = FinishCall(exp);

		return exp;

		Expression FinishCall(Expression callee)
		{
			var args = new List<Expression>();
			if (!Match((int)TokenKind.CloseParenthesis))
			{
				do
					args.Add(Expression());
				while (Match((int)TokenKind.Comma));
			}

			Token paren = Consume((int)TokenKind.CloseParenthesis, "Expected ')' after arguments.");

			return new CallExpression
			{
				callee = callee,
				closeParenthesis = paren,
				arguments = args
			};
		}
	}

	private Expression Primary()
	{
		var token = Next();
		switch ((TokenKind)token.kind)
		{
		case TokenKind.True:
			return new ValueExpression(token, true);
		case TokenKind.False:
			return new ValueExpression(token, false);
		case TokenKind.Nil:
			return new ValueExpression(token, null);
		case TokenKind.IntegerNumber:
			return new ValueExpression(
				token,
				int.Parse(source.Substring(token.index, token.length))
			);
		case TokenKind.RealNumber:
			return new ValueExpression(
				token,
				float.Parse(source.Substring(token.index, token.length))
			);
		case TokenKind.String:
			return new ValueExpression(
				token,
				source.Substring(token.index + 1, token.length - 2)
			);
		case TokenKind.Identifier:
			return new ValueExpression(
				token,
				source.Substring(token.index, token.length)
			);
		case TokenKind.OpenParenthesis:
			var exp = Expression();
			Consume((int)TokenKind.CloseParenthesis, "Expected ')' after expression");
			return new GroupExpression { expression = exp };
		default:
			throw new ParseException("Invalid expression");
		}
	}
}