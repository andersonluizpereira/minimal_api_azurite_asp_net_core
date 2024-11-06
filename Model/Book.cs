using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace minimal_azurite.Model;

public class Book
{
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } // Identificador único do livro

    [JsonPropertyName("tipo_livro")]
    public string TipoLivro { get; set; } // Tipo do livro (ex.: Novo, Usado)

    [JsonPropertyName("estante")]
    public string Estante { get; set; } // Categoria ou estante onde o livro está localizado

    [JsonPropertyName("idioma")]
    public string Idioma { get; set; } // Idioma do livro

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } // Título do livro

    [JsonPropertyName("autor")]
    public string Autor { get; set; } // Autor do livro

    [JsonPropertyName("editora")]
    public string Editora { get; set; } // Editora do livro

    [JsonPropertyName("ano")]
    public int Ano { get; set; } // Ano de publicação

    [JsonPropertyName("edicao")]
    public int Edicao { get; set; } // Edição do livro

    [JsonPropertyName("preco")]
    public double Preco { get; set; } // Preço do livro

    [JsonPropertyName("peso")]
    public int Peso { get; set; } // Peso do livro em gramas

    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } // Descrição do livro

    [JsonPropertyName("capa")]
    public string Capa { get; set; } // URL da imagem de capa do livro

    // Método de validação do ISBN
    public bool IsValidISBN()
    {
        return IsValidISBN10(ISBN) || IsValidISBN13(ISBN);
    }

    private bool IsValidISBN10(string isbn)
    {
        isbn = isbn.Replace("-", "").Replace(" ", "");
        
        // Verifica o formato: exatamente 10 caracteres
        if (isbn.Length != 10 || !Regex.IsMatch(isbn, @"^\d{9}[\dX]$"))
            return false;

        // Cálculo do dígito de verificação do ISBN-10
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            if (!int.TryParse(isbn[i].ToString(), out int digit))
                return false;
            sum += (10 - i) * digit;
        }

        // Verificação do último dígito (pode ser 'X' para representar 10)
        char lastChar = isbn[9];
        sum += lastChar == 'X' ? 10 : (int)char.GetNumericValue(lastChar);

        // O ISBN-10 é válido se a soma for divisível por 11
        return sum % 11 == 0;
    }

    private bool IsValidISBN13(string isbn)
    {
        isbn = isbn.Replace("-", "").Replace(" ", "");
        
        // Verifica o formato: exatamente 13 caracteres
        if (isbn.Length != 13 || !Regex.IsMatch(isbn, @"^\d{13}$"))
            return false;

        // Cálculo do dígito de verificação do ISBN-13
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            if (!int.TryParse(isbn[i].ToString(), out int digit))
                return false;
            sum += (i % 2 == 0 ? 1 : 3) * digit;
        }

        // Cálculo do dígito verificador
        int checkDigit = 10 - (sum % 10);
        if (checkDigit == 10) checkDigit = 0;

        // O ISBN-13 é válido se o último dígito for igual ao dígito verificador calculado
        return checkDigit == (int)char.GetNumericValue(isbn[12]);
    }
}