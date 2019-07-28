public enum Instruction
{
	Return,
	Pop,
	LoadNil,
	LoadTrue,
	LoadFalse,
	LoadLiteral,
	IntToFloat,
	FloatToInt,
	NegateInt,
	NegateFloat,
	AddInt,
	AddFloat,
	SubtractInt,
	SubtractFloat,
	MultiplyInt,
	MultiplyFloat,
	DivideInt,
	DivideFloat,
	Not,
	EqualBool,
	EqualInt,
	EqualFloat,
	EqualString,
	GreaterInt,
	GreaterFloat,
	LessInt,
	LessFloat,
}