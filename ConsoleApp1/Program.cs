using ConsoleApp1;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Program
{

    static byte[][] Separate(byte[] source, byte[] separator)
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

    public static void Main(string[] args)
    {
        var inputBytes = File.ReadAllBytes("FPREIS.BIN");
        byte[] delimeter = { 0xF4 };
        var splitInput = Separate(inputBytes, delimeter);


        var parts = new List<Part>();
        var uprocessed = new List<string>();
        int processedCounter = 0, unproccessedCounter = 0;
        foreach (var array in splitInput)
        {

            try
            {
                var deserializedPart = Strategy1(array);
                if (deserializedPart != null)
                {
                    parts.Add(deserializedPart);
                }
                else
                {
                    uprocessed.Add(Encoding.UTF8.GetString(array));
                }
                processedCounter++;
            }
            catch (Exception ex)
            {
                uprocessed.Add(Environment.NewLine);
                uprocessed.Add(Encoding.UTF8.GetString(array));
                uprocessed.Add(ex.Message);
                uprocessed.Add(Environment.NewLine);
                unproccessedCounter++;
                Console.WriteLine(unproccessedCounter);

            }
            Console.WriteLine($"Processed: {processedCounter} Unproccessed: {unproccessedCounter}");
        }
        File.WriteAllLines("parts.csv", parts.Select(x => x.ToString()));
        File.WriteAllLines("unprocessed.csv", uprocessed);
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
        return $"{Number},{Price},{PriceStartDate},{DiscountGroup}";
    }
}
