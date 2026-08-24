using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BruteForceAttack_GSD
{
    class Program
    {
        static readonly char[] Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        static readonly char[] Lower = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        static readonly char[] Digits = "0123456789".ToCharArray();
        static readonly char[] Special = "!@#$%^&*()-_=+[]{};:,.<>/?".ToCharArray();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== Demonstração de Brute Force de Senhas ===");
            Console.WriteLine("(senha gerada aleatoriamente e depois quebrada localmente)\n");

            var testes = new (int tamanho, char[] charset, string descricao)[]
            {
                (3, Combine(Upper, Lower),                       "3 dígitos - Letras maiúsculas e minúsculas"),
                (3, Combine(Upper, Lower, Digits),                "3 dígitos - Letras + números"),
                (3, Combine(Upper, Lower, Digits, Special),       "3 dígitos - Qualquer caractere de teclado"),

                (5, Combine(Upper, Lower),                       "5 dígitos - Letras maiúsculas e minúsculas"),
                (5, Combine(Upper, Lower, Digits),                "5 dígitos - Letras + números"),
                (5, Combine(Upper, Lower, Digits, Special),       "5 dígitos - Qualquer caractere de teclado"),

                (7, Combine(Upper, Lower),                       "7 dígitos - Letras maiúsculas e minúsculas"),
                (7, Combine(Upper, Lower, Digits),                "7 dígitos - Letras + números"),
                (7, Combine(Upper, Lower, Digits, Special),       "7 dígitos - Qualquer caractere de teclado"),
            };

            foreach (var teste in testes)
            {
                ExecutarTeste(teste.tamanho, teste.charset, teste.descricao);
            }

            Console.WriteLine("\nTodos os testes finalizados. Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        static char[] Combine(params char[][] arrays)
        {
            return arrays.SelectMany(a => a).Distinct().ToArray();
        }

        static string GerarSenhaAleatoria(int tamanho, char[] charset)
        {
            var rnd = new Random();
            var sb = new StringBuilder();
            for (int i = 0; i < tamanho; i++)
                sb.Append(charset[rnd.Next(charset.Length)]);
            return sb.ToString();
        }

        static void ExecutarTeste(int tamanho, char[] charset, string descricao)
        {
            string senha = GerarSenhaAleatoria(tamanho, charset);
            double totalCombinacoes = Math.Pow(charset.Length, tamanho);

            Console.WriteLine($"--- {descricao} ---");
            Console.WriteLine($"Tamanho do alfabeto: {charset.Length} caracteres");
            Console.WriteLine($"Senha gerada (alvo secreto): {senha}");
            Console.WriteLine($"Total de combinações possíveis: {totalCombinacoes:N0}");
            Console.WriteLine("Iniciando ataque de força bruta (multi-thread)...");

            var sw = Stopwatch.StartNew();
            string encontrada = BruteForceParalelo(senha, tamanho, charset);
            sw.Stop();

            Console.WriteLine($"Senha quebrada: {encontrada}");
            Console.WriteLine($"Tempo decorrido: {FormatarTempo(sw.Elapsed)}");
            Console.WriteLine();
        }

        // Divide o espaço de busca pelo primeiro caractere e distribui entre threads.
        // Cada thread faz busca sequencial (contador em base N) dentro da sua fatia.
        static string BruteForceParalelo(string senhaAlvo, int tamanho, char[] charset)
        {
            int totalChars = charset.Length;
            string resultado = null;
            var cts = new CancellationTokenSource();
            object lockObj = new object();

            try
            {
                Parallel.For(0, totalChars, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cts.Token
                }, (primeiroIndice, state) =>
                {
                    if (tamanho == 1)
                    {
                        string tentativaUnica = charset[primeiroIndice].ToString();
                        if (tentativaUnica == senhaAlvo)
                        {
                            lock (lockObj) { resultado = tentativaUnica; }
                            cts.Cancel();
                        }
                        return;
                    }

                    int restante = tamanho - 1;
                    int[] indices = new int[restante];
                    char[] tentativa = new char[tamanho];
                    tentativa[0] = charset[primeiroIndice];

                    while (!cts.IsCancellationRequested)
                    {
                        for (int i = 0; i < restante; i++)
                            tentativa[i + 1] = charset[indices[i]];

                        string tentativaStr = new string(tentativa);
                        if (tentativaStr == senhaAlvo)
                        {
                            lock (lockObj) { resultado = tentativaStr; }
                            cts.Cancel();
                            return;
                        }

                        int pos = restante - 1;
                        while (pos >= 0)
                        {
                            indices[pos]++;
                            if (indices[pos] < totalChars)
                                break;
                            indices[pos] = 0;
                            pos--;
                        }
                        if (pos < 0)
                            break; // esgotou as combinações desta fatia
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Cancelamento esperado quando a senha é encontrada; ignorar
            }

            return resultado;
        }

        static string FormatarTempo(TimeSpan ts)
        {
            if (ts.TotalSeconds < 1)
                return $"{ts.TotalMilliseconds:N2} ms";
            if (ts.TotalMinutes < 1)
                return $"{ts.TotalSeconds:N2} s";
            if (ts.TotalHours < 1)
                return $"{ts.Minutes} min {ts.Seconds} s";
            if (ts.TotalDays < 1)
                return $"{ts.TotalHours:N2} horas";
            return $"{ts.TotalDays:N2} dias";
        }
    }
}