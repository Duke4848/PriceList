using ConsoleApp1;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Program
{
    public static Dictionary<int, int> map = new();
    static byte[][] SplitArray(byte[] source, byte[] separator)
    {
        var Parts = new List<byte[]>();
        var Index = 0;
        byte[] Part;
        for (var I = 0; I < source.Length; ++I)
        {
            if (Equals(source, separator, I))
            {
                Part = new byte[I - Index];
                Array.Copy(source, Index, Part, 0, Part.Length);
                Parts.Add(Part);
                Index = I + separator.Length;
                I += separator.Length - 1;
            }
        }
        Part = new byte[source.Length - Index];
        Array.Copy(source, Index, Part, 0, Part.Length);
        Parts.Add(Part);
        return Parts.ToArray();
    }

    static bool Equals(byte[] source, byte[] separator, int index)
    {
        for (int i = 0; i < separator.Length; ++i)
            if (index + i >= source.Length || source[index + i] != separator[i])
                return false;
        return true;
    }


    static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
    static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:x2} ", b);
        return hex.ToString();
    }

    static Part Strategy2(byte[] lineArray, byte[] previousArray, Part previousPart)
    {
        var partNumberRegex = new Regex("[^a-zA-Z0-9 -]");

        var splitLine = SplitArray(lineArray, new byte[] { 0x81, 0x8B, 0x34, 0x01 });
        if (!map.ContainsKey(splitLine.Length))
        {
            map[splitLine.Length] = 1;
        }
        else
        {
            map[splitLine.Length]++;
        }
        if (splitLine.Length != 2)
        {
            if (splitLine.Length == 1 && previousArray != null)
            {
                splitLine = new byte[][] { null, splitLine[0] };
                var previousSplitLine = SplitArray(previousArray, new byte[] { 0x81, 0x8B, 0x34, 0x01 });
                if(previousSplitLine.Length == 1)
                {
                    //numer czesci bedzie w poprzedniej tablicy wystarczy podmiecni znaki string.empty
                }
            }
            else
            {
                Console.WriteLine(ByteArrayToString(lineArray));
                throw new Exception("BRAK DATY");
            }
        }
        var partContainingNumberPart = Encoding.UTF8.GetString(splitLine[0]);
        var partNumber = partNumberRegex.Replace(partContainingNumberPart, "");
        var priceAsHex = BitConverter.ToString(splitLine[1].SubArray(0, 4).Reverse().ToArray()).Replace("-", string.Empty); ;
        var price = Convert.ToInt32(priceAsHex, 16) / 100d;

        Console.WriteLine(lineArray.Length);
        var discountGroup = Encoding.UTF8.GetString(new byte[] { splitLine[1][5] });
        Console.WriteLine(partNumber);
        Console.WriteLine(discountGroup);
        Console.WriteLine(price);
        return new Part
        {
            Number = $"\t{partNumber}",
            DiscountGroup = discountGroup,
            Price = price,
            PriceStartDate = DateTime.UtcNow
        };
    }


    public static void Main(string[] args)
    {
        var inputBytes = File.ReadAllBytes("FPREIS.BIN");
        byte[] delimeter = { 0x01, 0xF4 };
        var splitInput = SplitArray(inputBytes, delimeter);


        var parts = new List<Part>();
        var uprocessedLines = new List<string>();
        int processedLinesCounter = 0, unproccessedLinesCounter = 0;
        byte[] previousArray = null;
        Part deserializedPart = null;
        Part previousPart = null;
        foreach (var array in splitInput)
        {
            try
            {
                deserializedPart = Strategy2(array, previousArray, previousPart);
                if (deserializedPart != null)
                {
                    parts.Add(deserializedPart);
                }
                else
                {
                    uprocessedLines.Add(Encoding.UTF8.GetString(array));
                }
                processedLinesCounter++;
            }
            catch (Exception ex)
            {
                uprocessedLines.Add(Environment.NewLine);
                uprocessedLines.Add(Encoding.UTF8.GetString(array));
                uprocessedLines.Add(ex.Message);
                uprocessedLines.Add(Environment.NewLine);
                unproccessedLinesCounter++;
                Console.WriteLine(unproccessedLinesCounter);

            }
            Console.WriteLine($"Processed: {processedLinesCounter} Unproccessed: {unproccessedLinesCounter}");
            previousArray = array;
            previousPart = deserializedPart;
        }
        File.WriteAllLines("parts.csv", parts.Select(x => x.ToString()));
        File.WriteAllLines("unprocessed.csv", uprocessedLines);
        foreach(var keyValuePair in map)
        {
            Console.WriteLine(keyValuePair.Key + " -> " + keyValuePair.Value);
        }
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
