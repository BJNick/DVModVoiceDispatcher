
using System.Linq;

public struct Line
{
    public string text;
    public string filename;

    public Line(string text, string filename)
    {
        this.text = text;
        this.filename = filename;
    }
    
    public static Line of(string text) {
        string filename = text.Replace(" ", "_").ToLower() + ".wav";
        return new Line(text, filename);
    }

    public static Line of(string filename, string text) {
        return new Line(text, filename);
    }
}

public interface IListOfLines {
    Line[] lines { get; }
    string directory => "";
}

public class JobTypeLines : IListOfLines {
    public string directory => "JobType/";
    public Line[] lines => new[] {
        Line.of("JobTypeCustom", "A custom order"),
        Line.of("JobTypeShuntingLoad", "A shunting load order"),
        Line.of("JobTypeShuntingUnload", "A shunting unload order"),
        Line.of("JobTypeTransport", "A freight haul order"),
        Line.of("JobTypeEmptyHaul", "A logistic haul order"),
        Line.of("JobTypeComplexTransport", "A complex transport order"),
    };
}

public class Numbers : IListOfLines {
    public string directory => "Number/";
    public Line[] lines => new[] {
        Line.of("0", "Zero."),
        Line.of("1", "One."),
        Line.of("2", "Two."),
        Line.of("3", "Three."),
        Line.of("4", "Four."),
        Line.of("5", "Five."),
        Line.of("6", "Six."),
        Line.of("7", "Seven."),
        Line.of("8", "Eight."),
        Line.of("9", "Nine."),
        Line.of("10", "Ten."),
        Line.of("11", "Eleven."),
        Line.of("12", "Twelve."),
    };
}

public class CarsNumbers : IListOfLines {
    public string directory => "CarsNumber/";
    public Line[] lines => new[] {
        Line.of("0Cars", "No cars"),
        Line.of("1Cars", "One car"),
        Line.of("2Cars", "Two cars"),
        Line.of("3Cars", "Three cars"),
        Line.of("4Cars", "Four cars"),
        Line.of("5Cars", "Five cars"),
        Line.of("6Cars", "Six cars"),
        Line.of("7Cars", "Seven cars"),
        Line.of("8Cars", "Eight cars"),
        Line.of("9Cars", "Nine cars"),
        Line.of("10Cars", "Ten cars"),
        Line.of("11Cars", "Eleven cars"),
        Line.of("12Cars", "Twelve cars"),
    };
}

public class PhoneticAlphabet : IListOfLines {
    public string directory => "IPA/";
    public Line[] lines => new[] {
        Line.of("A", "Alpha."),
        Line.of("B", "Bravo."),
        Line.of("C", "Charlie."),
        Line.of("D", "Delta."),
        Line.of("E", "Echo."),
        Line.of("F", "Foxtrot."),
        Line.of("G", "Golf."),
        Line.of("H", "Hotel."),
        Line.of("I", "India."),
        Line.of("J", "Juliett."),
        Line.of("K", "Kilo."),
        Line.of("L", "Lima."),
        Line.of("M", "Mike."),
        Line.of("N", "November."),
        Line.of("O", "Oscar."),
        Line.of("P", "Papa."),
        Line.of("Q", "Quebec."),
        Line.of("R", "Romeo."),
        Line.of("S", "Sierra."),
        Line.of("T", "Tango."),
        Line.of("U", "Uniform."),
        Line.of("V", "Victor."),
        Line.of("W", "Whiskey."),
        Line.of("X", "X-ray."),
        Line.of("Y", "Yankee."),
        Line.of("Z", "Zulu.")
    };
}

public class JobDescriptionLines : IListOfLines {
    public string directory => "JobDescription/";

    public Line[] lines => new[] {
        Line.of("YouHaveA", "You have a"),
        Line.of("YouHave", "You have"),
        Line.of("Orders", "orders"),
        Line.of("Move", "Move"),
        Line.of("Load", "Load"),
        Line.of("Unload", "Unload"),
        Line.of("Couple", "Couple"),
        Line.of("PickUp", "Pick up"),
        Line.of("Uncouple", "Uncouple"),
        Line.of("Cars", "cars"),
        Line.of("Car", "car"),
        Line.of("To", "to"),
        Line.of("ToTrack", "to track"),
        Line.of("FromTrack", "from track"),
        Line.of("At", "at"),
        Line.of("AtTrack", "at track"),
        Line.of("AtYard", "at the yard"),
        Line.of("InYard", "in the yard"),
        Line.of("Track", "track"),
        Line.of("ForUnloading", "for unloading."),
        Line.of("ForLoading", "for loading."),
        Line.of("ThenUncouple", "then uncouple"),
        Line.of("ThenMove", "then move"),
        Line.of("ThenMoveThoseCars", "then move those cars"),
        Line.of("ThenUnloadThoseCars", "then, unload those cars"),
        Line.of("ThenDropOffThoseCars", "then drop off those cars"),
        Line.of("ForDeparture", "for departure."),
        Line.of("ForStorage", "for storage."),
        Line.of("ToCompleteTheOrder", "to complete the order."),
        Line.of("And", "and"),
        Line.of("Or", "or"),
    };
}

public class TrackTypeLines : IListOfLines {
    public string directory => "TrackType/";
    
    public Line[] basicLines => new[] {
        Line.of("TrackTypeS", "storage track."),
        Line.of("TrackTypeL", "loading track."),
        Line.of("TrackTypeI", "inbound track."),
        Line.of("TrackTypeO", "outbound track."),
        Line.of("TrackTypeP", "parking track."),
        Line.of("TrackTypeM", "main line track."),
        Line.of("TrackTypeSP", "passenger storage track."),
        Line.of("TrackTypeLP", "passenger loading track."),
    };
    
    private static string[] prefixes = { "To", "At", "From" };

    private Line[] PrefixedLines() =>
        prefixes
            .SelectMany(prefix =>
                basicLines.Select(line =>
                    Line.of($"{prefix}{line.filename}", $"{prefix} {line.text}")
                )
            ).ToArray();

    public Line[] lines => PrefixedLines().ToArray();
}

public class YardNames : IListOfLines {
    public string directory => "YardName/";
    // Prefix with "Yard" to match the naming convention
    public Line[] lines => new[] {
        Line.of("YardCME", "Coal Mine East"),
        Line.of("YardCMS", "Coal Mine South"),
        Line.of("YardCP", "Coal Power Plant"),
        Line.of("YardCS", "City South"),
        Line.of("YardCW", "City West"),
        Line.of("YardFF", "Food Factory and Town"),
        Line.of("YardFM", "Farm Market"), // Originally "Farm"
        Line.of("YardFRC", "Forest Central"),
        Line.of("YardFRS", "Forest South"),
        Line.of("YardGF", "Goods Factory and Town"),
        Line.of("YardHB", "Harbor and Town"),
        Line.of("YardIME", "Iron Ore Mine East"),
        Line.of("YardIMW", "Iron Ore Mine West"),
        Line.of("YardMB", "Military Base"),
        Line.of("YardMF", "Machine Factory and Town"),
        Line.of("YardOR", "Oil Refinery"),
        Line.of("YardOWC", "Oil Well Central"),
        Line.of("YardOWN", "Oil Well North"),
        Line.of("YardSM", "Steel Mill"),
        Line.of("YardSW", "Sawmill")
    };
}

public class CarDescriptionLines : IListOfLines {
    public string directory => "CarDescription/";
    
    public Line[] lines => new[] {
        Line.of("ThisIsCar", "This is car"),
    };
}

public class AllLines {
    public static IListOfLines[] lists = new IListOfLines[] {
        new JobTypeLines(),
        new Numbers(),
        new CarsNumbers(),
        new PhoneticAlphabet(),
        new JobDescriptionLines(),
        new TrackTypeLines(),
        new CarDescriptionLines(),
        new YardNames(),
        // Add more lists here as needed
    };
}
