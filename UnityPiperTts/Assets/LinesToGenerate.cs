
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
        Line.of("JobTypeShuntingLoad", "A loading order"),
        Line.of("JobTypeShuntingUnload", "An unloading order"),
        Line.of("JobTypeTransport", "A delivery order"),
        Line.of("JobTypeEmptyHaul", "A logistical order"),
        Line.of("JobTypeComplexTransport", "A complex delivery order"),
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
        Line.of("At", "att"),
        Line.of("AtTrack", "at track"),
        Line.of("AtYard", "at the yard"),
        Line.of("In", "in"),
        Line.of("InThe", "in the"),
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
        Line.of("YardCME", "Coal Mine East."),
        Line.of("YardCMS", "Coal Mine South."),
        Line.of("YardCP", "Coal Plant."),
        Line.of("YardCS", "City South."),
        Line.of("YardCW", "City West."),
        Line.of("YardFF", "Food Factory."),
        Line.of("YardFM", "Farm Market."),
        Line.of("YardFRC", "Forest Central."),
        Line.of("YardFRS", "Forest South."),
        Line.of("YardGF", "Goods Factory."),
        Line.of("YardHB", "Harbor."),
        Line.of("YardHMB", "Harbor Military Base."),
        Line.of("YardIME", "Iron Mine East."),
        Line.of("YardIMW", "Iron Mine West."),
        Line.of("YardMB", "Military Base."),
        Line.of("YardMF", "Machine Factory."),
        Line.of("YardMFMB", "Machine Factory Military Base."),
        Line.of("YardOR", "Oil Refinery."),
        Line.of("YardOWC", "Oil Well Central."),
        Line.of("YardOWN", "Oil Well North."),
        Line.of("YardSM", "Steel Mill."),
        Line.of("YardSW", "Sawmill.")
    };
}

public class CarDescriptionLines : IListOfLines {
    public string directory => "CarDescription/";
    
    public Line[] lines => new[] {
        Line.of("ThisIsCar", "This is car"),
        Line.of("ThisCarIsBoundFor", "This car is bound for"),
        Line.of("ItIsBoundFor", "It is bound for"),
        Line.of("BoundFor", "Bound for"),
        Line.of("WaitingForUnloading", "Waiting for unloading"),
        Line.of("WaitingForLoading", "Waiting for loading"),
        Line.of("AsPartOf", "as part of"),
        Line.of("PartOf", "part of"),
        Line.of("NotPartOfAnyOrder", "not part of any order"),
        
        Line.of("CarInJob1", "Take this car."),
        Line.of("CarInJob2", "You need this car."),
        Line.of("CarInJob3", "Yes that one too."),
        
        Line.of("CarNotInJob1", "Unrelated, move on."),
        Line.of("CarNotInJob2", "Leave it alone."),
        Line.of("CarNotInJob3", "Not part of your order."),
        
    };
}

public class StationGreetingsLines : IListOfLines {
    public string directory => "StationGreetings/";
    
    public Line[] lines => new[] {
        Line.of("EnteringYard1", "Entering"),
        Line.of("EnteringYard2", "Clear to enter"),
        Line.of("EnteringYard3", "Welcome to"),
        Line.of("EnteringYard4", "Approaching"),
        Line.of("EnteringYard5", "Permission granted for"),
        
        Line.of("ExitingYard1", "Exit-ing"),
        Line.of("ExitingYard2", "Leaving"),
        Line.of("ExitingYard3", "Departing"),
        Line.of("ExitingYard4", "Safe travels from"),
        Line.of("ExitingYard5", "See you next time at"),
        
        Line.of("EnteringStation1", "Nice to see you at"),
        Line.of("EnteringStation2", "Make yourself at home in"),
        Line.of("EnteringStation3", "Enjoy your stay at"),
        Line.of("EnteringStation4", "Always work at"),
        Line.of("EnteringStation5", "Busy day at"),
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
        new StationGreetingsLines(),
        // Add more lists here as needed
    };
}
