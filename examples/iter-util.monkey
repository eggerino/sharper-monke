let reduce = fn(array, initial, f) {
    let iter = fn(arr, acc) {
        if (len(arr) == 0) {
            acc
        } else {
            iter(rest(arr), f(acc, first(arr)))
        }
    }
    iter(array, initial)
}

let forEach = fn(array, f) {
    reduce(array, 0, fn(a, item) { f(item) })
}

let map = fn(array, f) {
    reduce(array, [], fn(acc, item) { push(acc, f(item)) })
}

let filter = fn(array, f) {
    reduce(array, [], fn(acc, item) {
        if (f(item)) {
            push(acc, item)
        } else {
            acc
        }
    })
}

let numbers = [1, 2, 3, 4, 5];
puts(numbers)

forEach(numbers, fn(x) {puts(last(numbers) - x + 1)});

let squares = map(numbers, fn(x) { x * x });
puts(squares);

let largeSquares = filter(squares, fn(x) { x > 10 });
puts(largeSquares);

let sumLargeSquares = reduce(largeSquares, 0, fn(a, b) { a + b });
puts(sumLargeSquares);
