using ConsoleApp1;
using ConsoleApp1.Helpers;
using ConsoleApp1.Modesl;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Program
{
    public static Dictionary<int, int> CountersDictionary = new();
    public static Dictionary<PartResult, int> PartResultCountersDictionary = new();

    public static void UpdateCountersDictionary<T>(Dictionary<T, int> dictionary, T key)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary[key] = 1;
        }
        else
        {
            dictionary[key]++;
        }
    }

    public static void DisplayDictionary<T>(Dictionary<T, int> dictionary)
    {
        foreach (var keyValuePair in dictionary)
        {
            Console.WriteLine(keyValuePair.Key + " -> " + keyValuePair.Value);
        }
    }


    static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:x2} ", b);
        return hex.ToString();
    }

    static Tuple<Part, PartResult> ParseLineArray(byte[] lineArray, byte[] previousArray, Part previousPart)
    {

        var partResult = PartResult.Success;
        var partNumberRegex = new Regex("[^a-zA-Z0-9 -]");

        var splitLine = ArrayHelpers.SplitArray(lineArray, new byte[] { 0x81, 0x8B, 0x34, 0x01 });
        UpdateCountersDictionary(CountersDictionary, splitLine.Length);

        if (splitLine.Length == 1 && previousArray != null)
        {

            splitLine = new byte[][] { null, splitLine[0] };
            var previousSplitLine = ArrayHelpers.SplitArray(previousArray, new byte[] { 0x81, 0x8B, 0x34, 0x01 });

            if (previousSplitLine.Length == 1 && previousPart == null)
            {
                partResult = PartResult.PreviousLineJoinSuccess;
                splitLine[0] = previousSplitLine[0];
            }
            else
            {
                partResult = PartResult.NoPreviousLineJoinFailure;
                return new Tuple<Part, PartResult>(null, partResult);
            }
        }
        else if (splitLine.Length != 2 || previousArray == null)
        {
            partResult = PartResult.Failure;
            return new Tuple<Part, PartResult>(null, partResult);
        }
        var partContainingNumberPart = Encoding.UTF8.GetString(splitLine[0]);
        var partNumber = partNumberRegex.Replace(partContainingNumberPart, "");
        var priceAsHex = BitConverter.ToString(splitLine[1].SubArray(0, 4).Reverse().ToArray()).Replace("-", string.Empty);
        var price = Convert.ToInt32(priceAsHex, 16) / 100d;

        Console.WriteLine(lineArray.Length);
        var discountGroup = Encoding.UTF8.GetString(new byte[] { splitLine[1][5] });
        Console.WriteLine(partNumber);
        Console.WriteLine(discountGroup);
        Console.WriteLine(price);
        return new Tuple<Part, PartResult>(new Part
        {
            Number = $"\t{partNumber}",
            DiscountGroup = discountGroup,
            Price = price,
            PriceStartDate = DateTime.UtcNow
        },
        partResult);
    }


    public static void Main(string[] args)
    {
        var inputBytes = File.ReadAllBytes("FPREIS.BIN");
        byte[] delimeter = { 0x01, 0xF4 };
        var splitInput = ArrayHelpers.SplitArray(inputBytes, delimeter);


        var parts = new List<Part>();
        var uprocessedLines = new List<string>();
        int processedLinesCounter = 0, unproccessedLinesCounter = 0;
        Part deserializedPart;
        PartResult partResult;
        byte[] previousArray = null;
        Part previousPart = null;
        foreach (var array in splitInput)
        {

            (deserializedPart, partResult) = ParseLineArray(array, previousArray, previousPart);
            UpdateCountersDictionary(PartResultCountersDictionary, partResult);
            if (deserializedPart != null)
            {
                parts.Add(deserializedPart);
            }
            else
            {
                uprocessedLines.Add(Environment.NewLine);
                uprocessedLines.Add(Encoding.UTF8.GetString(array));
                uprocessedLines.Add(Environment.NewLine);
                unproccessedLinesCounter++;
                Console.WriteLine(unproccessedLinesCounter);
                uprocessedLines.Add(Encoding.UTF8.GetString(array));
            }
            processedLinesCounter++;


            Console.WriteLine($"Processed: {processedLinesCounter} Unproccessed: {unproccessedLinesCounter}");
            previousArray = array;
            previousPart = deserializedPart;
        }
        File.WriteAllLines("parts.csv", parts.Select(x => x.ToString()));
        File.WriteAllLines("unprocessed.csv", uprocessedLines);
        DisplayDictionary(CountersDictionary);
        Console.WriteLine(Environment.NewLine);
        DisplayDictionary(PartResultCountersDictionary);
    }
}
class Part
{
    public string Number { get; set; }
    public double Price { get; set; }
    public string DiscountGroup { get; set; }
    public DateTime PriceStartDate { get; set; }

    public override string ToString()
    {
        return $"{Number},{Price},{DiscountGroup}";
    }
}
