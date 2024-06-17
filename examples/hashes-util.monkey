let map = fn(arr, f) {
    let iter = fn(arr, acc) {
        if (len(arr) == 0) {
            acc
        } else {
            iter(rest(arr), push(acc, f(first(arr))))
        }
    }
    iter(arr, [])
}

let values = fn(hash) {
    map(keys(hash), fn(key) { hash[key] })
}

let items = fn(hash) {
    map(keys(hash), fn(key) { [key, hash[key]] })
}

let toHash = fn(arr) {
    let iter = fn(arr, acc) {
        if (len(arr) == 0) {
            acc
        } else {
            let item = first(arr);
            iter(rest(arr), push(acc, item[0], item[1]))
        }
    }
    iter(arr, {})
}

let hash = {1: 2, true: false, "ur": "mom"};

puts(keys(hash));
puts(values(hash));
puts(items(hash));
puts(toHash(items(hash)));