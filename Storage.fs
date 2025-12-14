namespace LibrarySystem

open System
open System.IO
open System.Text.Json

module Storage =
    
    let private filePath = "library_data.json"
    
    type BookDto = {
        Id: Guid
        Title: string
        Author: string
        Isbn: string
        IsBorrowed: bool
        Borrower: string
        DueDate: DateTime option
    }

    let private toDto (book: Book) =
        let (BookId bid) = book.Id
        let (m, d) = 
            match book.Status with 
            | Borrowed(MemberId memberId, dueDate) -> (memberId, Some dueDate)
            | Available -> ("", None)
        
        { Id = bid
          Title = book.Title
          Author = book.Author
          Isbn = ISBN.value book.ISBN
          IsBorrowed = (book.Status <> Available)
          Borrower = m
          DueDate = d }

    let private fromDto (dto: BookDto) =
        match ISBN.create dto.Isbn with
        | Error _ -> None 
        | Ok isbn ->
            let status = 
                if dto.IsBorrowed then 
                    Borrowed(MemberId dto.Borrower, Option.defaultValue DateTime.MinValue dto.DueDate)
                else Available
            
            Some { Id = BookId dto.Id; Title = dto.Title; Author = dto.Author; ISBN = isbn; Status = status }

    let save (books: Book list) =
        try
            let dtos = books |> List.map toDto
            let options = JsonSerializerOptions(WriteIndented = true)
            let json = JsonSerializer.Serialize(dtos, options)
            File.WriteAllText(filePath, json)
            Ok ()
        with
        | ex -> Error (StorageError ex.Message)

    let load () =
        try
            if File.Exists(filePath) then
                let json = File.ReadAllText(filePath)
                let dtos = JsonSerializer.Deserialize<BookDto list>(json)
                Ok (dtos |> List.choose fromDto)
            else
                Ok [] // Return empty list if file doesn't exist
        with
        | ex -> Error (StorageError ex.Message)
