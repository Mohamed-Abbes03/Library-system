namespace LibrarySystem

open LibrarySystem.Tests
open System
open System.Drawing
open System.Windows.Forms

module Program =

    type MainForm() as this =
        inherit Form()

        let mutable books: Book list = []

        // Header labels
        let titleLabel =
            new Label(
                Text = "Library Management System",
                Font = new Font("Segoe UI", 16.0f, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            )

        let statusLabel =
            new Label(
                Text = "Books in catalog: 0",
                Location = new Point(10, 50),
                AutoSize = true
            )

        // Tab control
        let tabControl =
            new TabControl(Location = new Point(10, 80), Size = new Size(770, 420))

        // === TAB 1: View All Books ===
        let viewBooksTab = new TabPage(Text = "View All Books")
        let booksListView =
            new ListView(Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true)

        // === TAB 2: Add Book ===
        let addBookTab = new TabPage(Text = "Add Book")
        let titleInput = new TextBox(Location = new Point(100, 20), Size = new Size(300, 25))
        let authorInput = new TextBox(Location = new Point(100, 60), Size = new Size(300, 25))
        let isbnInput = new TextBox(Location = new Point(100, 100), Size = new Size(300, 25))
        let addBookButton = new Button(Text = "Add Book", Location = new Point(100, 140), Size = new Size(120, 35))
        let addBookMessageLabel =
            new Label(Text = "", Location = new Point(100, 180), AutoSize = true, ForeColor = Color.Green)

        // === TAB 3: Search ===
        let searchTab = new TabPage(Text = "Search")
        let searchInput = new TextBox(Location = new Point(100, 20), Size = new Size(300, 25))
        let searchButton = new Button(Text = "Search", Location = new Point(410, 18), Size = new Size(100, 30))
        let searchResultsList = new ListBox(Location = new Point(100, 60), Size = new Size(410, 200))

        // === TAB 4: Borrow/Return ===
        let borrowReturnTab = new TabPage(Text = "Borrow/Return")
        let bookTitleInput = new TextBox(Location = new Point(120, 20), Size = new Size(300, 25))
        let memberNameInput = new TextBox(Location = new Point(120, 60), Size = new Size(300, 25))
        let borrowButton = new Button(Text = "Borrow Book", Location = new Point(120, 100), Size = new Size(120, 35))
        let returnButton = new Button(Text = "Return Book", Location = new Point(250, 100), Size = new Size(120, 35))
        let borrowReturnMessageLabel =
            new Label(Text = "", Location = new Point(120, 150), AutoSize = true, ForeColor = Color.Blue)

        // === TAB 5: Delete/Update Books (NEW) ===
        let deleteUpdateTab = new TabPage(Text = "Delete/Update")
        
        // Update section
        let updateTitleInput = new TextBox(Location = new Point(100, 20), Size = new Size(300, 25))
        let updateAuthorInput = new TextBox(Location = new Point(100, 60), Size = new Size(300, 25))
        let updateIsbnInput = new TextBox(Location = new Point(100, 100), Size = new Size(300, 25))
        let updateBookIdInput = new TextBox(Location = new Point(100, 140), Size = new Size(300, 25))
        let updateButton = new Button(Text = "Update Book", Location = new Point(100, 180), Size = new Size(120, 35))
        
        // Delete section
        let deleteBookIdInput = new TextBox(Location = new Point(450, 20), Size = new Size(150, 25))
        let deleteButton = new Button(Text = "Delete Book", Location = new Point(450, 60), Size = new Size(120, 35))
        
        let deleteUpdateMessageLabel =
            new Label(Text = "", Location = new Point(100, 230), AutoSize = true, ForeColor = Color.Orange)

        // Initialize ListView columns
        do
            booksListView.Columns.Add("Title", 200) |> ignore
            booksListView.Columns.Add("Author", 180) |> ignore
            booksListView.Columns.Add("ISBN", 150) |> ignore
            booksListView.Columns.Add("Status", 120) |> ignore
            booksListView.Columns.Add("Borrower", 120) |> ignore
            booksListView.Columns.Add("Due Date", 120) |> ignore

        // Helper: format status
        let formatStatus (status: BookStatus) =
            match status with
            | Available -> ("Available", "", "")
            | Borrowed (MemberId m, d) -> ("Borrowed", m, d.ToString("yyyy-MM-dd"))

        // Helper: refresh list + autosave
        let refreshBooksList () =
            booksListView.Items.Clear()
            statusLabel.Text <- sprintf "Books in catalog: %d" books.Length

            books
            |> List.iter (fun book ->
                let (status, borrower, dueDate) = formatStatus book.Status
                let item =
                    new ListViewItem(
                        [| book.Title
                           book.Author
                           ISBN.value book.ISBN
                           status
                           borrower
                           dueDate |]
                    )

                booksListView.Items.Add(item) |> ignore)

            match Storage.save books with
            | Ok () -> ()
            | Error e ->
                MessageBox.Show(sprintf "Error saving: %A" e, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                |> ignore

        // Add Book
        let addBook_Click _ =
            let title = titleInput.Text.Trim()
            let author = authorInput.Text.Trim()
            let isbn = isbnInput.Text.Trim()

            if String.IsNullOrWhiteSpace(title)
               || String.IsNullOrWhiteSpace(author)
               || String.IsNullOrWhiteSpace(isbn) then
                addBookMessageLabel.Text <- "Please fill in all fields"
                addBookMessageLabel.ForeColor <- Color.Red
            else
                match LibraryService.addBook title author isbn books with
                | Ok newBooks ->
                    books <- newBooks
                    titleInput.Clear()
                    authorInput.Clear()
                    isbnInput.Clear()
                    addBookMessageLabel.Text <- "Book added successfully!"
                    addBookMessageLabel.ForeColor <- Color.Green
                    refreshBooksList()

                    let timer = new Timer(Interval = 2000)
                    timer.Tick.Add(fun _ ->
                        addBookMessageLabel.Text <- ""
                        timer.Stop())
                    timer.Start()
                | Error e ->
                    addBookMessageLabel.Text <- sprintf "Error: %A" e
                    addBookMessageLabel.ForeColor <- Color.Red

        // Search
        let search_Click _ =
            let query = searchInput.Text.Trim()
            searchResultsList.Items.Clear()

            if String.IsNullOrWhiteSpace(query) then
                searchResultsList.Items.Add("Please enter a search term") |> ignore
            else
                let results = LibraryService.searchByTitle query books
                if results.IsEmpty then
                    searchResultsList.Items.Add("No books found") |> ignore
                else
                    results
                    |> List.iter (fun book ->
                        let status =
                            match book.Status with
                            | Available -> "Available"
                            | Borrowed _ -> "Borrowed"

                        searchResultsList.Items.Add(sprintf "%s by %s [%s]" book.Title book.Author status)
                        |> ignore)

        // Borrow
        let borrowBook_Click _ =
            let title = bookTitleInput.Text.Trim()
            let memberName = memberNameInput.Text.Trim()

            if String.IsNullOrWhiteSpace(title) || String.IsNullOrWhiteSpace(memberName) then
                borrowReturnMessageLabel.Text <- "Please enter book title and member name"
                borrowReturnMessageLabel.ForeColor <- Color.Red
            else
                match books |> List.tryFind (fun b -> b.Title = title) with
                | Some book ->
                    match LibraryService.borrowBook book.Id memberName books with
                    | Ok newBooks ->
                        books <- newBooks
                        borrowReturnMessageLabel.Text <- sprintf "Book '%s' borrowed by %s" title memberName
                        borrowReturnMessageLabel.ForeColor <- Color.Green
                        bookTitleInput.Clear()
                        memberNameInput.Clear()
                        refreshBooksList()
                    | Error e ->
                        borrowReturnMessageLabel.Text <- sprintf "Error: %A" e
                        borrowReturnMessageLabel.ForeColor <- Color.Red
                | None ->
                    borrowReturnMessageLabel.Text <- "Book not found"
                    borrowReturnMessageLabel.ForeColor <- Color.Red

        // Return
        let returnBook_Click _ =
            let title = bookTitleInput.Text.Trim()

            if String.IsNullOrWhiteSpace(title) then
                borrowReturnMessageLabel.Text <- "Please enter book title"
                borrowReturnMessageLabel.ForeColor <- Color.Red
            else
                match books |> List.tryFind (fun b -> b.Title = title) with
                | Some book ->
                    books <- LibraryService.returnBook book.Id books
                    borrowReturnMessageLabel.Text <- sprintf "Book '%s' returned successfully" title
                    borrowReturnMessageLabel.ForeColor <- Color.Green
                    bookTitleInput.Clear()
                    memberNameInput.Clear()
                    refreshBooksList()
                | None ->
                    borrowReturnMessageLabel.Text <- "Book not found"
                    borrowReturnMessageLabel.ForeColor <- Color.Red

        // 🆕 NEW: Update Book
        let updateBook_Click _ =
            let bookIdStr = updateBookIdInput.Text.Trim()
            let title = updateTitleInput.Text.Trim()
            let author = updateAuthorInput.Text.Trim()
            let isbn = updateIsbnInput.Text.Trim()

            if String.IsNullOrWhiteSpace(bookIdStr) then
                deleteUpdateMessageLabel.Text <- "Please enter Book ID"
                deleteUpdateMessageLabel.ForeColor <- Color.Red
            elif String.IsNullOrWhiteSpace(title)
                || String.IsNullOrWhiteSpace(author)
                || String.IsNullOrWhiteSpace(isbn) then
                deleteUpdateMessageLabel.Text <- "Please fill in all book fields"
                deleteUpdateMessageLabel.ForeColor <- Color.Red
            else
                match Guid.TryParse(bookIdStr) with
                | true, guid ->
                    let bookId = BookId guid
                    match LibraryService.updateBook bookId title author isbn books with
                    | Ok newBooks ->
                        books <- newBooks
                        deleteUpdateMessageLabel.Text <- sprintf "Book '%s' updated successfully!" title
                        deleteUpdateMessageLabel.ForeColor <- Color.Green
                        updateTitleInput.Clear()
                        updateAuthorInput.Clear()
                        updateIsbnInput.Clear()
                        updateBookIdInput.Clear()
                        refreshBooksList()
                    | Error e ->
                        deleteUpdateMessageLabel.Text <- sprintf "Update error: %A" e
                        deleteUpdateMessageLabel.ForeColor <- Color.Red
                | _ ->
                    deleteUpdateMessageLabel.Text <- "Invalid Book ID format"
                    deleteUpdateMessageLabel.ForeColor <- Color.Red

        // 🆕 NEW: Delete Book
        let deleteBook_Click _ =
            let bookIdStr = deleteBookIdInput.Text.Trim()

            if String.IsNullOrWhiteSpace(bookIdStr) then
                deleteUpdateMessageLabel.Text <- "Please enter Book ID"
                deleteUpdateMessageLabel.ForeColor <- Color.Red
            else
                match Guid.TryParse(bookIdStr) with
                | true, guid ->
                    let bookId = BookId guid
                    match LibraryService.deleteBook bookId books with
                    | Ok newBooks ->
                        books <- newBooks
                        deleteUpdateMessageLabel.Text <- "Book deleted successfully!"
                        deleteUpdateMessageLabel.ForeColor = Color.Green
                        deleteBookIdInput.Clear()
                        refreshBooksList()
                    | Error e ->
                        deleteUpdateMessageLabel.Text <- sprintf "Delete error: %A" e
                        deleteUpdateMessageLabel.ForeColor <- Color.Red
                | _ ->
                    deleteUpdateMessageLabel.Text <- "Invalid Book ID format"
                    deleteUpdateMessageLabel.ForeColor <- Color.Red

        // Form constructor
        do
            this.Text <- "Library Management System"
            this.Size <- new Size(800, 550)
            this.StartPosition <- FormStartPosition.CenterScreen
            this.MinimumSize <- new Size(800, 550)

            // Test UI
            let testOutputLabel =
                new Label(Text = "", Location = new Point(240, 48), Size = new Size(300, 25), ForeColor = Color.Blue)

            let testButton =
                new Button(Text = "Run Tests", Location = new Point(120, 48), Size = new Size(100, 25))

            testButton.Click.Add(fun _ ->
                testOutputLabel.Text <- "Running tests..."
                try
                    let allPassed = Tests.runTests (this.GetBooks())
                    if allPassed then
                        testOutputLabel.Text <- "Tests PASSED!"
                        testOutputLabel.ForeColor <- Color.Green
                    else
                        testOutputLabel.Text <- "Tests FAILED!"
                        testOutputLabel.ForeColor <- Color.Red
                with ex ->
                    testOutputLabel.Text <- sprintf "Error: %s" ex.Message
                    testOutputLabel.ForeColor <- Color.Red
            )

            // View tab
            viewBooksTab.Controls.Add(booksListView)

            // Add tab
            addBookTab.Controls.Add(new Label(Text = "Title:", Location = new Point(20, 22), AutoSize = true))
            addBookTab.Controls.Add(titleInput)
            addBookTab.Controls.Add(new Label(Text = "Author:", Location = new Point(20, 62), AutoSize = true))
            addBookTab.Controls.Add(authorInput)
            addBookTab.Controls.Add(new Label(Text = "ISBN:", Location = new Point(20, 102), AutoSize = true))
            addBookTab.Controls.Add(isbnInput)
            addBookTab.Controls.Add(addBookButton)
            addBookTab.Controls.Add(addBookMessageLabel)
            addBookButton.Click.Add(addBook_Click)

            // Search tab
            searchTab.Controls.Add(new Label(Text = "Search by Title:", Location = new Point(20, 22), AutoSize = true))
            searchTab.Controls.Add(searchInput)
            searchTab.Controls.Add(searchButton)
            searchTab.Controls.Add(searchResultsList)
            searchButton.Click.Add(search_Click)
            searchInput.KeyDown.Add(fun e ->
                if e.KeyCode = Keys.Enter then
                    search_Click null)

            // Borrow/Return tab
            borrowReturnTab.Controls.Add(new Label(Text = "Book Title:", Location = new Point(20, 22), AutoSize = true))
            borrowReturnTab.Controls.Add(bookTitleInput)
            borrowReturnTab.Controls.Add(new Label(Text = "Member Name:", Location = new Point(20, 62), AutoSize = true))
            borrowReturnTab.Controls.Add(memberNameInput)
            borrowReturnTab.Controls.Add(borrowButton)
            borrowReturnTab.Controls.Add(returnButton)
            borrowReturnTab.Controls.Add(borrowReturnMessageLabel)
            borrowButton.Click.Add(borrowBook_Click)
            returnButton.Click.Add(returnBook_Click)

            // 🆕 NEW: Delete/Update tab
            deleteUpdateTab.Controls.Add(new Label(Text = "Update Book - Enter Book ID (GUID):", Location = new Point(20, 142), AutoSize = true))
            deleteUpdateTab.Controls.Add(updateBookIdInput)
            deleteUpdateTab.Controls.Add(new Label(Text = "Title:", Location = new Point(20, 22), AutoSize = true))
            deleteUpdateTab.Controls.Add(updateTitleInput)
            deleteUpdateTab.Controls.Add(new Label(Text = "Author:", Location = new Point(20, 62), AutoSize = true))
            deleteUpdateTab.Controls.Add(updateAuthorInput)
            deleteUpdateTab.Controls.Add(new Label(Text = "ISBN:", Location = new Point(20, 102), AutoSize = true))
            deleteUpdateTab.Controls.Add(updateIsbnInput)
            deleteUpdateTab.Controls.Add(updateButton)
            
            deleteUpdateTab.Controls.Add(new Label(Text = "Delete Book - Enter Book ID (GUID):", Location = new Point(450, 2), AutoSize = true))
            deleteUpdateTab.Controls.Add(deleteBookIdInput)
            deleteUpdateTab.Controls.Add(deleteButton)
            
            deleteUpdateTab.Controls.Add(deleteUpdateMessageLabel)
            updateButton.Click.Add(updateBook_Click)
            deleteButton.Click.Add(deleteBook_Click)

            // Tabs
            tabControl.TabPages.Add(viewBooksTab)
            tabControl.TabPages.Add(addBookTab)
            tabControl.TabPages.Add(searchTab)
            tabControl.TabPages.Add(borrowReturnTab)
            tabControl.TabPages.Add(deleteUpdateTab)  // 🆕 NEW TAB ADDED

            // Controls
            this.Controls.Add(titleLabel)
            this.Controls.Add(statusLabel)
            this.Controls.Add(testButton)
            this.Controls.Add(testOutputLabel)
            this.Controls.Add(tabControl)

            // Load books
            books <-
                match Storage.load() with
                | Ok b -> b
                | Error _ -> []

            refreshBooksList()

            tabControl.SelectedIndexChanged.Add(fun _ ->
                if tabControl.SelectedTab = viewBooksTab then
                    refreshBooksList())

            this.FormClosing.Add(fun _ ->
                match Storage.save books with
                | Ok () -> ()
                | Error err ->
                    MessageBox.Show(sprintf "Error saving: %A" err, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    |> ignore)

        // member must come after let/do
        member this.GetBooks() = books

    [<EntryPoint>]
    let main _ =
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(false)
        let form = new MainForm()
        Application.Run(form)
        0
