using Xunit;
using cflat;

public sealed class TupleTests
{
	[Theory]
	[InlineData("fn f():int{let{a,b}={true,3} a b}", 3)]
	[InlineData("fn f():int{let{_,b}={true,3} b}", 3)]
	[InlineData("fn f():int{let t={true,3} let{a,b}=t a b}", 3)]
	[InlineData("fn f():int{let{_,mut b}={true,3} set b=5 b}", 5)]
	public void TupleDeconstructionTests(string source, int expected)
	{
		var v = TestHelper.Run<Int>(source, out var a);
		a.AssertSuccessCall();
		Assert.Equal(expected, v.value);
	}

	[Theory]
	[InlineData("fn f(){let{a,_}={true,3} a=false}")]
	public void TupleDeconstructionErrors(string source)
	{
		Assert.Throws<CompileErrorException>(() =>
		{
			TestHelper.Run<Unit>(source, out var a);
		});
	}

	[Theory]
	[InlineData("fn b(t:{bool,int}):int{let{a,b}=t a b}fn f():int{b({true,3})}", 3)]
	public void TupleParameterTests(string source, int expected)
	{
		var v = TestHelper.Run<Int>(source, out var a);
		a.AssertSuccessCall();
		Assert.Equal(expected, v.value);
	}

	[Theory]
	[InlineData(
		"fn f(){let _={{true,true},3}}",
		new[] { 0, 2, 2, 2 },
		new[] {
			TypeKind.Bool,
			TypeKind.Bool,
			TypeKind.Tuple,
			TypeKind.Int
		}
	)]
	[InlineData(
		"fn f(){let _={0,{true,true},3}}",
		new[] { 0, 2, 2, 3 },
		new[] {
			TypeKind.Bool,
			TypeKind.Bool,
			TypeKind.Int,
			TypeKind.Tuple,
			TypeKind.Int
		}
	)]
	[InlineData(
		"fn f(){let _={{0,{1.0,2.0},true},3}}",
		new[] { 0, 2, 2, 3, 5, 2 },
		new[] {
			TypeKind.Float,
			TypeKind.Float,
			TypeKind.Int,
			TypeKind.Tuple,
			TypeKind.Bool,
			TypeKind.Tuple,
			TypeKind.Int
		}
	)]
	public void NestedTupleTest(string source, int[] expectedSlices, TypeKind[] expectedElementKinds)
	{
		var cflat = new CFlat();

		string error = null;
		var errors = cflat.CompileSource(new Source(new Uri("source"), source), TestHelper.CompilerMode, Option.None);
		if (errors.count > 0)
			error = cflat.GetFormattedCompileErrors();
		Assert.Null(error);

		var sliceCount = expectedSlices.Length / 2;
		Assert.Equal(sliceCount, cflat.chunk.tupleTypes.count);
		Assert.Equal(expectedElementKinds.Length, cflat.chunk.tupleElementTypes.count);

		var slices = new int[cflat.chunk.tupleTypes.count * 2];
		for (var i = 0; i < sliceCount; i++)
		{
			var tupleType = cflat.chunk.tupleTypes.buffer[i];
			slices[i * 2] = tupleType.elements.index;
			slices[i * 2 + 1] = tupleType.elements.length;
		}
		Assert.Equal(expectedSlices, slices);

		var elementKinds = new TypeKind[cflat.chunk.tupleElementTypes.count];
		for (var i = 0; i < elementKinds.Length; i++)
		{
			var elementType = cflat.chunk.tupleElementTypes.buffer[i];
			elementKinds[i] = elementType.kind;
		}
		Assert.Equal(expectedElementKinds, elementKinds);
	}
}