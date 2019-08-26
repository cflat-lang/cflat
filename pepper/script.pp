fn fibonacci(n:int):int {
	if n <= 1 {
		return n
	}

	fibonacci(n - 2) + fibonacci(n - 1)
}

fn main() {
	let sw = StartStopwatch()
	fibonacci(30)
	let s = StopStopwatch(sw)
	print {"time",s}
}
