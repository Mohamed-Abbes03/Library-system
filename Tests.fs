namespace LibrarySystem

module Tests =
    let runTests () =
        printfn "Running Tests..."
        
        // Test 1: Add Book
        let emptyLib = []
        match LibraryService.addBook "F# in Action" "Isaac" "12345" emptyLib with
        | Ok lib -> printfn "✅ Add Book Test Passed"
        | Error _ -> printfn "❌ Add Book Test Failed"

        // Test 2: ISBN Validation
        match LibraryService.addBook "Bad Book" "Nobody" "" emptyLib with
        | Error (InvalidData _) -> printfn "✅ ISBN Validation Test Passed"
        | _ -> printfn "❌ ISBN Validation Test Failed"
