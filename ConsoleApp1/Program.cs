using ConsoleApp1;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Program
{

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

    static Part Strategy1(byte[] array)
    {
        var partNumberRegex = new Regex("[^a-zA-Z0-9 -]");
        var lastPIndex = Array.LastIndexOf<byte>(array, 0x50);
        if (lastPIndex != -1)
        {
            Console.WriteLine(array.Length);
            var discountGroup = Encoding.UTF8.GetString(array, lastPIndex, 2);
            var priceAsHex = BitConverter.ToString(array.SubArray(lastPIndex - 4, 4).Reverse().ToArray()).Replace("-", string.Empty);
            var x = BitConverter.ToString(array.SubArray(lastPIndex - 4, 4).ToArray()).Replace("-", string.Empty);
            var price = Convert.ToInt32(priceAsHex, 16) / 100d;
            //var priceStartDateAsHex = BitConverter.ToString(array.SubArray(lastPIndex - 8, 4).Reverse().ToArray()).Replace("-", string.Empty);
            //var priceStartDateAsRawString = Convert.ToInt32(priceStartDateAsHex, 16).ToString();
            //var priceStartDate = DateTime.ParseExact(priceStartDateAsRawString, "yyyyMMdd", CultureInfo.InvariantCulture);
            var partContainingNumberPart = Encoding.UTF8.GetString(array.SubArray(0, lastPIndex - 8));
            var partNumber = partNumberRegex.Replace(partContainingNumberPart, "");
            //Console.WriteLine(partNumber);
            //Console.WriteLine(discountGroup);
            //Console.WriteLine(price);
            return new Part
            {
                Number = partNumber,
                DiscountGroup = discountGroup,
                Price = price,
                PriceStartDate = DateTime.UtcNow
            };
        }
        return null;
    }

    static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:x2} ", b);
        return hex.ToString();
    }

    static Part Strategy2(byte[] lineArray, byte[] previous)
    {
        var partNumberRegex = new Regex("[^a-zA-Z0-9 -]");
        var splitLine = SplitArray(lineArray, new byte[] { 0x81, 0x8B, 0x34, 0x01 });
        if (splitLine.Length != 2)
        {
            Console.WriteLine(ByteArrayToString(lineArray));
            throw new Exception("BRAK DATY");
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
        byte[] previous = null;
        foreach (var array in splitInput)
        {

            try
            {
                var deserializedPart = Strategy2(array, previous);
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
            previous = array;
        }
        File.WriteAllLines("parts.csv", parts.Select(x => x.ToString()));
        File.WriteAllLines("unprocessed.csv", uprocessedLines);
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
