namespace LibrarySystem

open System

// Domain Types
type BookId = BookId of Guid

type ISBN = private ISBN of string
module ISBN =
    // Smart constructor to validate ISBN
    let create (s: string) =
        if String.IsNullOrWhiteSpace(s) then Error "ISBN cannot be empty"
        else Ok (ISBN s)
    let value (ISBN s) = s

type MemberId = MemberId of string

type BookStatus =
    | Available
    | Borrowed of memberId: MemberId * dueDate: DateTime

type Book = {
    Id: BookId
    Title: string
    Author: string
    ISBN: ISBN
    Status: BookStatus
}

// Domain Errors
type LibraryError =
    | BookNotFound
    | BookNotAvailable
    | InvalidData of string
    | StorageError of string
