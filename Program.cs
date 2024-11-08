using System.Runtime.InteropServices.JavaScript;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json;
using minimal_azurite.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

string connectionString = builder.Configuration["AzureStorage:ConnectionString"];

var app = builder.Build();

// Endpoint para verificar se a API está online
app.MapGet("/", () => "Hello World!");

// 1. Adicionar um novo livro - ok
app.MapPost("/api/books", async (Book book) =>
{
    
    var queueClient = new QueueClient(connectionString, "books-queue");
    var tableServiceClient = new TableServiceClient(connectionString);

    await queueClient.CreateIfNotExistsAsync();
    await queueClient.SendMessageAsync(JsonSerializer.Serialize(book));

    var tableClient = tableServiceClient.GetTableClient("BooksTable");
    await tableClient.CreateIfNotExistsAsync();

    var tableEntity = new TableEntity("Book", book.ISBN)
    {
        // Serializa o objeto Book como JSON e armazena-o no campo "BookData"
        { "BookData", JsonSerializer.Serialize(new
            {
                isbn = book.ISBN,
                tipo_livro = book.TipoLivro,
                estante = book.Estante,
                idioma = book.Idioma,
                titulo = book.Titulo,
                autor = book.Autor,
                editora = book.Editora,
                ano = book.Ano,
                edicao = book.Edicao,
                preco = book.Preco,
                peso = book.Peso,
                descricao = book.Descricao,
                capa = book.Capa
            })
        }
    };

    await tableClient.AddEntityAsync(tableEntity);

    return Results.Created($"/api/books/{book.ISBN}", book);
});

// 2. Upload de imagem da capa do livro
app.MapPost("/api/books/{isbn}/upload", async (string isbn, IFormFile coverImage) =>
{
    var blobServiceClient = new BlobServiceClient(connectionString);
    var blobContainerClient = blobServiceClient.GetBlobContainerClient("book-covers");
    await blobContainerClient.CreateIfNotExistsAsync();

    var blobClient = blobContainerClient.GetBlobClient(isbn);
    using (var stream = coverImage.OpenReadStream())
    {
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    var coverImageUrl = blobClient.Uri.ToString();

    var tableServiceClient = new TableServiceClient(connectionString);
    var tableClient = tableServiceClient.GetTableClient("BooksTable");
    var entity = await tableClient.GetEntityAsync<TableEntity>("Book", isbn);


//    await tableClient.UpdateEntityAsync(entity, isbn);

    return Results.Ok(new { CoverImageUrl = coverImageUrl });
});

// 3. Retorna todos os livros - ok
app.MapGet("/api/books", async () =>
{
    var tableServiceClient = new TableServiceClient(connectionString);
    var tableClient = tableServiceClient.GetTableClient("BooksTable");

    var books = new List<Book>();

    try
    {
        // Itera sobre as entidades da tabela de forma assíncrona
        await foreach (var entity in tableClient.QueryAsync<TableEntity>(filter: "PartitionKey eq 'Book'"))
        {
            // Extrai o campo JSON (se existir) e desserializa em um objeto `Book`
            if (entity.TryGetValue("BookData", out var bookDataJson) && bookDataJson is string bookDataString)
            {
                var book = JsonSerializer.Deserialize<Book>(bookDataString);
                if (book != null)
                {
                    books.Add(book);
                }
            }
        }

        return Results.Ok(books);
    }
    catch (Azure.RequestFailedException ex)
    {
        return Results.Problem($"Erro ao acessar a tabela: {ex.Message}");
    }
});

// 4. Retorna um livro específico pelo ISBN --ok
app.MapGet("/api/books/{isbn}", async (string isbn) =>
{
    var tableServiceClient = new TableServiceClient(connectionString);
    var tableClient = tableServiceClient.GetTableClient("BooksTable");

    try
    {
        // Obtém a entidade diretamente usando GetEntityAsync
        var entity = await tableClient.GetEntityAsync<TableEntity>("Book", isbn);

        // Acessa o campo "BookData" e verifica se ele contém JSON
        if (entity.Value.TryGetValue("BookData", out var bookDataJson) && bookDataJson is string bookDataString)
        {
            // Desserializa o JSON para um objeto do tipo `Book`
            var book = JsonSerializer.Deserialize<Book>(bookDataString);

            return book is not null ? Results.Ok(book) : Results.NotFound("Formato inválido no campo BookData.");
        }
        else
        {
            return Results.NotFound("Campo 'BookData' não encontrado.");
        }
    }
    catch (Azure.RequestFailedException ex) when (ex.Status == 404)
    {
        return Results.NotFound();
    }
});
// 5. Atualiza os dados de um livro - ok
app.MapPut("/api/books/{isbn}", async (string isbn, Book updatedBook) =>
{
    var tableServiceClient = new TableServiceClient(connectionString);
    var tableClient = tableServiceClient.GetTableClient("BooksTable");

    var tableEntity = new TableEntity("Book", updatedBook.ISBN)
    {
        // Serializa o objeto Book como JSON e armazena-o no campo "BookData"
        { "BookData", JsonSerializer.Serialize(new
            {
                isbn = updatedBook.ISBN,
                tipo_livro = updatedBook.TipoLivro,
                estante = updatedBook.Estante,
                idioma = updatedBook.Idioma,
                titulo = updatedBook.Titulo,
                autor = updatedBook.Autor,
                editora = updatedBook.Editora,
                ano = updatedBook.Ano,
                edicao = updatedBook.Edicao,
                preco = updatedBook.Preco,
                peso = updatedBook.Peso,
                descricao = updatedBook.Descricao,
                capa = updatedBook.Capa
            })
        }
    };

    await tableClient.UpsertEntityAsync(tableEntity);
    return Results.Ok(updatedBook);
});

// 6. Remove um livro  - ok
app.MapDelete("/api/books/{isbn}", async (string isbn) =>
{
    var tableServiceClient = new TableServiceClient(connectionString);
    var tableClient = tableServiceClient.GetTableClient("BooksTable");

    try
    {
        await tableClient.DeleteEntityAsync("Book", isbn);
        return Results.NoContent();
    }
    catch (Azure.RequestFailedException ex) when (ex.Status == 404)
    {
        return Results.NotFound();
    }
});

app.Run();