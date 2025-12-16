using simple_console_RPG;

//using Internal;
public class CharacterStats
{
    // character basics
    public string Name { get; set; } = null!;

    public int StartingClass { get; set; }

    public string CharacterClass { get; set; } = null!;

    public int StartingLevel { get; set; }

    // attributes
    public int Health { get; set; }

    public int HP { get; set; }

    public int Speed { get; set; }

    public int Strength { get; set; }

    public int Range { get; set; }

    public int Magic { get; set; }

    public int Luck { get; set; }

    public int Level { get; set; }

    public int EXP { get; set; }

    public int Dice { get; set; }

    public int DiceValue { get; set; }

    public string? RollDiceChoice { get; set; }

    public void PrintOptions()
    {
        Task.Delay(1000).Wait();
        Console.WriteLine("Choose your starting class...");
        Task.Delay(1000).Wait();
        Console.WriteLine("1. Elf - +4 speed, +2 strength,");
        Task.Delay(1000).Wait();
        Console.WriteLine("2. Mage - +6 magic"); // magic;
        Task.Delay(1000).Wait();
        Console.WriteLine("3. Hero - +5 strength, +2 speed"); // strength; speed;
        Task.Delay(1000).Wait();
        Console.WriteLine("4. Hobbit - +6 luck"); // luck;
        Task.Delay(1000).Wait();
        Console.WriteLine("5. Dwarf - +8 strength;"); // strengh;
        Task.Delay(1000).Wait();
        Console.WriteLine("6. Barbarian - +1 HP, +5 strength"); // hp; strength;
        Task.Delay(1000).Wait();
        Console.WriteLine("7. Theif - +5 speed, +1 luck"); // speed; luck;
        Console.WriteLine("Enter your choice: ");
        string? input = Console.ReadLine() ?? string.Empty;
        if (int.TryParse(input, out int classChoice))
        {
            StartingClass = classChoice;
        }
        else
        {
            StartingClass = 1; // Default to Elf
        }
        Task.Delay(1000);
        Console.WriteLine("\n");
    }

    public async Task<PlayerStats> CreateCharacter()
    {
        Console.WriteLine("Enter your character name ");
        Name = Console.ReadLine() ?? "Adventurer";
        Console.WriteLine("\n");

        Task.Delay(1000).Wait();
        Console.WriteLine($"Welcome {Name}! \n");
        PrintOptions();

        // Create PlayerStats based on class selection
        PlayerStats playerStats = CreateClassStats();
        if (playerStats == null)
        {
            Console.WriteLine("Invalid class selection, defaulting to Elf");
            StartingClass = 1;
            playerStats = CreateClassStats();
        }

        // Stat point distribution
        var pointsToSpend = 10;
        int pointsRemaining = 10;
        var stats = new[] { "Health", "Speed", "Strength", "Magic", "Luck" };
        var statsValues = new int[stats.Length];

        Task.Delay(1000).Wait();
        Console.WriteLine($"You have {pointsToSpend} points to spend. \n");
        Task.Delay(2000).Wait();
        Console.WriteLine("These are your stats!");
        Task.Delay(3000).Wait();
        Console.WriteLine($"Health: {playerStats.Health}");
        Console.WriteLine($"Speed: {playerStats.Speed}");
        Console.WriteLine($"Strength: {playerStats.Strength}");
        Console.WriteLine($"Magic: {playerStats.Magic}");
        Console.WriteLine($"Luck: {playerStats.Luck}");

        for (int i = 0; i < stats.Length; i++)
        {
            Console.Write($"{stats[i]}: ");
            string? input = Console.ReadLine();
            if (int.TryParse(input, out int value))
            {
                statsValues[i] += value;
            }
            pointsRemaining = pointsToSpend -= statsValues[i];

            if (pointsRemaining == 0 || pointsRemaining < 0)
            {
                Console.WriteLine($"You have ran out of points \n");
                break;
            }

            Console.WriteLine($"Remaining points to spend: {pointsRemaining} \n");
        }

        playerStats.UpdateStats(statsValues, Name, CharacterClass);

        Task.Delay(1000).Wait();
        Console.WriteLine("These are your stats! \n");
        Task.Delay(1000).Wait();
        Console.WriteLine($"Health: {playerStats.Health}");
        Task.Delay(1000).Wait();
        Console.WriteLine($"Speed: {playerStats.Speed}");
        Task.Delay(1000).Wait();
        Console.WriteLine($"Strength: {playerStats.Strength}");
        Task.Delay(1000).Wait();
        Console.WriteLine($"Magic: {playerStats.Magic}");
        Task.Delay(1000).Wait();
        Console.WriteLine($"Luck: {playerStats.Luck} \n");

        RollDiceOptions();
        Task.Delay(1000).Wait();
        _ = RollDice();
        _ = RollDice();
        _ = RollDice();

        Console.WriteLine("one moment while we create your story... \n");

        return playerStats;
    }

    private PlayerStats? CreateClassStats()
    {
        var stats = StartingClass switch
        {
            1 => new PlayerStats { Speed = 4, Strength = 2 },
            2 => new PlayerStats { Magic = 6 },
            3 => new PlayerStats { Strength = 5, Speed = 2 },
            4 => new PlayerStats { Luck = 6 },
            5 => new PlayerStats { Strength = 8 },
            6 => new PlayerStats { Health = 1, Strength = 5 },
            7 => new PlayerStats { Speed = 5, Luck = 1 },
            _ => null
        };
        
        if (stats != null)
        {
            CharacterClass = StartingClass switch
            {
                1 => "Elf",
                2 => "Mage",
                3 => "Hero",
                4 => "Hobbit",
                5 => "Dwarf",
                6 => "Barbarian",
                7 => "Thief",
                _ => "Unknown"
            };
            Console.WriteLine($"You have chosen {CharacterClass}! \n");
        }
        
        return stats;
    }

    public void RollDiceOptions()
    {
        Console.WriteLine("Would you like to roll the dice...");
        Console.WriteLine("2 - for yes");
        Console.WriteLine("1 - for no");
        RollDiceChoice = Console.ReadLine() ?? string.Empty;
        if (RollDiceChoice == "2")
        {
            Console.WriteLine("\n");
        }
        else
        {
            Console.WriteLine("you suck");
        }
    }

    public int RollDice()
    {
        return new Random().Next(0, 9);
    }
}
