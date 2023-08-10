using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

public class TextHunt
{
    private static CancellationTokenSource cancellationTokenSource;
    private static Random random = new Random();
    private static object lockObject = new object();

    public static async Task FindRandTextParallel(string SearchedWord, char[] characterSet, CancellationToken cancellationToken)
    {
        BigInteger totalAttempts = 0;
        Stopwatch stopwatch = new Stopwatch();

        BigInteger theoreticalProbability = BigInteger.Pow(characterSet.Length + 1, SearchedWord.Length);

        string formattedTheoreticalProbability = FormatNumberWithDots(theoreticalProbability);

        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"Theoretical probability: 1/{formattedTheoreticalProbability}");
        Console.ResetColor();

        stopwatch.Start();

        await Task.Run(() =>
        {
            Parallel.ForEach(SplitIntoChunks(SearchedWord, 100), (subString) =>
            {
                BigInteger localAttempts = FindSubstring(subString, characterSet, cancellationToken);

                lock (lockObject)
                {
                    totalAttempts += localAttempts;
                }
            });
        });

        double totalTime = stopwatch.Elapsed.TotalSeconds;

        if (totalAttempts > 0)
        {
            double averageAttemptsPerCharacter = (double)totalAttempts / SearchedWord.Length;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Average attempts per character: {averageAttemptsPerCharacter:F2}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Total time taken: {totalTime:F2} seconds");
        Console.ResetColor();
    }

    public static BigInteger FindSubstring(string substring, char[] characterSet, CancellationToken cancellationToken)
    {
        BigInteger localAttempts = 0;

        foreach (char targetChar in substring)
        {
            BigInteger attemptCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                attemptCount++;
                char randomChar = characterSet[random.Next(characterSet.Length)];

                if (randomChar == targetChar)
                {
                    localAttempts += attemptCount;
                    break;
                }
            }
        }

        return localAttempts;
    }

    public static string[] SplitIntoChunks(string text, int chunkSize)
    {
        BigInteger length = text.Length;
        BigInteger numOfChunks = (length + chunkSize - 1) / chunkSize;
        string[] chunks = new string[(int)numOfChunks];

        for (int i = 0; i < (int)numOfChunks; i++)
        {
            int startIndex = i * chunkSize;
            int endIndex = (int)Math.Min(startIndex + chunkSize, (int)length);
            chunks[i] = text.Substring(startIndex, endIndex - startIndex);
        }

        return chunks;
    }

    public static void Main()
    {
        char[] characterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxy0123456789ÇçĞğİıİıÖöŞşÜü!@#$%^&*()_+-={}[]|:;'<>,.?/`~ ".ToCharArray();

        while (true)
        {
            Console.WriteLine("Please enter the combination you would like to search for, or type 'exit' to quit.");
            string SearchedWord = Console.ReadLine();

            if (SearchedWord.ToLower() == "exit")
            {
                break;
            }

            cancellationTokenSource = new CancellationTokenSource();

            Console.WriteLine("");
            Task.Run(async () =>
            {
                await FindRandTextParallel(SearchedWord, characterSet, cancellationTokenSource.Token);
            }).Wait();
        }
    }

    public static string FormatNumberWithDots(BigInteger number)
    {
        string numberString = number.ToString();
        int groupSize = 3;
        int length = numberString.Length;

        if (length <= groupSize)
        {
            return numberString;
        }

        string formattedNumber = numberString.Substring(0, length % groupSize);

        for (int i = length % groupSize; i < length; i += groupSize)
        {
            if (i != 0)
            {
                formattedNumber += ".";
            }

            formattedNumber += numberString.Substring(i, Math.Min(groupSize, length - i));
        }

        return formattedNumber;
    }
}