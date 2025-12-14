namespace LibrarySystem

open System

module LibraryService =

    // --- Role 2: CRUD Developer ---
    let addBook title author isbnStr (books: Book list) =
        if String.IsNullOrWhiteSpace title then
            Error (InvalidData "Title cannot be empty")
        elif String.IsNullOrWhiteSpace author then
            Error (InvalidData "Author cannot be empty")
        elif String.IsNullOrWhiteSpace isbnStr then
            Error (InvalidData "ISBN cannot be empty")
        else
            match ISBN.create isbnStr with
            | Error msg -> Error (InvalidData msg)
            | Ok validIsbn ->
                let newBook = {
                    Id = BookId (Guid.NewGuid())
                    Title = title
                    Author = author
                    ISBN = validIsbn
                    Status = Available
                }
                Ok (newBook :: books)

    // 🆕 UPDATE BOOK
    let updateBook (bookId: BookId) title author isbnStr (books: Book list) =
        if String.IsNullOrWhiteSpace title then
            Error (InvalidData "Title cannot be empty")
        elif String.IsNullOrWhiteSpace author then
            Error (InvalidData "Author cannot be empty")
        elif String.IsNullOrWhiteSpace isbnStr then
            Error (InvalidData "ISBN cannot be empty")
        else
            match ISBN.create isbnStr with
            | Error msg -> Error (InvalidData msg)
            | Ok validIsbn ->
                let updateSingle b =
                    if b.Id = bookId then
                        Ok { b with 
                                Title = title
                                Author = author
                                ISBN = validIsbn }
                    else Ok b
                
                let results = books |> List.map updateSingle
                let firstError = results |> List.tryPick (function Error e -> Some e | _ -> None)
                match firstError with
                | Some e -> Error e
                | None ->
                    Ok (results |> List.map (function Ok b -> b | Error _ -> failwith "Impossible"))

    // 🆕 DELETE BOOK
    let deleteBook (bookId: BookId) (books: Book list) : Result<Book list, LibraryError> =
        let bookExists = books |> List.exists (fun b -> b.Id = bookId)
        if not bookExists then
            Error BookNotFound
        else
            Ok (books |> List.filter (fun b -> b.Id <> bookId))

    // --- Role 3: Search Developer ---
    let searchByTitle (query: string) (books: Book list) =
        books
        |> List.filter (fun b -> b.Title.Contains(query, StringComparison.OrdinalIgnoreCase))

    let searchAvailable (books: Book list) =
        books
        |> List.filter (fun b -> b.Status = Available)

    // --- Role 4: Borrow/Return Logic Developer ---
    let borrowBook (bookId: BookId) (memberId: string) (books: Book list) =
        let updateBook b =
            if b.Id = bookId then
                match b.Status with
                | Available ->
                    Ok { b with Status = Borrowed(MemberId memberId, DateTime.Now.AddDays(14.0)) }
                | _ -> Error BookNotAvailable
            else
                Ok b

        let results = books |> List.map updateBook
        let firstError = results |> List.tryPick (function Error e -> Some e | _ -> None)
        match firstError with
        | Some e -> Error e
        | None ->
            Ok (results |> List.map (function Ok b -> b | Error _ -> failwith "Impossible"))

    let returnBook (bookId: BookId) (books: Book list) =
        books
        |> List.map (fun b ->
            if b.Id = bookId then { b with Status = Available } else b
        )
