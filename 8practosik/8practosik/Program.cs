using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class User
{
    public string Name { get; set; }
    public int CharactersPerMinute { get; set; }
}

public class TypingTest
{
    private static string testText = "страна как знать пойти дом к должный раз пойти мой видеть чем лишь во ты про перед каждый выйти здесь быть нога бывать себя там";
    private static int currentIndex;
    private static Stopwatch stopwatch;
    private static bool typingFinished;
    private static readonly object lockObject = new object();
    private static List<User> leaderBoard = new List<User>();

    private const string leaderboardFilePath = "leaderboard.json";

    public static async Task StartTestAsync(string userName)
    {
        Console.Clear();

        Console.WriteLine($"Добро пожаловать, {userName}! Приготовьтесь к тесту на скоропечатание.");
        Console.WriteLine("Нажмите Enter, чтобы начать печатать.");

        Console.ReadLine();

        Console.Clear();
        Console.WriteLine($"Введите следующий текст:\n\n{testText}\n");

        currentIndex = 0;
        stopwatch = Stopwatch.StartNew();
        typingFinished = false;

        Task timerTask = StartTimerAsync();
        Task typingTask = TypingTaskAsync();

        await Task.WhenAll(timerTask, typingTask);

        stopwatch.Stop();

        Console.Clear();
        Console.WriteLine("Тест завершен!\n");

        int charactersPerMinute = (int)(currentIndex / stopwatch.Elapsed.TotalMinutes);
        Console.WriteLine($"Ваша скорость печати: {charactersPerMinute} знаков в минуту");

        SaveUserResult(userName, charactersPerMinute);

        Console.WriteLine("\nТаблица лидеров:\n");
        DisplayLeaderboard();

        Console.WriteLine("\nНажмите Enter, чтобы продолжить печать.");
        Console.ReadLine();
    }

    private static async Task TypingTaskAsync()
    {
        Console.CursorVisible = false;

        Console.SetCursorPosition(0, 2);
        Console.Write(testText);

        Console.SetCursorPosition(currentIndex, 2);

        while (!typingFinished && stopwatch.Elapsed.TotalSeconds < 30)
        {
            ConsoleKeyInfo keyInfo = await ReadKeyAsync();

            lock (lockObject)
            {
                if (currentIndex < testText.Length)
                {
                    char expectedChar = testText[currentIndex];

                    if (keyInfo.KeyChar == expectedChar)
                    {
                        Console.SetCursorPosition(currentIndex, 2);
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(keyInfo.KeyChar);
                        Console.ResetColor();
                        currentIndex++;
                    }
                    else
                    {
                        Console.SetCursorPosition(currentIndex, 2);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(expectedChar);
                        Console.ResetColor();
                    }
                }

                if (currentIndex == testText.Length)
                {
                    typingFinished = true;
                }
            }
        }
    }

    private static async Task StartTimerAsync()
    {
        while (!typingFinished && stopwatch.Elapsed.TotalSeconds < 30)
        {
            Console.SetCursorPosition(0, 4);
            Console.Write($"Прошло времени: {stopwatch.Elapsed.Seconds} секунд");
            await Task.Delay(1000);
        }

        typingFinished = true;
    }

    private static Task<ConsoleKeyInfo> ReadKeyAsync()
    {
        var tcs = new TaskCompletionSource<ConsoleKeyInfo>();
        Thread backgroundThread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(Console.ReadKey(true));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        backgroundThread.Start();
        return tcs.Task;
    }

    private static void SaveUserResult(string userName, int charactersPerMinute)
    {
        List<User> users = LoadUsers();
        User user = users.FirstOrDefault(u => u.Name == userName);

        if (user == null)
        {
            user = new User { Name = userName };
            users.Add(user);
        }

        user.CharactersPerMinute = charactersPerMinute;

        SaveUsers(users);
    }

    private static List<User> LoadUsers()
    {
        if (File.Exists(leaderboardFilePath))
        {
            string json = File.ReadAllText(leaderboardFilePath);
            return JsonSerializer.Deserialize<List<User>>(json);
        }

        return new List<User>();
    }

    private static void SaveUsers(List<User> users)
    {
        string json = JsonSerializer.Serialize(users);
        File.WriteAllText(leaderboardFilePath, json);
    }

    private static void DisplayLeaderboard()
    {
        leaderBoard = LoadUsers().OrderByDescending(u => u.CharactersPerMinute).ToList();

        Console.WriteLine("{0,-15} {1,-25}", "Имя", "Знаков в минуту");

        foreach (var user in leaderBoard)
        {
            Console.WriteLine($"{user.Name,-15} {user.CharactersPerMinute,-25}");
        }
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Введите ваше имя:");
        string userName = Console.ReadLine();

        await TypingTest.StartTestAsync(userName);

        Console.WriteLine("Нажмите Enter для выхода.");
        Console.ReadLine();
    }
}
