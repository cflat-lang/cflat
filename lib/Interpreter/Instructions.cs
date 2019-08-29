public enum Instruction
{
	Halt,
	Call,
	CallNative,
	CallNativeAuto,
	Return,
	Print,
	Pop,
	PopMultiple,
	Move,
	LoadUnit,
	LoadTrue,
	LoadFalse,
	LoadLiteral,
	LoadFunction,
	LoadNativeFunction,
	AssignLocal,
	LoadLocal,
	AssignLocalMultiple,
	LoadLocalMultiple,
	IncrementLocalInt,
	LoadField,
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
	JumpForward,
	JumpBackward,
	JumpForwardIfFalse,
	JumpForwardIfTrue,
	PopAndJumpForwardIfFalse,
	ForLoopCheck,

	DebugSaveTypeStack,
}