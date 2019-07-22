using Xunit;

public sealed class ParserTest
{
	private readonly LangTokenizer tokenizer = new LangTokenizer();
	private readonly LangParser parser = new LangParser();

	[Theory]
	[InlineData("-1")]
	[InlineData("1 + 2")]
	[InlineData("1 * -2")]
	[InlineData("1 + 2 * 3")]
	[InlineData("(1 + 2) + 3 * 4 + 5")]
	[InlineData("(1 + 2) + 3 == 4 + 5")]
	[InlineData("1 < 2 != 3 >= 4")]
	[InlineData("true == !false")]
	[InlineData("\"text\" != nil")]
	[InlineData("true or false")]
	[InlineData("true and false or 3 > 2")]
	[InlineData("assign = true or false")]
	public void TestExpressions(string source)
	{
		var result = parser.parser.Parse(source, tokenizer.scanners, parser.Expression);
		Assert.True(result.isOk, ParserHelper.FormatError(source, result.error, 2));
	}

	[Theory]
	[InlineData("while true { 1 + 2 }")]
	[InlineData("if true { 1 + 2 }")]
	[InlineData("if true { 1 + 2 } else { 3 == 4 }")]
	[InlineData("if true { 1 + 2 } else if a > 3 { 3 == 4 } else { c = \"txt\" }")]
	[InlineData("if if true { false } { c = 4 }")]
	public void TestComplexExpressions(string source)
	{
		var result = parser.parser.Parse(source, tokenizer.scanners, parser.Expression);
		Assert.True(result.isOk, ParserHelper.FormatError(source, result.error, 2));
	}

	[Theory]
	[InlineData("a = 2 b = 3")]
	[InlineData("a = 2 b = 3 + 4")]
	[InlineData("a = if true { 1 < 2 } b = 3 + 4")]
	public void TestMultiExpressions(string source)
	{
		var result = parser.parser.Parse(source, tokenizer.scanners, parser.Expression);
		Assert.True(result.isOk, ParserHelper.FormatError(source, result.error, 2));
	}

	[Theory]
	[InlineData("fn foo(a,b) { return true }")]
	public void TestFunctionDeclaration(string source)
	{
		var result = parser.parser.Parse(source, tokenizer.scanners, parser.Function);
		Assert.True(result.isOk, ParserHelper.FormatError(source, result.error, 2));
	}
}
